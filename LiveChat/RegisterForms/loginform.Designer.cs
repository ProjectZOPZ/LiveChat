namespace LiveChat
{
    partial class AuthForm
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges1 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges2 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges3 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges4 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges5 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges6 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            button = new Guna.UI2.WinForms.Guna2Button();
            tbPassword = new Guna.UI2.WinForms.Guna2TextBox();
            tbUsername = new Guna.UI2.WinForms.Guna2TextBox();
            SuspendLayout();
            // 
            // button
            // 
            button.CustomizableEdges = customizableEdges1;
            button.DisabledState.BorderColor = Color.DarkGray;
            button.DisabledState.CustomBorderColor = Color.DarkGray;
            button.DisabledState.FillColor = Color.FromArgb(169, 169, 169);
            button.DisabledState.ForeColor = Color.FromArgb(141, 141, 141);
            button.FillColor = Color.FromArgb(15, 15, 15);
            button.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            button.ForeColor = Color.White;
            button.Location = new Point(63, 184);
            button.Name = "button";
            button.ShadowDecoration.CustomizableEdges = customizableEdges2;
            button.Size = new Size(442, 45);
            button.TabIndex = 8;
            button.Text = "Login";
            button.Click += button_Click_1;
            // 
            // tbPassword
            // 
            tbPassword.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            tbPassword.BackColor = Color.FromArgb(20, 20, 20);
            tbPassword.BorderColor = Color.FromArgb(20, 20, 20);
            tbPassword.CustomizableEdges = customizableEdges3;
            tbPassword.DefaultText = "";
            tbPassword.DisabledState.BorderColor = Color.FromArgb(208, 208, 208);
            tbPassword.DisabledState.FillColor = Color.FromArgb(226, 226, 226);
            tbPassword.DisabledState.ForeColor = Color.FromArgb(138, 138, 138);
            tbPassword.DisabledState.PlaceholderForeColor = Color.FromArgb(138, 138, 138);
            tbPassword.FillColor = Color.FromArgb(20, 20, 20);
            tbPassword.FocusedState.BorderColor = Color.FromArgb(20, 20, 20);
            tbPassword.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            tbPassword.ForeColor = Color.WhiteSmoke;
            tbPassword.HoverState.BorderColor = Color.FromArgb(20, 20, 20);
            tbPassword.Location = new Point(63, 119);
            tbPassword.Name = "tbPassword";
            tbPassword.PlaceholderForeColor = Color.WhiteSmoke;
            tbPassword.PlaceholderText = "Password";
            tbPassword.SelectedText = "";
            tbPassword.ShadowDecoration.CustomizableEdges = customizableEdges4;
            tbPassword.Size = new Size(442, 41);
            tbPassword.TabIndex = 7;
            // 
            // tbUsername
            // 
            tbUsername.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            tbUsername.BackColor = Color.FromArgb(20, 20, 20);
            tbUsername.BorderColor = Color.FromArgb(20, 20, 20);
            tbUsername.CustomizableEdges = customizableEdges5;
            tbUsername.DefaultText = "";
            tbUsername.DisabledState.BorderColor = Color.FromArgb(208, 208, 208);
            tbUsername.DisabledState.FillColor = Color.FromArgb(226, 226, 226);
            tbUsername.DisabledState.ForeColor = Color.FromArgb(138, 138, 138);
            tbUsername.DisabledState.PlaceholderForeColor = Color.FromArgb(138, 138, 138);
            tbUsername.FillColor = Color.FromArgb(20, 20, 20);
            tbUsername.FocusedState.BorderColor = Color.FromArgb(20, 20, 20);
            tbUsername.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            tbUsername.ForeColor = Color.WhiteSmoke;
            tbUsername.HoverState.BorderColor = Color.FromArgb(20, 20, 20);
            tbUsername.Location = new Point(63, 61);
            tbUsername.Name = "tbUsername";
            tbUsername.PlaceholderForeColor = Color.WhiteSmoke;
            tbUsername.PlaceholderText = "Username";
            tbUsername.SelectedText = "";
            tbUsername.ShadowDecoration.CustomizableEdges = customizableEdges6;
            tbUsername.Size = new Size(442, 41);
            tbUsername.TabIndex = 6;
            // 
            // AuthForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(30, 30, 30);
            Controls.Add(button);
            Controls.Add(tbPassword);
            Controls.Add(tbUsername);
            Name = "AuthForm";
            Size = new Size(593, 317);
            Load += loginform_Load;
            ResumeLayout(false);
        }

        #endregion

        private Guna.UI2.WinForms.Guna2Button button;
        private Guna.UI2.WinForms.Guna2TextBox tbPassword;
        private Guna.UI2.WinForms.Guna2TextBox tbUsername;
    }
}
