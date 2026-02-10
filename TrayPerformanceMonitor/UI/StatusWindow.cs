// -----------------------------------------------------------------------
// <copyright file="StatusWindow.cs" company="TrayPerformanceMonitor">
//     Copyright (c) TrayPerformanceMonitor. All rights reserved.
// </copyright>
// <summary>
//     Provides a small, always-on-top transparent status window for displaying
//     performance metrics near the system tray.
// </summary>
// -----------------------------------------------------------------------

using System.Runtime.InteropServices;
using TrayPerformanceMonitor.Configuration;

namespace TrayPerformanceMonitor.UI
{
    /// <summary>
    /// A small, always-on-top transparent status window that displays CPU and RAM usage
    /// metrics positioned near the system tray area.
    /// </summary>
    /// <remarks>
    /// This window uses Windows-specific features to remain visible above other windows
    /// without stealing focus. It positions itself inside or near the taskbar area
    /// and provides a context menu for accessing application functions.
    /// </remarks>
    internal sealed class StatusWindow : Form
    {
        private readonly Label _cpuLabel;
        private readonly Label _ramLabel;
        private readonly System.Windows.Forms.Timer _keepPinnedTimer;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="StatusWindow"/> class.
        /// </summary>
        /// <param name="onShowPerformance">Action to execute when "Show Performance" is clicked.</param>
        /// <param name="onExit">Action to execute when "Exit" is clicked.</param>
        /// <param name="onSettings">Action to execute when "Settings" is clicked. Can be null if not supported.</param>
        /// <param name="onViewLogs">Action to execute when "View Logs" is clicked. Can be null if not supported.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="onShowPerformance"/> or <paramref name="onExit"/> is null.</exception>
        public StatusWindow(Action onShowPerformance, Action onExit, Action? onSettings = null, Action? onViewLogs = null)
        {
            ArgumentNullException.ThrowIfNull(onShowPerformance);
            ArgumentNullException.ThrowIfNull(onExit);

            InitializeWindowProperties();
            (_cpuLabel, _ramLabel) = CreateLabels();
            SetWindowPosition();
            SetupContextMenu(onShowPerformance, onExit, onSettings, onViewLogs);
            _keepPinnedTimer = SetupKeepPinnedTimer();
        }

        /// <summary>
        /// Gets a value indicating whether the window should be shown without activation.
        /// </summary>
        protected override bool ShowWithoutActivation => true;

        /// <summary>
        /// Gets the creation parameters for the window, configuring it as a tool window
        /// that doesn't appear in the taskbar and doesn't steal focus.
        /// </summary>
        protected override CreateParams CreateParams
        {
            get
            {
                const int WS_EX_TOOLWINDOW = 0x00000080;
                const int WS_EX_LAYERED = 0x00080000;
                const int WS_EX_NOACTIVATE = 0x08000000;

                var createParams = base.CreateParams;
                createParams.ExStyle |= WS_EX_TOOLWINDOW;   // Tool window (no taskbar button)
                createParams.ExStyle |= WS_EX_LAYERED;      // Layered window (for transparency)
                createParams.ExStyle |= WS_EX_NOACTIVATE;   // Prevents focus stealing
                return createParams;
            }
        }

        /// <summary>
        /// Updates the displayed CPU and RAM values.
        /// </summary>
        /// <param name="cpuPercent">The current CPU usage percentage.</param>
        /// <param name="ramPercent">The current RAM usage percentage.</param>
        public void UpdateStatus(float cpuPercent, float ramPercent)
        {
            if (_disposed)
            {
                return;
            }

            try
            {
                _cpuLabel.Text = $"CPU {cpuPercent:F0}%";
                _ramLabel.Text = $"RAM {ramPercent:F0}%";
            }
            catch (ObjectDisposedException)
            {
                // Window has been disposed
            }

            // Re-pin to ensure visibility if another window briefly covered it
            EnsurePinned();
        }

        /// <summary>
        /// Gets or sets a value indicating whether pinning should be temporarily suspended.
        /// Used when modal dialogs are open to prevent focus stealing.
        /// </summary>
        public bool SuspendPinning { get; set; }

        /// <summary>
        /// Ensures the window remains visible and on top without stealing focus.
        /// </summary>
        public void EnsurePinned()
        {
            if (_disposed || IsDisposed || SuspendPinning)
            {
                return;
            }

            // Don't pin if there's a modal dialog open (like settings)
            if (Application.OpenForms.Cast<Form>().Any(f => f.Modal && f.Visible))
            {
                return;
            }

            try
            {
                if (!Visible)
                {
                    Show();
                }

                if (WindowState == FormWindowState.Minimized)
                {
                    WindowState = FormWindowState.Normal;
                }

                TopMost = true;

                if (IsHandleCreated)
                {
                    NativeMethods.SetWindowPos(
                        Handle,
                        NativeMethods.HWND_TOPMOST,
                        0, 0, 0, 0,
                        NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE |
                        NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_SHOWWINDOW);
                }
            }
            catch (Exception)
            {
                // Don't crash if window operations fail
            }
        }

        /// <inheritdoc/>
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            EnsurePinned();
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                try
                {
                    _keepPinnedTimer?.Stop();
                    _keepPinnedTimer?.Dispose();
                }
                catch (Exception)
                {
                    // Ignore disposal errors
                }
            }

            _disposed = true;
            base.Dispose(disposing);
        }

        /// <summary>
        /// Initializes the window's basic properties.
        /// </summary>
        private void InitializeWindowProperties()
        {
            StartPosition = FormStartPosition.Manual;
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;

            // Transparent background using color key
            BackColor = Color.Lime;
            TransparencyKey = Color.Lime;
            Opacity = 1.0;

            Width = 160;
            Height = 52;

            // Reduce flicker with double buffering
            SetStyle(
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint,
                true);
        }

        /// <summary>
        /// Creates and configures the CPU and RAM labels.
        /// </summary>
        /// <returns>A tuple containing the CPU and RAM labels.</returns>
        private (Label cpuLabel, Label ramLabel) CreateLabels()
        {
            var labelFont = new Font("Segoe UI", 10, FontStyle.Bold);

            var cpuLabel = new Label
            {
                AutoSize = false,
                Height = 24,
                Dock = DockStyle.Top,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.LimeGreen,
                BackColor = Color.Transparent,
                Font = labelFont,
                Padding = new Padding(6, 0, 4, 0)
            };

            var ramLabel = new Label
            {
                AutoSize = false,
                Height = 24,
                Dock = DockStyle.Top,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.LimeGreen,
                BackColor = Color.Transparent,
                Font = labelFont,
                Padding = new Padding(6, 0, 4, 0)
            };

            // Add in reverse order due to Dock.Top stacking
            Controls.Add(ramLabel);
            Controls.Add(cpuLabel);

            return (cpuLabel, ramLabel);
        }

        /// <summary>
        /// Sets the window position near the taskbar/system tray area.
        /// </summary>
        private void SetWindowPosition()
        {
            var (x, y) = CalculateWindowPosition();
            Location = new Point(x, y);
        }

        /// <summary>
        /// Calculates the optimal window position based on the taskbar location.
        /// </summary>
        /// <returns>The X and Y coordinates for the window.</returns>
        private (int x, int y) CalculateWindowPosition()
        {
            try
            {
                var taskbarHandle = NativeMethods.FindWindow("Shell_TrayWnd", null);

                if (taskbarHandle != IntPtr.Zero &&
                    NativeMethods.GetWindowRect(taskbarHandle, out var taskbarRect))
                {
                    // Position inside taskbar bounds, to the right of the widgets/weather area
                    int x = taskbarRect.Left + 240;
                    int y = taskbarRect.Top + ((taskbarRect.Bottom - taskbarRect.Top) - Height) / 2;
                    return (x, y);
                }
            }
            catch (Exception)
            {
                // Fall through to screen-based positioning
            }

            // Fallback: position in bottom-left of working area
            return GetFallbackPosition();
        }

        /// <summary>
        /// Gets a fallback position when the taskbar cannot be located.
        /// </summary>
        /// <returns>The X and Y coordinates for the fallback position.</returns>
        private (int x, int y) GetFallbackPosition()
        {
            var screen = Screen.PrimaryScreen ?? (Screen.AllScreens.Length > 0 ? Screen.AllScreens[0] : null);

            if (screen != null)
            {
                var workingArea = screen.WorkingArea;
                return (workingArea.Left + 8, workingArea.Bottom - Height - 8);
            }

            return (8, 8);
        }

        /// <summary>
        /// Sets up the context menu for the window.
        /// </summary>
        /// <param name="onShowPerformance">Action for the "Show Performance" menu item.</param>
        /// <param name="onExit">Action for the "Exit" menu item.</param>
        /// <param name="onSettings">Action for the "Settings" menu item. Can be null.</param>
        /// <param name="onViewLogs">Action for the "View Logs" menu item. Can be null.</param>
        private void SetupContextMenu(Action onShowPerformance, Action onExit, Action? onSettings, Action? onViewLogs)
        {
            var contextMenu = new ContextMenuStrip();

            var showMenuItem = contextMenu.Items.Add("Show Performance");
            showMenuItem.Click += (_, _) => onShowPerformance();

            if (onSettings != null)
            {
                var settingsMenuItem = contextMenu.Items.Add("Settings");
                settingsMenuItem.Click += (_, _) => onSettings();
            }

            if (onViewLogs != null)
            {
                var logsMenuItem = contextMenu.Items.Add("View Logs");
                logsMenuItem.Click += (_, _) => onViewLogs();
            }

            contextMenu.Items.Add(new ToolStripSeparator());

            var exitMenuItem = contextMenu.Items.Add("Exit");
            exitMenuItem.Click += (_, _) => onExit();

            ContextMenuStrip = contextMenu;

            MouseUp += (_, e) =>
            {
                if (e.Button == MouseButtons.Right)
                {
                    ContextMenuStrip.Show(this, e.Location);
                }
            };
        }

        /// <summary>
        /// Sets up and starts the timer that keeps the window pinned on top.
        /// </summary>
        /// <returns>The configured timer.</returns>
        private System.Windows.Forms.Timer SetupKeepPinnedTimer()
        {
            var timer = new System.Windows.Forms.Timer
            {
                Interval = AppConfiguration.KeepPinnedIntervalMs
            };

            timer.Tick += (_, _) => EnsurePinned();
            timer.Start();

            return timer;
        }

        /// <summary>
        /// Contains P/Invoke declarations for native Windows APIs used for window management.
        /// </summary>
        private static class NativeMethods
        {
            /// <summary>Handle value for topmost window positioning.</summary>
            public static readonly IntPtr HWND_TOPMOST = new(-1);

            /// <summary>Retains the current size (ignores cx and cy parameters).</summary>
            public const uint SWP_NOSIZE = 0x0001;

            /// <summary>Retains the current position (ignores X and Y parameters).</summary>
            public const uint SWP_NOMOVE = 0x0002;

            /// <summary>Does not activate the window.</summary>
            public const uint SWP_NOACTIVATE = 0x0010;

            /// <summary>Displays the window.</summary>
            public const uint SWP_SHOWWINDOW = 0x0040;

            /// <summary>
            /// Retrieves a handle to a window whose class name and window name match the specified strings.
            /// </summary>
            /// <param name="lpClassName">The class name.</param>
            /// <param name="lpWindowName">The window name.</param>
            /// <returns>A handle to the window, or IntPtr.Zero if not found.</returns>
            [DllImport("user32.dll", SetLastError = true)]
            public static extern IntPtr FindWindow(string lpClassName, string? lpWindowName);

            /// <summary>
            /// Retrieves the dimensions of the bounding rectangle of the specified window.
            /// </summary>
            /// <param name="hWnd">A handle to the window.</param>
            /// <param name="lpRect">A pointer to a RECT structure that receives the screen coordinates.</param>
            /// <returns>True if the function succeeds; otherwise, false.</returns>
            [DllImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

            /// <summary>
            /// Changes the size, position, and Z order of a window.
            /// </summary>
            /// <param name="hWnd">A handle to the window.</param>
            /// <param name="hWndInsertAfter">A handle to the window to precede the positioned window in the Z order.</param>
            /// <param name="x">The new position of the left side of the window.</param>
            /// <param name="y">The new position of the top of the window.</param>
            /// <param name="cx">The new width of the window.</param>
            /// <param name="cy">The new height of the window.</param>
            /// <param name="uFlags">The window sizing and positioning flags.</param>
            /// <returns>True if the function succeeds; otherwise, false.</returns>
            [DllImport("user32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool SetWindowPos(
                IntPtr hWnd,
                IntPtr hWndInsertAfter,
                int x,
                int y,
                int cx,
                int cy,
                uint uFlags);

            /// <summary>
            /// Defines the coordinates of the upper-left and lower-right corners of a rectangle.
            /// </summary>
            [StructLayout(LayoutKind.Sequential)]
            public struct RECT
            {
                /// <summary>The x-coordinate of the upper-left corner.</summary>
                public int Left;

                /// <summary>The y-coordinate of the upper-left corner.</summary>
                public int Top;

                /// <summary>The x-coordinate of the lower-right corner.</summary>
                public int Right;

                /// <summary>The y-coordinate of the lower-right corner.</summary>
                public int Bottom;
            }
        }
    }
}
