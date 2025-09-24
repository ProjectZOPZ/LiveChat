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

namespace LiveChat
{
    public partial class register : UserControl
    {
        private static readonly HttpClient client = new HttpClient();

        public register()
        {
            InitializeComponent();
        }

        private async void btnJoinCall_Click(object sender, EventArgs e)
        {
            try
            {
                // Grab username and password from your textboxes
                string username = guna2TextBox1.Text.Trim(); // replace 'usernameTb' with your actual textbox name
                string password = guna2TextBox2.Text.Trim(); // replace 'passwordTb' with your actual textbox name

                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    MessageBox.Show("Please enter both username and password.");
                    return;
                }

                // Create JSON payload
                var payload = new
                {
                    username = username,
                    password = password
                };
                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                // Send POST request to register endpoint
                var response = await client.PostAsync("https://zopzsniff.xyz/auth/register", content);

                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show("Successfully registered!");
                    guna2TextBox1.Clear();
                    guna2TextBox2.Clear();
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
