// Copyright (c) Thomas Gossler. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace AzTagger;

public partial class CustomToolTipForm : Form
{
    private const int ShadowSize = 5;

    private Label _label;

    public CustomToolTipForm()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        _label = new Label();

        SuspendLayout();

        _label.BackColor = Color.FromArgb(249, 249, 249);
        _label.ForeColor = Color.FromArgb(86, 86, 86);
        _label.BorderStyle = BorderStyle.None;
        _label.Padding = new Padding(3);

        Controls.Add(_label);

        AutoScaleMode = AutoScaleMode.Dpi;
        FormBorderStyle = FormBorderStyle.None;
        BackColor = Color.Magenta;
        DoubleBuffered = true;
        ControlBox = false;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowIcon = false;
        ShowInTaskbar = false;
        SizeGripStyle = SizeGripStyle.Hide;
        StartPosition = FormStartPosition.Manual;
        TopMost = true;

        ResumeLayout(false);
    }

    public void ShowToolTip(string text, Point location, Font font = null)
    {
        _label.Text = text;
        font = font ?? SystemFonts.DefaultFont;

        Size toolTipLabelSize = GetPreferredLabelSize(text, font);
        _label.Size = toolTipLabelSize;

        Rectangle tooltipLabelRect = new Rectangle(0, 0, toolTipLabelSize.Width, toolTipLabelSize.Height);
        Size bitmapSize = new Size(
            tooltipLabelRect.Width + ShadowSize,
            tooltipLabelRect.Height + ShadowSize
        );
        ClientSize = bitmapSize;

        using (Bitmap bitmap = new Bitmap(bitmapSize.Width, bitmapSize.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
        {
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                using (GraphicsPath path = GetRoundedRectanglePath(tooltipLabelRect, 2))
                {
                    using (SolidBrush brush = new SolidBrush(_label.BackColor))
                    {
                        g.FillPath(brush, path);
                    }
                    using (Pen pen = new Pen(_label.BackColor))
                    {
                        g.DrawPath(pen, path);
                    }
                }

                Rectangle textRect = new Rectangle(
                    tooltipLabelRect.X + _label.Padding.Left,
                    tooltipLabelRect.Y + _label.Padding.Top,
                    tooltipLabelRect.Width - _label.Padding.Horizontal,
                    tooltipLabelRect.Height - _label.Padding.Vertical
                );

                using (StringFormat stringFormat = new StringFormat())
                {
                    stringFormat.Alignment = StringAlignment.Near;
                    stringFormat.LineAlignment = StringAlignment.Near;
                    stringFormat.Trimming = StringTrimming.EllipsisCharacter;
                    stringFormat.FormatFlags = StringFormatFlags.NoWrap;

                    g.DrawString(text, _label.Font, new SolidBrush(_label.ForeColor), textRect, stringFormat);
                }

                DrawShadow(g, tooltipLabelRect);
            }

            Size cursorSize = SystemInformation.CursorSize;
            Point adjustedLocation = new Point(location.X + cursorSize.Width, location.Y);

            Rectangle screenBounds = Screen.FromPoint(location).WorkingArea;
            if (adjustedLocation.X + bitmapSize.Width > screenBounds.Right)
            {
                adjustedLocation.X = screenBounds.Right - bitmapSize.Width;
                if (adjustedLocation.X < screenBounds.Left)
                {
                    adjustedLocation.X = screenBounds.Left;
                }
                adjustedLocation.Y += cursorSize.Height;
            }

            Location = adjustedLocation;
            UpdateLayeredWindow(bitmap, adjustedLocation);
        }

        Show();
    }

    private void UpdateLayeredWindow(Bitmap bitmap, Point location)
    {
        IntPtr screenDC = GetDC(IntPtr.Zero);
        IntPtr memDC = CreateCompatibleDC(screenDC);
        IntPtr hBitmap = bitmap.GetHbitmap(Color.FromArgb(0));
        IntPtr oldBitmap = SelectObject(memDC, hBitmap);

        Size size = bitmap.Size;
        Point pointSource = new Point(0, 0);

        BLENDFUNCTION blend = new BLENDFUNCTION
        {
            BlendOp = AC_SRC_OVER,
            BlendFlags = 0,
            SourceConstantAlpha = 255,
            AlphaFormat = AC_SRC_ALPHA
        };

        UpdateLayeredWindow(this.Handle, screenDC, ref location, ref size, memDC, ref pointSource, 0, ref blend, ULW_ALPHA);

        SelectObject(memDC, oldBitmap);
        DeleteObject(hBitmap);
        DeleteDC(memDC);
        ReleaseDC(IntPtr.Zero, screenDC);
    }

    private Size GetPreferredLabelSize(string text, Font font = null)
    {
        font = font ?? SystemFonts.DefaultFont;

        using (Bitmap bitmap = new Bitmap(1, 1))
        {
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                SizeF textSize = g.MeasureString(text, font);

                int totalWidth = (int)Math.Ceiling(textSize.Width) + _label.Padding.Horizontal + 2;
                int totalHeight = (int)Math.Ceiling(textSize.Height) + _label.Padding.Vertical + 2;

                return new Size(totalWidth, totalHeight);
            }
        }
    }

    protected override CreateParams CreateParams
    {
        get
        {
            const int WS_EX_NOACTIVATE = 0x08000000;
            const int WS_EX_TOOLWINDOW = 0x00000080;
            const int WS_EX_LAYERED = 0x00080000;

            CreateParams cp = base.CreateParams;
            cp.ExStyle |= WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW | WS_EX_LAYERED;
            return cp;
        }
    }

    protected override bool ShowWithoutActivation => true;

    private void DrawShadow(Graphics g, Rectangle tooltipRect)
    {
        int shadowSize = ShadowSize;

        Rectangle rightShadowRect = new Rectangle(
            tooltipRect.Right,
            tooltipRect.Top + shadowSize,
            shadowSize,
            tooltipRect.Height - shadowSize
        );

        using (LinearGradientBrush brush = new LinearGradientBrush(
            rightShadowRect,
            Color.FromArgb(100, Color.Black),
            Color.Transparent,
            LinearGradientMode.Horizontal))
        {
            g.FillRectangle(brush, rightShadowRect);
        }

        Rectangle bottomShadowRect = new Rectangle(
            tooltipRect.Left + shadowSize,
            tooltipRect.Bottom,
            tooltipRect.Width - shadowSize,
            shadowSize
        );

        using (LinearGradientBrush brush = new LinearGradientBrush(
            bottomShadowRect,
            Color.FromArgb(100, Color.Black),
            Color.Transparent,
            LinearGradientMode.Vertical))
        {
            g.FillRectangle(brush, bottomShadowRect);
        }

        Rectangle cornerShadowRect = new Rectangle(
            tooltipRect.Right - shadowSize / 2,
            tooltipRect.Bottom - shadowSize / 2,
            shadowSize,
            shadowSize
        );

        using (GraphicsPath path = new GraphicsPath())
        {
            path.AddEllipse(cornerShadowRect);
            using (PathGradientBrush brush = new PathGradientBrush(path)
            {
                CenterColor = Color.FromArgb(100, Color.Black),
                SurroundColors = new[] { Color.Transparent }
            })
            {
                g.FillPath(brush, path);
            }
        }
    }

    private GraphicsPath GetRoundedRectanglePath(Rectangle bounds, int radius)
    {
        int diameter = radius * 2;

        GraphicsPath path = new GraphicsPath();
        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);

        path.CloseFigure();
        return path;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _label?.Dispose();
        }
        base.Dispose(disposing);
    }

    private const int ULW_ALPHA = 0x00000002;
    private const byte AC_SRC_OVER = 0x00;
    private const byte AC_SRC_ALPHA = 0x01;

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct BLENDFUNCTION
    {
        public byte BlendOp;
        public byte BlendFlags;
        public byte SourceConstantAlpha;
        public byte AlphaFormat;
    }

    [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
    private static extern bool UpdateLayeredWindow(IntPtr hwnd, IntPtr hdcDst,
        ref Point pptDst, ref Size psize, IntPtr hdcSrc, ref Point pptSrc,
        int crKey, ref BLENDFUNCTION pblend, int dwFlags);

    [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
    private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

    [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
    private static extern bool DeleteDC(IntPtr hdc);

    [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
    private static extern IntPtr GetDC(IntPtr hWnd);

    [DllImport("user32.dll", ExactSpelling = true)]
    private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
    private static extern IntPtr SelectObject(IntPtr hdc, IntPtr h);

    [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
    private static extern bool DeleteObject(IntPtr hObject);
}
