using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using NAudio.Wave;

namespace LiveChat
{
    public partial class MainForm : Form
    {
        private string userId;
        private string currentGroupId = null;
        private string activeCallGroupId = null;
        private static readonly HttpClient client = new HttpClient();
        private System.Windows.Forms.Timer autoReloadTimer;
        private string settingsFile = "settings.json";
        private AppSettings appSettings;

        // NAudio
        private WaveInEvent waveIn;
        private BufferedWaveProvider waveProvider;
        private WaveOutEvent waveOut;
        private System.Threading.CancellationTokenSource callToken;

        public MainForm()
        {
            InitializeComponent();
            InitializeButtons();
            LoadSettings();
            LoadAudioDevices();
            usernamelb.Text = "" + (userId.Length >= 4 ? userId.Substring(0, 4) : userId);

            this.Text = string.Empty;
            this.ControlBox = false;
            this.MaximizedBounds = Screen.FromHandle(this.Handle).WorkingArea;

            flowLayoutPanel1.FlowDirection = FlowDirection.TopDown;
            flowLayoutPanel1.WrapContents = false;
            flowLayoutPanel1.AutoScroll = true;

            LoadMessages().ConfigureAwait(false);
            LoadMyGroups().ConfigureAwait(false);

            autoReloadTimer = new System.Windows.Forms.Timer();
            autoReloadTimer.Interval = 5000;
            autoReloadTimer.Tick += async (s, e) => await LoadMessages();
            autoReloadTimer.Start();

            // Textbox Enter-to-Send
            guna2TextBox1.KeyDown += async (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    if (!string.IsNullOrWhiteSpace(guna2TextBox1.Text))
                    {
                        await SendMessage();
                        await LoadMessages();
                    }
                }
            };

            guna2TextBoxInvite.KeyDown += async (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    string inviteCode = guna2TextBoxInvite.Text.Trim();
                    if (!string.IsNullOrEmpty(inviteCode))
                    {
                        await JoinGroup(inviteCode);
                        guna2TextBoxInvite.Clear();
                    }
                }
            };

            guna2TextBoxGroupName.KeyDown += async (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    string groupName = guna2TextBoxGroupName.Text.Trim();
                    if (!string.IsNullOrEmpty(groupName))
                    {
                        await CreateGroup(groupName);
                        guna2TextBoxGroupName.Clear();
                    }
                }
            };
        }

        private void guna2CirclePictureBox1_Click(object sender, EventArgs e)
        {
            using OpenFileDialog opf = new OpenFileDialog
            {
                Filter = "Choose Image(*.jpg;*.png;*.gif)|*.jpg;*.png;*.gif",
                InitialDirectory = appSettings.LastFolder
            };

            if (opf.ShowDialog() == DialogResult.OK)
            {
                string selectedFile = opf.FileName;
                appSettings.LastFolder = Path.GetDirectoryName(selectedFile);
                appSettings.LastImagePath = selectedFile;
                SaveSettings();
                guna2CirclePictureBox1.Image = Image.FromFile(selectedFile);
            }
        }

        #region Buttons
        private void InitializeButtons()
        {
            btnStartCall.Click += async (s, e) => await StartCall();
            btnJoinCall.Click += async (s, e) => await JoinCall();
            btnLeaveCall.Click += async (s, e) => await LeaveCall();
        }
        #endregion

        #region Voice Call
        private async Task StartCall()
        {
            if (string.IsNullOrEmpty(currentGroupId))
            {
                MessageBox.Show("Select a group first!");
                return;
            }

            var payload = new { userId };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await client.PostAsync($"https://zopzsniff.xyz/groups/{currentGroupId}/call/start", content);
            if (!response.IsSuccessStatusCode)
            {
                MessageBox.Show("Failed to start call: " + response.ReasonPhrase);
                return;
            }

            activeCallGroupId = currentGroupId;
            StartAudioStreaming();
            MessageBox.Show("Call started!");
        }

        private async Task JoinCall()
        {
            if (string.IsNullOrEmpty(currentGroupId))
            {
                MessageBox.Show("Select a group first!");
                return;
            }

            var payload = new { userId };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await client.PostAsync($"https://zopzsniff.xyz/groups/{currentGroupId}/call/join", content);
            if (!response.IsSuccessStatusCode)
            {
                MessageBox.Show("Failed to join call: " + response.ReasonPhrase);
                return;
            }

            activeCallGroupId = currentGroupId;
            StartAudioStreaming();
            MessageBox.Show("Joined call!");
        }

        private async Task LeaveCall()
        {
            if (string.IsNullOrEmpty(activeCallGroupId)) return;

            var payload = new { userId };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            await client.PostAsync($"https://zopzsniff.xyz/groups/{activeCallGroupId}/call/leave", content);

            StopAudioStreaming();
            activeCallGroupId = null;
            MessageBox.Show("Call left.");
        }
        private void StartAudioStreaming()
        {
            int inputDevice = comboBoxInputDevices.SelectedIndex >= 0 ? comboBoxInputDevices.SelectedIndex : 0;
            int outputDevice = comboBoxOutputDevices.SelectedIndex >= 0 ? comboBoxOutputDevices.SelectedIndex : 0;

            waveIn = new WaveInEvent
            {
                DeviceNumber = inputDevice,
                WaveFormat = new WaveFormat(48000, 16, 1)
            };

            waveIn.DataAvailable += async (s, a) =>
            {
                try
                {
                    var content = new ByteArrayContent(a.Buffer);
                    await client.PostAsync($"https://zopzsniff.xyz/groups/{activeCallGroupId}/audio", content);
                }
                catch { }
            };
            waveIn.StartRecording();

            waveProvider = new BufferedWaveProvider(new WaveFormat(48000, 16, 1));
            waveOut = new WaveOutEvent { DeviceNumber = outputDevice };
            waveOut.Init(waveProvider);
            waveOut.Play();

            callToken = new System.Threading.CancellationTokenSource();
            Task.Run(async () =>
            {
                while (!callToken.Token.IsCancellationRequested)
                {
                    try
                    {
                        var bytes = await client.GetByteArrayAsync($"https://zopzsniff.xyz/groups/{activeCallGroupId}/audio");
                        waveProvider.AddSamples(bytes, 0, bytes.Length);
                    }
                    catch { }
                    await Task.Delay(50);
                }
            });
        }


        private void StopAudioStreaming()
        {
            waveIn?.StopRecording();
            waveIn?.Dispose();
            waveIn = null;

            waveOut?.Stop();
            waveOut?.Dispose();
            waveOut = null;

            callToken?.Cancel();
            callToken = null;
        }
        #endregion

        #region Settings
        private void LoadSettings()
        {
            if (File.Exists(settingsFile))
            {
                string json = File.ReadAllText(settingsFile);
                appSettings = JsonSerializer.Deserialize<AppSettings>(json);
            }

            if (appSettings == null)
                appSettings = new AppSettings { UserId = Guid.NewGuid().ToString() };

            userId = appSettings.UserId;
            currentGroupId = appSettings.LastGroupId;

            if (!string.IsNullOrEmpty(appSettings.LastImagePath) && File.Exists(appSettings.LastImagePath))
            {
                try { guna2CirclePictureBox1.Image = Image.FromFile(appSettings.LastImagePath); }
                catch { }
            }
        }

        private void SaveSettings()
        {
            appSettings.LastGroupId = currentGroupId;
            string json = JsonSerializer.Serialize(appSettings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(settingsFile, json);
        }
        #endregion

        #region Groups & Messages
        private async Task CreateGroup(string groupName)
        {
            try
            {
                string url = "https://zopzsniff.xyz/groups/create";
                var payload = new { name = groupName };
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(url, content);
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadAsStringAsync();
                var group = JsonSerializer.Deserialize<Group>(result, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (group == null || string.IsNullOrEmpty(group.InviteCode))
                {
                    MessageBox.Show("Error: Invite code not returned by backend.");
                    return;
                }

                await JoinGroup(group.InviteCode);
                MessageBox.Show($"Group created!\nInvite code: {group.InviteCode}");
            }
            catch (Exception ex) { MessageBox.Show("Error creating group: " + ex.Message); }
        }

        private async Task JoinGroup(string inviteCode)
        {
            try
            {
                string url = "https://zopzsniff.xyz/groups/join";
                var payload = new { inviteCode, userId };
                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                var response = await client.PostAsync(url, content);
                if (!response.IsSuccessStatusCode)
                {
                    MessageBox.Show($"Error joining group: {response.StatusCode}");
                    return;
                }

                var result = await response.Content.ReadAsStringAsync();
                var group = JsonSerializer.Deserialize<Group>(result, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                currentGroupId = group.Id;
                SaveSettings();

                await LoadMyGroups();
                await LoadMessages();

                MessageBox.Show($"Joined group: {group.Name} (Invite code: {group.InviteCode})");
            }
            catch (Exception ex) { MessageBox.Show("Error joining group: " + ex.Message); }
        }

        private async Task LoadMyGroups()
        {
            try
            {
                string url = $"https://zopzsniff.xyz/groups/user/{userId}";
                var response = await client.GetStringAsync(url);
                var groups = JsonSerializer.Deserialize<List<Group>>(response, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                flowGroups.Controls.Clear();

                var mainChat = new GroupItemControl("Main Chat", null, "") { Width = flowGroups.Width };
                mainChat.OnGroupSelected += async (s, e) => { currentGroupId = null; SaveSettings(); await LoadMessages(); };
                flowGroups.Controls.Add(mainChat);

                foreach (var g in groups)
                {
                    var ctrl = new GroupItemControl(g.Name, g.Id, g.InviteCode) { Width = flowGroups.Width };
                    ctrl.OnGroupSelected += async (s, e) => { currentGroupId = g.Id; SaveSettings(); await LoadMessages(); };
                    flowGroups.Controls.Add(ctrl);

                    if (currentGroupId == g.Id) await LoadMessages();
                }
            }
            catch (Exception ex) { MessageBox.Show("Error loading groups: " + ex.Message); }
        }

        private async Task LoadMessages()
        {
            try
            {
                string url = string.IsNullOrEmpty(currentGroupId)
                    ? "https://zopzsniff.xyz/messages"
                    : $"https://zopzsniff.xyz/groups/{currentGroupId}/messages";

                var response = await client.GetStringAsync(url);
                var messages = JsonSerializer.Deserialize<List<Message>>(response, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (messages == null) return;

                messages.Reverse();
                flowLayoutPanel1.SuspendLayout();
                flowLayoutPanel1.Controls.Clear();

                foreach (var msg in messages)
                {
                    bool isSender = msg.UserId == userId || msg.IsSender;
                    var ctrl = new MessageControl(msg.Type, msg.Content, msg.Timestamp, isSender)
                    {
                        Dock = DockStyle.Top,
                        Anchor = isSender ? AnchorStyles.Right : AnchorStyles.Left
                    };
                    flowLayoutPanel1.Controls.Add(ctrl);
                }

                flowLayoutPanel1.FlowDirection = FlowDirection.TopDown;
                flowLayoutPanel1.ResumeLayout();
            }
            catch { }
        }
        private async void btnStartCall_Click(object sender, EventArgs e)
        {
            await StartCall();
        }

        private async void btnJoinCall_Click(object sender, EventArgs e)
        {
            await JoinCall();
        }

        private async void btnLeaveCall_Click(object sender, EventArgs e)
        {
            await LeaveCall();
        }
        private async Task SendMessage()
        {
            try
            {
                string url = string.IsNullOrEmpty(currentGroupId)
                    ? "https://zopzsniff.xyz/send"
                    : $"https://zopzsniff.xyz/groups/{currentGroupId}/send";

                string text = guna2TextBox1.Text.Trim();
                if (string.IsNullOrEmpty(text)) return;

                string type = text.EndsWith(".gif") || text.EndsWith(".jpg") || text.EndsWith(".jpeg") || text.EndsWith(".png") ? "image" :
                              text.StartsWith("http") ? "link" : "text";

                var payload = new { type, content = text, userId };
                var contentJson = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                var response = await client.PostAsync(url, contentJson);
                response.EnsureSuccessStatusCode();

                guna2TextBox1.Clear();
                await LoadMessages();
            }
            catch { }
        }
        #endregion
        private void LoadAudioDevices()
        {
            comboBoxInputDevices.Items.Clear();
            for (int i = 0; i < WaveInEvent.DeviceCount; i++)
            {
                var deviceInfo = WaveInEvent.GetCapabilities(i);
                comboBoxInputDevices.Items.Add($"{i}: {deviceInfo.ProductName}");
            }
            if (comboBoxInputDevices.Items.Count > 0)
                comboBoxInputDevices.SelectedIndex = 0;

            comboBoxOutputDevices.Items.Clear();
            for (int i = 0; i < WaveOut.DeviceCount; i++)
            {
                var deviceInfo = WaveOut.GetCapabilities(i);
                comboBoxOutputDevices.Items.Add($"{i}: {deviceInfo.ProductName}");
            }
            if (comboBoxOutputDevices.Items.Count > 0)
                comboBoxOutputDevices.SelectedIndex = 0;
        }

        #region Models
        public class AppSettings
        {
            public string UserId { get; set; }
            public string LastGroupId { get; set; }
            public string LastFolder { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            public string LastImagePath { get; set; } = string.Empty;
        }
        public class Group
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string InviteCode { get; set; }
            public List<Message> Messages { get; set; }
            public List<string> Members { get; set; }
        }

        public class Message
        {
            public string Type { get; set; }
            public string Content { get; set; }
            public string Timestamp { get; set; }
            public string UserId { get; set; }
            public bool IsSender { get; set; }
        }

        #endregion

        private void comboBoxOutputDevices_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (waveOut != null)
            {
                waveOut.Stop();
                waveOut.Dispose();
                waveOut = null;
                StartAudioStreaming(); // restart with new output
            }
        }

        private void comboBoxInputDevices_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (waveIn != null)
            {
                waveIn.StopRecording();
                waveIn.Dispose();
                waveIn = null;
                StartAudioStreaming(); // restart with new input
            }
        }
    }
}
