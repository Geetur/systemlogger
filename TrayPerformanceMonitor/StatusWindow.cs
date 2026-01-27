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

            // transparent background
            BackColor = Color.Lime;
            TransparencyKey = Color.Lime;
            Opacity = 1.0;

            Width = 160;
            Height = 52;

            // reduce flicker
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);

            cpuLabel = new Label
            {
                AutoSize = false,
                Height = 24,
                Dock = DockStyle.Top,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.LimeGreen,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Padding = new Padding(6, 0, 4, 0)
            };

            ramLabel = new Label
            {
                AutoSize = false,
                Height = 24,
                Dock = DockStyle.Top,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.LimeGreen,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Padding = new Padding(6, 0, 4, 0)
            };

            Controls.Add(ramLabel);
            Controls.Add(cpuLabel);

            // Position inside the taskbar (left side, near widgets/weather)
            int x = 8, y = 8;
            try
            {
                var taskbar = FindWindow("Shell_TrayWnd", null);
                if (taskbar != IntPtr.Zero && GetWindowRect(taskbar, out RECT r))
                {
                    // place inside taskbar bounds, to the right of the widgets/weather area
                    x = r.left + 240;
                    y = r.top + ((r.bottom - r.top) - Height) / 2;
                }
                else
                {
                    var screen = Screen.PrimaryScreen ?? (Screen.AllScreens.Length > 0 ? Screen.AllScreens[0] : null);
                    if (screen != null)
                    {
                        var wa = screen.WorkingArea;
                        x = wa.Left + 8;
                        y = wa.Bottom - Height - 8;
                    }
                }
            }
            catch
            {
                var screen = Screen.PrimaryScreen ?? (Screen.AllScreens.Length > 0 ? Screen.AllScreens[0] : null);
                if (screen != null)
                {
                    var wa = screen.WorkingArea;
                    x = wa.Left + 8;
                    y = wa.Bottom - Height - 8;
                }
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
