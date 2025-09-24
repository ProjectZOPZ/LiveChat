using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace LiveChat
{
    public partial class MessageControl : UserControl
    {
        private Label lblUsername;
        private Label lblContent;
        private LinkLabel lblLink;
        private PictureBox picContent;
        private Label lblTimestamp;

        private GraphicsPath bubblePath;

        public MessageControl(string username, string type, string content, string timestamp, bool isSender = false)
        {
            InitializeComponent();

            username ??= string.Empty;
            content ??= string.Empty;
            timestamp ??= string.Empty;

            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            Padding = new Padding(10);
            Margin = new Padding(5);

            BackColor = isSender ? Color.FromArgb(25, 25, 25) : Color.FromArgb(25, 25, 25);
            ForeColor = Color.White;
            DoubleBuffered = true;

            var layout = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 0,
                BackColor = Color.Transparent,
            };
            Controls.Add(layout);

            lblUsername = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                ForeColor = Color.DeepSkyBlue,
                BackColor = Color.Transparent,
                Text = username
            };
            layout.Controls.Add(lblUsername);

            if (IsImageUrl(content))
            {
                picContent = new PictureBox
                {
                    SizeMode = PictureBoxSizeMode.Zoom,
                    MaximumSize = new Size(400, 250),
                    Dock = DockStyle.Fill
                };
                try { picContent.LoadAsync(content); } catch { }
                layout.Controls.Add(picContent);
            }
            else if (IsLink(content))
            {
                lblLink = new LinkLabel
                {
                    AutoSize = true,
                    MaximumSize = new Size(400, 0),
                    Font = new Font("Segoe UI Emoji", 10),
                    Text = content,
                    LinkColor = Color.DeepSkyBlue,
                    BackColor = Color.Transparent
                };
                lblLink.Links.Add(0, content.Length, content);
                lblLink.LinkClicked += (s, e) =>
                {
                    try
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = e.Link.LinkData.ToString(),
                            UseShellExecute = true
                        });
                    }
                    catch { }
                };
                layout.Controls.Add(lblLink);
            }
            else
            {
                lblContent = new Label
                {
                    AutoSize = true,
                    MaximumSize = new Size(400, 0),
                    Font = new Font("Segoe UI Emoji", 10),
                    Text = content,
                    ForeColor = Color.White,
                    BackColor = Color.Transparent,
                };
                layout.Controls.Add(lblContent);
            }

            lblTimestamp = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 7, FontStyle.Italic),
                ForeColor = Color.LightGray,
                BackColor = Color.Transparent,
                Text = timestamp,
                Dock = DockStyle.Bottom
            };
            layout.Controls.Add(lblTimestamp);

            Anchor = isSender ? AnchorStyles.Right : AnchorStyles.Left;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            bubblePath = GetRoundedRectanglePath(ClientRectangle, 15);
            Region = new Region(bubblePath);
        }

        private GraphicsPath GetRoundedRectanglePath(Rectangle rect, int radius)
        {
            int d = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        private bool IsImageUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;
            try
            {
                var uri = new Uri(url);
                string path = uri.AbsolutePath;
                return path.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                       path.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                       path.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                       path.EndsWith(".gif", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private bool IsLink(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            return Regex.IsMatch(text, @"^https?:\/\/", RegexOptions.IgnoreCase);
        }
    }
}
