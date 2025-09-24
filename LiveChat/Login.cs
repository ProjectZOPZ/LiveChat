using Guna.UI2.WinForms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Net.Http;

namespace LiveChat
{
    public partial class Login : Form
    {
        private static readonly HttpClient client = new HttpClient();

        private string settingsFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ZOPZ CHAT",
            "loginSettings.json"
        );

        private LoginSettings loginSettings;

        public Login()
        {
            InitializeComponent();
            LoadSettings();

            this.Text = string.Empty;
            this.ControlBox = false;
            this.MaximizedBounds = Screen.FromHandle(this.Handle).WorkingArea;
        }

        private void LoadSettings()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(settingsFile));

            if (File.Exists(settingsFile))
            {
                string json = File.ReadAllText(settingsFile);
                loginSettings = JsonSerializer.Deserialize<LoginSettings>(json);
            }
        }

        private void SaveSettings()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(settingsFile));

            string json = JsonSerializer.Serialize(loginSettings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(settingsFile, json);
        }

        private class LoginSettings
        {
            public string Username { get; set; }
            public string Password { get; set; }
        }

        private void btnLeaveCall_Click(object sender, EventArgs e)
        {
            panel2.Controls.Clear();
            panel2.Controls.Add(new LiveChat.AuthForm());
        }

        private void btnStartCall_Click(object sender, EventArgs e)
        {
            panel2.Controls.Clear();
            panel2.Controls.Add(new register());
        }

        private void Login_Load(object sender, EventArgs e)
        {
            if (loginSettings != null)
            {
                tbUsername.Text = loginSettings.Username;
                tbPassword.Text = loginSettings.Password;
            }
        }

        private async void LoginBTN_Click(object sender, EventArgs e)
        {
            string username = tbUsername.Text.Trim();
            string password = tbPassword.Text.Trim();

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Please enter username and password.");
                return;
            }

            try
            {
                var payload = new { username, password };
                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                var response = await client.PostAsync("https://zopzsniff.xyz/chat/auth/login", content);

                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show("Login successful!");

                    loginSettings = new LoginSettings { Username = username, Password = password };
                    SaveSettings();

                    Hide();
                    new MainForm().Show();
                }
                else
                {
                    MessageBox.Show("Login failed: " + response.ReasonPhrase);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error logging in: " + ex.Message);
            }
        }

        private async void guna2Button1_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(tbUsername.Text) || string.IsNullOrEmpty(tbPassword.Text))
                {
                    MessageBox.Show("Please enter both username and password.");
                    return;
                }
                var payload = new
                {
                    username = tbUsername.Text,
                    password = tbPassword.Text
                };
                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                var response = await client.PostAsync("https://zopzsniff.xyz/chat/auth/register", content);

                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show("Successfully registered!");
                }
                else
                {
                    string respText = await response.Content.ReadAsStringAsync();
                    MessageBox.Show($"Registration failed: {respText}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error during registration: " + ex.Message);
            }
        }
    }
}
