using System;
using System.ComponentModel;
using System.Drawing;
using System.Text.Json.Serialization;
using System.Windows.Forms;

namespace LiveChat
{
    public partial class GroupItemControl : UserControl
    {
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [JsonIgnore]
        public string GroupId { get; set; }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [JsonIgnore]
        public string InviteCode { get; set; }

        public event EventHandler OnGroupSelected;

        public GroupItemControl()
        {
            InitializeComponent();
        }

        public GroupItemControl(string groupName, string groupId, string inviteCode)
            : this()
        {
            GroupId = groupId;
            InviteCode = inviteCode;

            BuildUi(groupName ?? string.Empty, inviteCode ?? string.Empty);
        }

        private void BuildUi(string groupName, string inviteCode)
        {
            // Dark mode
            Height = 60;
            BackColor = Color.FromArgb(25, 25, 25);
            Margin = new Padding(0, 0, 0, 1);

            // Avatar circle
            var avatarPanel = new Panel
            {
                Width = 40,
                Height = 40,
                Left = 10,
                Top = 10,
                BackColor = Color.FromArgb(30,30,30)
            };
            avatarPanel.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using (var brush = new SolidBrush(avatarPanel.BackColor))
                    e.Graphics.FillEllipse(brush, 0, 0, avatarPanel.Width, avatarPanel.Height);

                string initial = !string.IsNullOrEmpty(groupName) ? groupName.Substring(0, 1).ToUpper() : "?";
                using (var f = new Font("Segoe UI", 12, FontStyle.Bold))
                {
                    var size = e.Graphics.MeasureString(initial, f);
                    e.Graphics.DrawString(initial, f, Brushes.White,
                        (avatarPanel.Width - size.Width) / 2, (avatarPanel.Height - size.Height) / 2);
                }
            };

            // Group name label
            var lblName = new Label
            {
                Text = groupName,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Left = 60,
                Top = 10
            };

            // Invite code label
            var lblInvite = new Label
            {
                Text = string.IsNullOrEmpty(inviteCode) ? "" : $"Invite: {inviteCode}",
                Font = new Font("Segoe UI", 8, FontStyle.Regular),
                ForeColor = Color.LightGray,
                AutoSize = true,
                Left = 60,
                Top = 30
            };

            Controls.Add(avatarPanel);
            Controls.Add(lblName);
            Controls.Add(lblInvite);

            // Hover effect for dark mode
            MouseEnter += (s, e) => BackColor = Color.FromArgb(45, 45, 45);
            MouseLeave += (s, e) => BackColor = Color.FromArgb(25, 25, 25);

            // Click forwarding
            void RaiseSelected() => OnGroupSelected?.Invoke(this, EventArgs.Empty);

            Click += (s, e) => RaiseSelected();
            avatarPanel.Click += (s, e) => RaiseSelected();
            lblName.Click += (s, e) => RaiseSelected();
            lblInvite.Click += (s, e) => RaiseSelected();
        }
    }
}
