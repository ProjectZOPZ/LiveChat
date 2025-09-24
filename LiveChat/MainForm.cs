using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        private string username;
        private string password;
        private string? currentGroupId = null;
        private string? currentDMId = null;
        private string? activeCallGroupId = null;
        private static readonly HttpClient client = new HttpClient();
        private System.Windows.Forms.Timer autoReloadTimer;
        private string settingsFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ZOPZ CHAT",
            "loginSettings.json"
        );
        private string profileSettingsFile = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
    "ZOPZ CHAT",
    "settings.json"
);

        private ProfileSettings profileSettings;
        private AppSettings appSettings;
        private WaveInEvent waveIn;
        private BufferedWaveProvider waveProvider;
        private WaveOutEvent waveOut;
        private System.Threading.CancellationTokenSource callToken;

        private GroupItemControl? selectedGroupControl = null;
        private GroupItemControl? selectedDMControl = null;

        private List<User> allUsers = new List<User>();

        public MainForm()
        {
            InitializeComponent();
            InitializeButtons();
            LoadSettings();
            LoadAudioDevices();

            this.Text = string.Empty;
            this.ControlBox = false;
            this.MaximizedBounds = Screen.FromHandle(this.Handle).WorkingArea;

            flowLayoutPanel1.FlowDirection = FlowDirection.TopDown;
            flowLayoutPanel1.WrapContents = false;
            flowLayoutPanel1.AutoScroll = true;

            Task.Run(LoadAllUsers).Wait();

            LoadMessages().ConfigureAwait(false);
            LoadMyGroups().ConfigureAwait(false);
            LoadUsersAndDMs().ConfigureAwait(false);

            autoReloadTimer = new System.Windows.Forms.Timer();
            autoReloadTimer.Interval = 5000;
            autoReloadTimer.Tick += async (s, e) =>
            {
                if (guna2TabControl1.SelectedIndex == 0)
                    await LoadMessages();
                else if (guna2TabControl1.SelectedIndex == 3 && !string.IsNullOrEmpty(currentDMId))
                    await LoadDMMessages(currentDMId);

                await LoadUsersAndDMs();
            };
            autoReloadTimer.Start();

            guna2TextBox1.KeyDown += async (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    if (!string.IsNullOrWhiteSpace(guna2TextBox1.Text))
                        await SendMessage();
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
                        UpdateControlSafe(guna2TextBoxInvite, () => guna2TextBoxInvite.Clear());
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
                        UpdateControlSafe(guna2TextBoxGroupName, () => guna2TextBoxGroupName.Clear());
                    }
                }
            };

            comboBoxOutputDevices.SelectedIndexChanged += comboBoxOutputDevices_SelectedIndexChanged;
            comboBoxInputDevices.SelectedIndexChanged += comboBoxInputDevices_SelectedIndexChanged;

            usernamelb.Text = username;

            LoadProfileSettings();
        }
        private async Task LoadAllUsers() { try { var request = new HttpRequestMessage(HttpMethod.Get, "https://zopzsniff.xyz/livechatusers"); AddAuthHeaders(request); var response = await client.SendAsync(request); response.EnsureSuccessStatusCode(); var json = await response.Content.ReadAsStringAsync(); allUsers = JsonSerializer.Deserialize<List<User>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<User>(); } catch { allUsers = new List<User>(); } }
        private void UpdateControlSafe(Control ctrl, Action action)
        {
            if (ctrl.InvokeRequired) ctrl.Invoke(action);
            else action();
        }

        private void InitializeButtons()
        {
            btnStartCall.Click += async (s, e) => await StartCall();
            btnJoinCall.Click += async (s, e) => await JoinCall();
            btnLeaveCall.Click += async (s, e) => await LeaveCall();
        }

        private void AddAuthHeaders(HttpRequestMessage request)
        {
            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                request.Headers.Remove("username");
                request.Headers.Remove("password");
                request.Headers.Add("username", username);
                request.Headers.Add("password", password);
            }
        }

        #region Calls
        private async Task StartCall()
        {
            if (string.IsNullOrEmpty(currentGroupId))
            {
                MessageBox.Show("Select a group first!");
                return;
            }

            var payload = new { userId };
            var request = new HttpRequestMessage(HttpMethod.Post, $"https://zopzsniff.xyz/groups/{currentGroupId}/call/start");
            request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            AddAuthHeaders(request);

            var response = await client.SendAsync(request);
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
            var request = new HttpRequestMessage(HttpMethod.Post, $"https://zopzsniff.xyz/groups/{currentGroupId}/call/join");
            request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            AddAuthHeaders(request);

            var response = await client.SendAsync(request);
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
            var request = new HttpRequestMessage(HttpMethod.Post, $"https://zopzsniff.xyz/groups/{activeCallGroupId}/call/leave");
            request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            AddAuthHeaders(request);

            await client.SendAsync(request);

            StopAudioStreaming();
            activeCallGroupId = null;
            MessageBox.Show("Call left.");
        }
        #endregion

        private void StartAudioStreaming() { /* Implement audio streaming */ }
        private void StopAudioStreaming() { /* Implement audio stop */ }

        #region Settings
        private void LoadSettings()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(settingsFile));

            if (File.Exists(settingsFile))
            {
                string json = File.ReadAllText(settingsFile);
                appSettings = JsonSerializer.Deserialize<AppSettings>(json);
            }

            if (appSettings == null)
                appSettings = new AppSettings { UserId = Guid.NewGuid().ToString() };

            userId = appSettings.UserId;
            username = appSettings.Username;
            password = appSettings.Password;
            currentGroupId = appSettings.LastGroupId;
            currentDMId = appSettings.LastDMId;

            if (!string.IsNullOrWhiteSpace(appSettings.LastImagePath) && File.Exists(appSettings.LastImagePath))
            {
                try { guna2CirclePictureBox1.Image = Image.FromFile(appSettings.LastImagePath); }
                catch { }
            }
        }

        private void SaveSettings()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(settingsFile));
            appSettings.UserId = userId;
            appSettings.Username = username;
            appSettings.Password = password;
            appSettings.LastGroupId = currentGroupId;
            appSettings.LastDMId = currentDMId;

            string json = JsonSerializer.Serialize(appSettings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(settingsFile, json);
        }
        #endregion

        #region Groups
        private async Task CreateGroup(string groupName)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, "https://zopzsniff.xyz/groups/create");
                request.Content = new StringContent(JsonSerializer.Serialize(new { name = groupName }), Encoding.UTF8, "application/json");
                AddAuthHeaders(request);

                var response = await client.SendAsync(request);
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
                var request = new HttpRequestMessage(HttpMethod.Post, "https://zopzsniff.xyz/groups/join");
                request.Content = new StringContent(JsonSerializer.Serialize(new { inviteCode, userId }), Encoding.UTF8, "application/json");
                AddAuthHeaders(request);

                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadAsStringAsync();
                var group = JsonSerializer.Deserialize<Group>(result, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                currentGroupId = group.Id;
                currentDMId = null;
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
                var request = new HttpRequestMessage(HttpMethod.Get, "https://zopzsniff.xyz/groups/user");
                AddAuthHeaders(request);

                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                var groups = JsonSerializer.Deserialize<List<Group>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                UpdateControlSafe(flowGroups, () =>
                {
                    flowGroups.Controls.Clear();

                    var mainChat = new GroupItemControl("Main Chat", null, "") { Width = flowGroups.Width };
                    mainChat.OnGroupSelected += async (s, e) =>
                    {
                        currentGroupId = null;
                        currentDMId = null;
                        SaveSettings();
                        selectedGroupControl = mainChat;
                        await LoadMessages();
                    };
                    flowGroups.Controls.Add(mainChat);

                    if (groups != null)
                    {
                        foreach (var g in groups)
                        {
                            var ctrl = new GroupItemControl(g.Name, g.Id, g.InviteCode) { Width = flowGroups.Width };
                            ctrl.OnGroupSelected += async (s, e) =>
                            {
                                currentGroupId = g.Id;
                                currentDMId = null;
                                SaveSettings();
                                selectedGroupControl = ctrl;
                                await LoadMessages();
                            };
                            flowGroups.Controls.Add(ctrl);

                            if (g.Id == currentGroupId)
                                selectedGroupControl = ctrl;
                        }
                    }

                    selectedGroupControl?.Select();
                });
            }
            catch { }
        }
        #endregion

        #region Users & DMs (Merged)
        private async Task LoadUsersAndDMs()
        {
            try
            {
                var requestUsers = new HttpRequestMessage(HttpMethod.Get, "https://zopzsniff.xyz/livechatusers");
                AddAuthHeaders(requestUsers);
                var responseUsers = await client.SendAsync(requestUsers);
                responseUsers.EnsureSuccessStatusCode();
                var jsonUsers = await responseUsers.Content.ReadAsStringAsync();
                allUsers = JsonSerializer.Deserialize<List<User>>(jsonUsers, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<User>();

                var requestDMs = new HttpRequestMessage(HttpMethod.Get, "https://zopzsniff.xyz/dm");
                AddAuthHeaders(requestDMs);
                var responseDMs = await client.SendAsync(requestDMs);
                responseDMs.EnsureSuccessStatusCode();
                var jsonDMs = await responseDMs.Content.ReadAsStringAsync();
                var conversations = JsonSerializer.Deserialize<List<DMConversation>>(jsonDMs, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<DMConversation>();

                UpdateControlSafe(flowUsers, () =>
                {
                    flowUsers.Controls.Clear();

                    foreach (var u in allUsers)
                    {
                        if (u.Username == username) continue;

                        var userCtrl = new GroupItemControl(u.Username, u.Id, null) { Width = flowUsers.Width };
                        userCtrl.OnGroupSelected += async (s, e) =>
                        {
                            await StartOrOpenDM(u.Id);
                        };
                        flowUsers.Controls.Add(userCtrl);
                    }

                    foreach (var c in conversations)
                    {
                        string otherUserId = c.Participants.FirstOrDefault(p => p != userId);
                        if (string.IsNullOrEmpty(otherUserId)) continue;

                        string otherUsername = allUsers.FirstOrDefault(u => u.Id == otherUserId)?.Username ?? otherUserId;

                        if (otherUsername == username) continue;

                        bool alreadyExists = flowUsers.Controls
                            .OfType<GroupItemControl>()
                            .Any(ctrl => ctrl.Username == otherUsername);

                        if (alreadyExists) continue;

                        var dmCtrl = new GroupItemControl(otherUsername, c.Id, null) { Width = flowUsers.Width };
                        dmCtrl.OnGroupSelected += async (s, e) =>
                        {
                            currentGroupId = null;
                            currentDMId = c.Id;
                            SaveSettings();

                            foreach (Control item in flowUsers.Controls)

                            await LoadDMMessages(c.Id);
                            guna2TabControl1.SelectedIndex = 3;
                            guna2TextBox1.Focus();
                        };

                        flowUsers.Controls.Add(dmCtrl);

                        if (c.Id == currentDMId)
                            dmCtrl.BackColor = System.Drawing.Color.LightBlue;
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load users and DMs: " + ex.Message);
            }
        }



        private async Task StartOrOpenDM(string otherUserId)
        {
            try
            {
                var payload = new { otherUserId };
                var request = new HttpRequestMessage(HttpMethod.Post, "https://zopzsniff.xyz/dm/start")
                {
                    Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
                };
                AddAuthHeaders(request);

                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                var dmConversation = JsonSerializer.Deserialize<DMConversation>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (dmConversation == null) return;

                currentGroupId = null;
                currentDMId = dmConversation.Id;
                SaveSettings();

                await LoadDMMessages(dmConversation.Id);
                await LoadUsersAndDMs();

                guna2TabControl1.SelectedIndex = 3;
                guna2TextBox1.Focus();
            }
            catch { }
        }
        #endregion

        #region Messages
        private async Task LoadMessages()
        {
            try
            {
                string url = string.IsNullOrEmpty(currentGroupId)
                    ? "https://zopzsniff.xyz/messages"
                    : $"https://zopzsniff.xyz/groups/{currentGroupId}/messages";

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                AddAuthHeaders(request);
                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var messages = JsonSerializer.Deserialize<List<Message>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (messages == null) return;

                messages.Reverse();
                UpdateControlSafe(flowLayoutPanel1, () =>
                {
                    flowLayoutPanel1.SuspendLayout();
                    flowLayoutPanel1.Controls.Clear();

                    foreach (var msg in messages)
                    {
                        bool isSender = msg.UserId == userId || msg.IsSender;
                        string senderName = allUsers.FirstOrDefault(u => u.Id == msg.UserId)?.Username ?? "Unknown";

                        var ctrl = new MessageControl(senderName, msg.Type, msg.Content, msg.Timestamp, isSender)
                        {
                            Dock = DockStyle.Top,
                            Anchor = isSender ? AnchorStyles.Right : AnchorStyles.Left
                        };
                        flowLayoutPanel1.Controls.Add(ctrl);
                    }

                    flowLayoutPanel1.FlowDirection = FlowDirection.TopDown;
                    flowLayoutPanel1.ResumeLayout();

                    if (flowLayoutPanel1.Controls.Count > 0)
                        flowLayoutPanel1.ScrollControlIntoView(flowLayoutPanel1.Controls[flowLayoutPanel1.Controls.Count - 1]);
                });
            }
            catch { }
        }

        private async Task LoadDMMessages(string conversationId)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"https://zopzsniff.xyz/dm/{conversationId}/messages");
                AddAuthHeaders(request);
                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var messages = JsonSerializer.Deserialize<List<Message>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (messages == null) return;

                messages.Reverse();
                UpdateControlSafe(flowLayoutPanel1, () =>
                {
                    flowLayoutPanel1.SuspendLayout();
                    flowLayoutPanel1.Controls.Clear();

                    foreach (var msg in messages)
                    {
                        bool isSender = msg.UserId == userId || msg.IsSender;
                        string senderName = allUsers.FirstOrDefault(u => u.Id == msg.UserId)?.Username ?? "Unknown";

                        var ctrl = new MessageControl(senderName, msg.Type, msg.Content, msg.Timestamp, isSender)
                        {
                            Dock = DockStyle.Top,
                            Anchor = isSender ? AnchorStyles.Right : AnchorStyles.Left
                        };
                        flowLayoutPanel1.Controls.Add(ctrl);
                    }

                    flowLayoutPanel1.FlowDirection = FlowDirection.TopDown;
                    flowLayoutPanel1.ResumeLayout();

                    if (flowLayoutPanel1.Controls.Count > 0)
                        flowLayoutPanel1.ScrollControlIntoView(flowLayoutPanel1.Controls[flowLayoutPanel1.Controls.Count - 1]);
                });
            }
            catch { }
        }

        private async Task SendMessage()
        {
            try
            {
                string url = null;

                if (!string.IsNullOrEmpty(currentGroupId))
                    url = $"https://zopzsniff.xyz/groups/{currentGroupId}/send";
                else if (!string.IsNullOrEmpty(currentDMId))
                    url = $"https://zopzsniff.xyz/dm/{currentDMId}/send";

                if (string.IsNullOrEmpty(url))
                {
                    MessageBox.Show("Select a group or user first!");
                    return;
                }

                string text = guna2TextBox1.Text.Trim();
                if (string.IsNullOrEmpty(text)) return;

                string type = text.EndsWith(".gif") || text.EndsWith(".jpg") || text.EndsWith(".jpeg") || text.EndsWith(".png") ? "image" :
                              text.StartsWith("http") ? "link" : "text";

                var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Content = new StringContent(JsonSerializer.Serialize(new { type, content = text, userId }), Encoding.UTF8, "application/json");
                AddAuthHeaders(request);

                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();

                guna2TextBox1.Clear();

                if (!string.IsNullOrEmpty(currentDMId))
                    await LoadDMMessages(currentDMId);
                else if (!string.IsNullOrEmpty(currentGroupId))
                    await LoadMessages();
            }
            catch { }
        }
        #endregion

        #region Audio Devices
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

        private void comboBoxOutputDevices_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (waveOut != null)
            {
                waveOut.Stop();
                waveOut.Dispose();
                waveOut = null;
                StartAudioStreaming();
            }
        }

        private void comboBoxInputDevices_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (waveIn != null)
            {
                waveIn.StopRecording();
                waveIn.Dispose();
                waveIn = null;
                StartAudioStreaming();
            }
        }
        #endregion

        #region Models
        public class User { public string Id { get; set; } public string Username { get; set; } }
        public class DMConversation { public string Id { get; set; } public List<string> Participants { get; set; } public Message LastMessage { get; set; } }
        public class AppSettings
        {
            public string? UserId { get; set; }
            public string? Username { get; set; }
            public string? Password { get; set; }
            public string? LastGroupId { get; set; }
            public string? LastDMId { get; set; }
            public string LastFolder { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            public string LastImagePath { get; set; } = string.Empty;
        }
        public class Group { public string? Id { get; set; } public string? Name { get; set; } public string? InviteCode { get; set; } public List<Message>? Messages { get; set; } public List<string>? Members { get; set; } }
        public class Message { public string? Type { get; set; } public string? Content { get; set; } public string? Timestamp { get; set; } public string? UserId { get; set; } public bool IsSender { get; set; } }
        #endregion
        public class ProfileSettings
        {
            public string LastFolder { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            public string LastImagePath { get; set; } = string.Empty;
            public string ProfileLink { get; set; } = string.Empty;
        }
        private void guna2CirclePictureBox1_Click(object sender, EventArgs e)
        {
            using OpenFileDialog opf = new OpenFileDialog
            {
                Filter = "Choose Image(*.jpg;*.png;*.gif)|*.jpg;*.png;*.gif",
                InitialDirectory = profileSettings.LastFolder
            };

            if (opf.ShowDialog() == DialogResult.OK)
            {
                string selectedFile = opf.FileName;
                profileSettings.LastFolder = Path.GetDirectoryName(selectedFile);
                profileSettings.LastImagePath = selectedFile;
                SaveProfileSettings();

                try { guna2CirclePictureBox1.Image = Image.FromFile(selectedFile); }
                catch { }
            }
        }
        private void LoadProfileSettings()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(profileSettingsFile));

            if (File.Exists(profileSettingsFile))
            {
                string json = File.ReadAllText(profileSettingsFile);
                profileSettings = JsonSerializer.Deserialize<ProfileSettings>(json);
            }

            if (profileSettings == null)
                profileSettings = new ProfileSettings();

            if (!string.IsNullOrWhiteSpace(profileSettings.LastImagePath) && File.Exists(profileSettings.LastImagePath))
            {
                try { guna2CirclePictureBox1.Image = Image.FromFile(profileSettings.LastImagePath); }
                catch { }
            }
        }

        private void SaveProfileSettings()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(profileSettingsFile));
            string json = JsonSerializer.Serialize(profileSettings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(profileSettingsFile, json);
        }

        private void guna2ControlBox1_Click(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }

        private void MainForm_Load(object sender, EventArgs e) { }
    }
}
