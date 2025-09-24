using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace LiveChat
{
    public partial class RoundedScrollBar : UserControl
    {
        private int thumbHeight = 40;
        private int thumbTop = 0;
        private bool isDragging = false;
        private int dragOffset;

        private int minimum = 0;
        private int maximum = 100;
        private int value = 0;

        [Category("Behavior")]
        [DefaultValue(0)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public int Minimum
        {
            get => minimum;
            set { minimum = value; Invalidate(); }
        }

        [Category("Behavior")]
        [DefaultValue(100)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public int Maximum
        {
            get => maximum;
            set { maximum = value; Invalidate(); }
        }

        [Category("Behavior")]
        [DefaultValue(0)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public int Value
        {
            get => this.value;
            set
            {
                this.value = Math.Max(minimum, Math.Min(maximum, value));
                UpdateThumb();
                Invalidate();
                ValueChanged?.Invoke(this, EventArgs.Empty);
            }
        }


        public event EventHandler ValueChanged;

        public RoundedScrollBar()
        {
            InitializeComponent();
            this.Width = 16;
            this.DoubleBuffered = true;
        }

        private void UpdateThumb()
        {
            if (maximum > minimum)
            {
                thumbTop = (int)((double)(value - minimum) / (maximum - minimum) * (Height - thumbHeight));
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            using (SolidBrush trackBrush = new SolidBrush(Color.LightGray))
            {
                Rectangle trackRect = new Rectangle(Width / 4, 0, Width / 2, Height);
                e.Graphics.FillRoundedRectangle(trackBrush, trackRect, 7);
            }

            using (SolidBrush thumbBrush = new SolidBrush(Color.DodgerBlue))
            {
                Rectangle thumbRect = new Rectangle(2, thumbTop, Width - 4, thumbHeight);
                e.Graphics.FillRoundedRectangle(thumbBrush, thumbRect, 7);
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (new Rectangle(2, thumbTop, Width - 4, thumbHeight).Contains(e.Location))
            {
                isDragging = true;
                dragOffset = e.Y - thumbTop;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (isDragging)
            {
                int newTop = e.Y - dragOffset;
                newTop = Math.Max(0, Math.Min(Height - thumbHeight, newTop));

                thumbTop = newTop;
                value = minimum + (int)((double)thumbTop / (Height - thumbHeight) * (maximum - minimum));

                ValueChanged?.Invoke(this, EventArgs.Empty);
                Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            isDragging = false;
        }
    }

    public static class GraphicsExtensions
    {
        public static void FillRoundedRectangle(this Graphics g, Brush brush, Rectangle rect, int radius)
        {
            using (GraphicsPath path = RoundedRect(rect, radius))
            {
                g.FillPath(brush, path);
            }
        }

        private static GraphicsPath RoundedRect(Rectangle rect, int radius)
        {
            int d = radius * 2;
            GraphicsPath path = new GraphicsPath();
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
