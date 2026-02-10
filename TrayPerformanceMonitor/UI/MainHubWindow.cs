// -----------------------------------------------------------------------
// <copyright file="MainHubWindow.cs" company="TrayPerformanceMonitor">
//     Copyright (c) TrayPerformanceMonitor. All rights reserved.
// </copyright>
// <summary>
//     Provides the main application hub window shown when the user clicks
//     the desktop shortcut or double-clicks the tray icon.
// </summary>
// -----------------------------------------------------------------------

namespace TrayPerformanceMonitor.UI
{
    /// <summary>
    /// A dark-themed main hub window that gives the user quick access to all
    /// application features: Settings, View Logs, Show Performance, and Exit.
    /// </summary>
    /// <remarks>
    /// This window is shown when the running instance receives a signal from a
    /// second launch attempt, or when the user double-clicks the tray icon.
    /// It acts as the central landing page for the application.
    /// </remarks>
    internal sealed class MainHubWindow : Form
    {
        // ── Color palette (matches LogViewerWindow) ─────────────────────
        private static readonly Color BackgroundColor = Color.FromArgb(18, 18, 18);
        private static readonly Color PanelColor = Color.FromArgb(26, 26, 26);
        private static readonly Color HeaderColor = Color.FromArgb(32, 32, 32);
        private static readonly Color AccentColor = Color.FromArgb(255, 200, 0);
        private static readonly Color TextColor = Color.FromArgb(220, 220, 210);
        private static readonly Color DimTextColor = Color.FromArgb(130, 130, 120);
        private static readonly Color BorderColor = Color.FromArgb(55, 55, 50);
        private static readonly Color ButtonBgColor = Color.FromArgb(36, 36, 32);
        private static readonly Color ButtonHoverColor = Color.FromArgb(50, 48, 40);
        private static readonly Color ButtonPressColor = Color.FromArgb(40, 38, 30);

        private readonly Action _onSettings;
        private readonly Action _onViewLogs;
        private readonly Action _onShowPerformance;
        private readonly Action _onExit;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainHubWindow"/> class.
        /// </summary>
        /// <param name="onSettings">Action to open the Settings dialog.</param>
        /// <param name="onViewLogs">Action to open the Log Viewer window.</param>
        /// <param name="onShowPerformance">Action to show the performance dialog.</param>
        /// <param name="onExit">Action to exit the application.</param>
        public MainHubWindow(Action onSettings, Action onViewLogs, Action onShowPerformance, Action onExit)
        {
            ArgumentNullException.ThrowIfNull(onSettings);
            ArgumentNullException.ThrowIfNull(onViewLogs);
            ArgumentNullException.ThrowIfNull(onShowPerformance);
            ArgumentNullException.ThrowIfNull(onExit);

            _onSettings = onSettings;
            _onViewLogs = onViewLogs;
            _onShowPerformance = onShowPerformance;
            _onExit = onExit;

            InitializeForm();
            BuildLayout();
        }

        /// <summary>
        /// Configures basic form properties.
        /// </summary>
        private void InitializeForm()
        {
            Text = "⚡ TrayPerformanceMonitor";
            Size = new Size(420, 400);
            MinimumSize = new Size(420, 400);
            MaximumSize = new Size(420, 400);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            BackColor = BackgroundColor;
            ForeColor = TextColor;
            Font = new Font("Segoe UI", 10f);
            ShowInTaskbar = true;
            KeyPreview = true;
            Icon = LoadApplicationIcon();

            KeyDown += (_, e) =>
            {
                if (e.KeyCode == Keys.Escape)
                {
                    Close();
                }
            };
        }

        /// <summary>
        /// Builds the full visual layout of the hub window.
        /// </summary>
        private void BuildLayout()
        {
            // ── Header panel ────────────────────────────────────────────
            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                BackColor = HeaderColor
            };

            var headerBorder = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 2,
                BackColor = AccentColor
            };
            header.Controls.Add(headerBorder);

            var titleLabel = new Label
            {
                Text = "⚡ PERFORMANCE MONITOR",
                Font = new Font("Consolas", 15f, FontStyle.Bold),
                ForeColor = AccentColor,
                AutoSize = true,
                Location = new Point(24, 14),
                BackColor = Color.Transparent
            };
            header.Controls.Add(titleLabel);

            var subtitleLabel = new Label
            {
                Text = "Intelligent System Resource Monitoring",
                Font = new Font("Segoe UI", 9f),
                ForeColor = DimTextColor,
                AutoSize = true,
                Location = new Point(26, 48),
                BackColor = Color.Transparent
            };
            header.Controls.Add(subtitleLabel);

            Controls.Add(header);

            // ── Button area ─────────────────────────────────────────────
            var buttonPanel = new Panel
            {
                Location = new Point(0, 80),
                Size = new Size(420, 320),
                BackColor = BackgroundColor,
                Padding = new Padding(28, 20, 28, 16)
            };

            var yPos = 20;
            const int buttonHeight = 52;
            const int spacing = 12;
            const int buttonWidth = 350;

            // ── View Logs button (primary action) ───────────────────────
            var logsButton = CreateHubButton(
                "📋  View Logs",
                "Open the performance spike log viewer",
                new Point(28, yPos),
                new Size(buttonWidth, buttonHeight),
                isPrimary: true);
            logsButton.Click += (_, _) => { _onViewLogs(); Close(); };
            buttonPanel.Controls.Add(logsButton);
            yPos += buttonHeight + spacing;

            // ── Settings button ─────────────────────────────────────────
            var settingsButton = CreateHubButton(
                "⚙️  Settings",
                "Configure AI model and process count",
                new Point(28, yPos),
                new Size(buttonWidth, buttonHeight));
            settingsButton.Click += (_, _) => { _onSettings(); Close(); };
            buttonPanel.Controls.Add(settingsButton);
            yPos += buttonHeight + spacing;

            // ── Show Performance button ─────────────────────────────────
            var perfButton = CreateHubButton(
                "📊  Show Performance",
                "View current CPU and RAM usage",
                new Point(28, yPos),
                new Size(buttonWidth, buttonHeight));
            perfButton.Click += (_, _) => { _onShowPerformance(); Close(); };
            buttonPanel.Controls.Add(perfButton);
            yPos += buttonHeight + spacing;

            // ── Exit button ─────────────────────────────────────────────
            var exitButton = CreateHubButton(
                "🚪  Exit Application",
                "Stop monitoring and close the app",
                new Point(28, yPos),
                new Size(buttonWidth, buttonHeight));
            exitButton.Click += (_, _) => { Close(); _onExit(); };
            buttonPanel.Controls.Add(exitButton);

            Controls.Add(buttonPanel);

            // ── Footer ──────────────────────────────────────────────────
            var footer = new Label
            {
                Text = "Running in system tray  •  This window can be closed safely",
                Font = new Font("Segoe UI", 8f),
                ForeColor = Color.FromArgb(80, 80, 75),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Bottom,
                Height = 28,
                BackColor = HeaderColor
            };
            Controls.Add(footer);
        }

        /// <summary>
        /// Creates a styled hub button with a title and description.
        /// </summary>
        private static Button CreateHubButton(string text, string description, Point location, Size size, bool isPrimary = false)
        {
            var btn = new Button
            {
                Text = text,
                Location = location,
                Size = size,
                FlatStyle = FlatStyle.Flat,
                BackColor = isPrimary ? Color.FromArgb(40, 38, 10) : ButtonBgColor,
                ForeColor = isPrimary ? AccentColor : TextColor,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(16, 0, 0, 0)
            };

            btn.FlatAppearance.BorderColor = isPrimary ? AccentColor : BorderColor;
            btn.FlatAppearance.BorderSize = isPrimary ? 2 : 1;
            btn.FlatAppearance.MouseOverBackColor = ButtonHoverColor;
            btn.FlatAppearance.MouseDownBackColor = ButtonPressColor;

            // Tooltip with description
            var tt = new ToolTip
            {
                BackColor = PanelColor,
                ForeColor = TextColor
            };
            tt.SetToolTip(btn, description);

            return btn;
        }

        /// <summary>
        /// Loads the application icon.
        /// </summary>
        private static Icon? LoadApplicationIcon()
        {
            try
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                using var stream = assembly.GetManifestResourceStream("TrayPerformanceMonitor.app.ico");
                if (stream != null)
                {
                    return new Icon(stream);
                }
            }
            catch
            {
                // Fall back
            }

            try
            {
                return Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch
            {
                return null;
            }
        }
    }
}
