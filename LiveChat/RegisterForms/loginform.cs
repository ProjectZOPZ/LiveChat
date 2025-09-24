using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LiveChat
{
    public partial class AuthForm : UserControl
    {
        private static readonly HttpClient client = new HttpClient();
        private string settingsFile = "loginSettings.json";
        private LoginSettings loginSettings;

        public AuthForm()
        {
            InitializeComponent();
            LoadSettings();
        }

        private void loginform_Load(object sender, EventArgs e)
        {
            // Pre-fill username/password if saved
            if (loginSettings != null)
            {
                tbUsername.Text = loginSettings.Username;
                tbPassword.Text = loginSettings.Password;
            }
        }

        private void LoadSettings()
        {
            if (File.Exists(settingsFile))
            {
                string json = File.ReadAllText(settingsFile);
                loginSettings = JsonSerializer.Deserialize<LoginSettings>(json);
            }
        }

        private void SaveSettings()
        {
            string json = JsonSerializer.Serialize(loginSettings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(settingsFile, json);
        }

        private class LoginSettings
        {
            public string Username { get; set; }
            public string Password { get; set; }
        }

        private async void button_Click(object sender, EventArgs e)
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

                var response = await client.PostAsync("https://zopzsniff.xyz/auth/login", content);

                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show("Login successful!");

                    // Save credentials to local settings
                    loginSettings = new LoginSettings { Username = username, Password = password };
                    SaveSettings();

                    // TODO: Proceed to main form or chat
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

        private async void button_Click_1(object sender, EventArgs e)
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

                var response = await client.PostAsync("https://zopzsniff.xyz/auth/login", content);

                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show("Login successful!");

                    // Save credentials to local settings
                    loginSettings = new LoginSettings { Username = username, Password = password };
                    SaveSettings();

                    // Open MainForm and close/hide login form
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
    }
}
