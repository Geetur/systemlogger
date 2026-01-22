using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace TrayPerformanceMonitor
{
    // Small always-on-top transparent status window positioned left of the system tray
    internal class StatusWindow : Form
    {
        private readonly Label cpuLabel;
        private readonly Label ramLabel;

        // Keep the window pinned on top (some windows can temporarily cover TopMost windows)
        private readonly System.Windows.Forms.Timer keepPinnedTimer;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string? lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string? lpszWindow);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        // Force Z-order to TopMost without stealing focus
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(
            IntPtr hWnd,
            IntPtr hWndInsertAfter,
            int X,
            int Y,
            int cx,
            int cy,
            uint uFlags
        );

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);

        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_SHOWWINDOW = 0x0040;

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT { public int left, top, right, bottom; }

        public StatusWindow(Action onShow, Action onExit)
        {
            StartPosition = FormStartPosition.Manual;
            FormBorderStyle = FormBorderStyle.None;

            // don't create a taskbar button
            ShowInTaskbar = false;

            // Start as always-on-top
            TopMost = true;

            // transparent background using a transparency key color
            BackColor = Color.Lime;
            TransparencyKey = Color.Lime;
            Opacity = 1.0;

            Width = 170;
            Height = 22;

            // reduce flicker
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);

            cpuLabel = new Label
            {
                AutoSize = false,
                Width = 80,
                Dock = DockStyle.Left,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.Black,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 8, FontStyle.Regular),
                Padding = new Padding(2)
            };

            ramLabel = new Label
            {
                AutoSize = false,
                Width = 90,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.Black,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 8, FontStyle.Regular),
                Padding = new Padding(2)
            };

            Controls.Add(ramLabel);
            Controls.Add(cpuLabel);

            // Try to position just to the left of the notification area (system tray)
            int x = 8, y = 8;
            try
            {
                var shell = FindWindow("Shell_TrayWnd", null);
                if (shell != IntPtr.Zero)
                {
                    var tray = FindWindowEx(shell, IntPtr.Zero, "TrayNotifyWnd", null);
                    if (tray == IntPtr.Zero)
                    {
                        tray = FindWindowEx(shell, IntPtr.Zero, "NotificationAreaWindow", null);
                    }

                    if (tray != IntPtr.Zero && GetWindowRect(tray, out RECT r))
                    {
                        x = r.left - Width - 6;
                        y = r.top + ((r.bottom - r.top) - Height) / 2;
                    }
                    else
                    {
                        var wa = Screen.PrimaryScreen.Bounds;
                        x = wa.Right - Width - 8;
                        y = wa.Bottom - Height - 8;
                    }
                }
            }
            catch
            {
                var wa = Screen.PrimaryScreen.Bounds;
                x = wa.Right - Width - 8;
                y = wa.Bottom - Height - 8;
            }

            Location = new Point(x, y);

            var ctx = new ContextMenuStrip();
            ctx.Items.Add("Show Performance").Click += (_, __) => onShow();
            ctx.Items.Add("Exit").Click += (_, __) => onExit();
            ContextMenuStrip = ctx;

            MouseUp += (s, e) =>
            {
                if (e.Button == MouseButtons.Right)
                    ContextMenuStrip.Show(this, e.Location);
            };

            // Re-assert "always visible / always on top" without stealing focus
            keepPinnedTimer = new System.Windows.Forms.Timer { Interval = 250 };
            keepPinnedTimer.Tick += (_, __) => EnsurePinned();
            keepPinnedTimer.Start();
        }

        // Show without stealing focus
        protected override bool ShowWithoutActivation => true;

        // Force visible + TopMost (without activation)
        public void EnsurePinned()
        {
            if (IsDisposed) return;

            try
            {
                if (!Visible) Show();
                if (WindowState == FormWindowState.Minimized)
                    WindowState = FormWindowState.Normal;

                TopMost = true;

                if (IsHandleCreated)
                {
                    SetWindowPos(
                        Handle,
                        HWND_TOPMOST,
                        0, 0, 0, 0,
                        SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW
                    );
                }
            }
            catch
            {
                // don't crash if something goes wrong
            }
        }

        public void UpdateStatus(float cpu, float ram)
        {
            try
            {
                cpuLabel.Text = $"CPU {cpu:F0}%";
                ramLabel.Text = $"RAM {ram:F0}%";
            }
            catch { }

            EnsurePinned(); // helps if another window briefly covered it
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            EnsurePinned();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try { keepPinnedTimer?.Stop(); } catch { }
                try { keepPinnedTimer?.Dispose(); } catch { }
            }
            base.Dispose(disposing);
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= 0x00000080;  // WS_EX_TOOLWINDOW
                cp.ExStyle |= 0x00080000;  // WS_EX_LAYERED
                cp.ExStyle |= 0x08000000;  // WS_EX_NOACTIVATE (prevents focus stealing)
                return cp;
            }
        }
    }
}
