using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PgKeeper
{
    static class Theme
    {
        public static readonly Color Back = Color.FromArgb(241, 245, 249);
        public static readonly Color Surface = Color.White;
        public static readonly Color Border = Color.FromArgb(226, 232, 240);
        public static readonly Color Text = Color.FromArgb(15, 23, 42);
        public static readonly Color Muted = Color.FromArgb(100, 116, 139);
        public static readonly Color Primary = Color.FromArgb(37, 99, 235);
        public static readonly Color PrimaryDark = Color.FromArgb(29, 78, 216);
        public static readonly Color Green = Color.FromArgb(22, 163, 74);
        public static readonly Color Red = Color.FromArgb(220, 38, 38);

        public static Button Button(string text, Action click, bool primary = false)
        {
            var b = new Button
            {
                Text = text,
                AutoSize = true,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Padding = new Padding(10, 3, 10, 3),
                Margin = new Padding(4),
                BackColor = primary ? Primary : Surface,
                ForeColor = primary ? Color.White : Text,
                UseVisualStyleBackColor = false,
            };
            b.FlatAppearance.BorderColor = primary ? Primary : Color.FromArgb(203, 213, 225);
            b.FlatAppearance.MouseOverBackColor = primary ? PrimaryDark : Back;
            b.FlatAppearance.MouseDownBackColor = primary ? PrimaryDark : Border;
            b.Click += (o, e) => click();
            return b;
        }
    }

    class Card : Panel
    {
        public readonly TableLayoutPanel Body;

        public Card(string title, bool fill = false)
        {
            BackColor = Theme.Surface;
            DoubleBuffered = true;
            ResizeRedraw = true;
            Padding = new Padding(16, 12, 16, 12);
            Margin = new Padding(6);
            Dock = DockStyle.Fill;
            AutoSize = !fill;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            Body = new TableLayoutPanel { ColumnCount = 1, BackColor = Theme.Surface, Dock = fill ? DockStyle.Fill : DockStyle.Top, AutoSize = !fill };
            Body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            if (title != null)
            {
                Body.Controls.Add(new Label { Text = title, AutoSize = true, ForeColor = Theme.Text, Font = new Font("Segoe UI", 11f, FontStyle.Bold), Margin = new Padding(0, 0, 0, 6) });
                Body.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            }
            Controls.Add(Body);
        }

        public void Add(Control control, bool grow = false)
        {
            control.Dock = DockStyle.Fill;
            Body.Controls.Add(control);
            Body.RowStyles.Add(grow ? new RowStyle(SizeType.Percent, 100) : new RowStyle(SizeType.AutoSize));
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Parent != null ? Parent.BackColor : Theme.Back);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            const float r = 10f;
            float w = Width - 1.5f, h = Height - 1.5f;
            using (var path = new GraphicsPath())
            using (var brush = new SolidBrush(BackColor))
            using (var pen = new Pen(Theme.Border))
            {
                path.AddArc(0.5f, 0.5f, r * 2, r * 2, 180, 90);
                path.AddArc(w - r * 2, 0.5f, r * 2, r * 2, 270, 90);
                path.AddArc(w - r * 2, h - r * 2, r * 2, r * 2, 0, 90);
                path.AddArc(0.5f, h - r * 2, r * 2, r * 2, 90, 90);
                path.CloseFigure();
                g.FillPath(brush, path);
                g.DrawPath(pen, path);
            }
        }
    }

    class Header : Panel
    {
        public Header(Control picker)
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
            Dock = DockStyle.Top;
            Height = 78;
            Padding = new Padding(18, 12, 18, 12);

            var grid = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 2, BackColor = Color.Transparent };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            var icon = new PictureBox { Image = Program.AppIcon(48).ToBitmap(), SizeMode = PictureBoxSizeMode.Zoom, Size = new Size(48, 48), BackColor = Color.Transparent, Margin = new Padding(0, 0, 12, 0) };
            grid.Controls.Add(icon, 0, 0);
            grid.SetRowSpan(icon, 2);
            grid.Controls.Add(new Label { Text = "PgKeeper", AutoSize = true, ForeColor = Color.White, BackColor = Color.Transparent, Font = new Font("Segoe UI", 15f, FontStyle.Bold), Margin = new Padding(0) }, 1, 0);
            grid.Controls.Add(new Label { Text = L.Get("subtitle"), AutoSize = true, ForeColor = Color.FromArgb(219, 234, 254), BackColor = Color.Transparent, Margin = new Padding(2, 0, 0, 0) }, 1, 1);
            picker.Anchor = AnchorStyles.Right;
            grid.Controls.Add(picker, 2, 0);
            grid.SetRowSpan(picker, 2);
            Controls.Add(grid);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            if (Width <= 0 || Height <= 0) return;
            using (var brush = new LinearGradientBrush(ClientRectangle, Color.FromArgb(30, 58, 138), Theme.Primary, LinearGradientMode.Horizontal))
                e.Graphics.FillRectangle(brush, ClientRectangle);
        }
    }
}
