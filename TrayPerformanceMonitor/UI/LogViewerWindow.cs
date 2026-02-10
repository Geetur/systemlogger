// -----------------------------------------------------------------------
// <copyright file="LogViewerWindow.cs" company="TrayPerformanceMonitor">
//     Copyright (c) TrayPerformanceMonitor. All rights reserved.
// </copyright>
// <summary>
//     Provides a terminal-style log viewer window with auto-refresh capability.
// </summary>
// -----------------------------------------------------------------------

using TrayPerformanceMonitor.Configuration;

namespace TrayPerformanceMonitor.UI
{
    /// <summary>
    /// A dark, terminal-style log viewer window that displays the contents of
    /// the performance log file with syntax highlighting, auto-refresh, and
    /// search functionality.
    /// </summary>
    /// <remarks>
    /// The viewer reads the log file with shared access so it can display
    /// live updates while the logging service continues writing. The visual
    /// style uses a black background with yellow/amber accents inspired by
    /// classic terminal aesthetics.
    /// </remarks>
    internal sealed class LogViewerWindow : Form
    {
        // ── Color palette (terminal / amber theme) ──────────────────────
        private static readonly Color BackgroundColor = Color.FromArgb(18, 18, 18);
        private static readonly Color PanelColor = Color.FromArgb(26, 26, 26);
        private static readonly Color HeaderColor = Color.FromArgb(32, 32, 32);
        private static readonly Color AccentColor = Color.FromArgb(255, 200, 0);      // Amber / gold
        private static readonly Color AccentDimColor = Color.FromArgb(180, 140, 0);
        private static readonly Color TextColor = Color.FromArgb(220, 220, 210);
        private static readonly Color DimTextColor = Color.FromArgb(130, 130, 120);
        private static readonly Color HighlightBgColor = Color.FromArgb(60, 55, 20);
        private static readonly Color CurrentMatchBgColor = Color.FromArgb(150, 130, 10);
        private static readonly Color DateHeaderColor = Color.FromArgb(80, 200, 255);
        private static readonly Color SpikeColor = Color.FromArgb(255, 100, 80);
        private static readonly Color AiColor = Color.FromArgb(120, 220, 120);
        private static readonly Color ProcessColor = Color.FromArgb(255, 200, 0);
        private static readonly Color BorderColor = Color.FromArgb(55, 55, 50);
        private static readonly Color SearchBoxBg = Color.FromArgb(36, 36, 32);
        private static readonly Color ScrollBarColor = Color.FromArgb(60, 58, 50);
        private static readonly Color ButtonHoverColor = Color.FromArgb(50, 48, 40);

        // ── Controls ────────────────────────────────────────────────────
        private readonly RichTextBox _logTextBox;
        private readonly TextBox _searchBox;
        private readonly Label _titleLabel;
        private readonly Label _statusLabel;
        private readonly Label _filePathLabel;
        private readonly Label _matchCountLabel;
        private readonly Button _refreshButton;
        private readonly Button _clearSearchButton;
        private readonly Button _findButton;
        private readonly Button _findPrevButton;
        private readonly Button _findNextButton;
        private readonly Button _scrollTopButton;
        private readonly Button _scrollBottomButton;
        private readonly CheckBox _autoRefreshCheckBox;
        private readonly CheckBox _autoScrollCheckBox;
        private readonly System.Windows.Forms.Timer _refreshTimer;
        private readonly System.Windows.Forms.Timer _searchDebounceTimer;
        private readonly Panel _headerPanel;
        private readonly Panel _toolbarPanel;
        private readonly Panel _statusBarPanel;

        private readonly string _logFilePath;
        private readonly List<int> _matchPositions = new();
        private long _lastFileSize;
        private DateTime _lastModified;
        private int _currentMatchIndex = -1;
        private bool _isSearchActive;
        private bool _suppressSearchTextChanged;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="LogViewerWindow"/> class.
        /// </summary>
        /// <param name="logFilePath">The path to the log file to display.</param>
        public LogViewerWindow(string logFilePath)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(logFilePath);
            _logFilePath = logFilePath;

            // ── Form setup ──────────────────────────────────────────────
            Text = "📋 Performance Log Viewer";
            Size = new Size(820, 620);
            MinimumSize = new Size(600, 400);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = BackgroundColor;
            ForeColor = TextColor;
            Font = new Font("Segoe UI", 9.5f);
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            MinimizeBox = true;
            ShowInTaskbar = true;
            Icon = LoadApplicationIcon();
            KeyPreview = true;

            // ── Header panel ────────────────────────────────────────────
            _headerPanel = CreateHeaderPanel(out _titleLabel, out _filePathLabel);

            // ── Toolbar panel ───────────────────────────────────────────
            _toolbarPanel = CreateToolbarPanel(
                out _searchBox,
                out _findButton,
                out _findPrevButton,
                out _findNextButton,
                out _matchCountLabel,
                out _clearSearchButton,
                out _refreshButton,
                out _scrollTopButton,
                out _scrollBottomButton,
                out _autoRefreshCheckBox,
                out _autoScrollCheckBox);

            // ── Log text area ───────────────────────────────────────────
            _logTextBox = CreateLogTextBox();

            // ── Status bar ──────────────────────────────────────────────
            _statusBarPanel = CreateStatusBar(out _statusLabel);

            // ── Layout ───────────────────────────────────────────────
            // WinForms docking processes controls in REVERSE collection
            // order, but Z-order is index 0 = front.  We need Fill at the
            // BACK so it never steals mouse events from the toolbar.
            // Docking resolution (reverse index):
            //   _logTextBox  (index 3, Fill)  → processed last  → remaining
            //   _statusBar   (index 2, Bottom)→ processed third → bottom
            //   _headerPanel (index 1, Top)   → processed second→ very top
            //   _toolbarPanel(index 0, Top)   → processed first → below header
            Controls.Add(_toolbarPanel);     // Top  – index 0 (front)
            Controls.Add(_headerPanel);      // Top  – index 1
            Controls.Add(_statusBarPanel);   // Bot  – index 2
            Controls.Add(_logTextBox);       // Fill – index 3 (back)

            // ── Auto-refresh timer ──────────────────────────────────────
            _refreshTimer = new System.Windows.Forms.Timer { Interval = 2000 };
            _refreshTimer.Tick += (_, _) => RefreshIfChanged();
            _refreshTimer.Start();

            // ── Key bindings ────────────────────────────────────────────
            KeyDown += OnFormKeyDown;
            _searchBox.KeyDown += OnSearchBoxKeyDown;
            _searchBox.TextChanged += OnSearchTextChanged;

            // ── Search debounce timer (300 ms delay for live search) ────
            _searchDebounceTimer = new System.Windows.Forms.Timer { Interval = 300 };
            _searchDebounceTimer.Tick += (_, _) =>
            {
                _searchDebounceTimer.Stop();
                ExecuteSearch();
            };

            // ── Initial load ────────────────────────────────────────────
            LoadLogContents();
        }

        // =================================================================
        //  Factory helpers – build the visual tree
        // =================================================================

        /// <summary>
        /// Creates the header panel with title and file path display.
        /// </summary>
        private Panel CreateHeaderPanel(out Label titleLabel, out Label filePathLabel)
        {
            var panel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 62,
                BackColor = HeaderColor,
                Padding = new Padding(16, 10, 16, 6)
            };

            // Bottom border line
            var borderLine = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 1,
                BackColor = AccentColor
            };
            panel.Controls.Add(borderLine);

            titleLabel = new Label
            {
                Text = "⚡ PERFORMANCE LOG VIEWER",
                Font = new Font("Consolas", 13f, FontStyle.Bold),
                ForeColor = AccentColor,
                AutoSize = true,
                Location = new Point(16, 8),
                BackColor = Color.Transparent
            };
            panel.Controls.Add(titleLabel);

            filePathLabel = new Label
            {
                Text = $"📁 {_logFilePath}",
                Font = new Font("Consolas", 8.5f),
                ForeColor = DimTextColor,
                AutoSize = true,
                Location = new Point(16, 34),
                BackColor = Color.Transparent
            };
            panel.Controls.Add(filePathLabel);

            return panel;
        }

        /// <summary>
        /// Creates the toolbar with search, refresh, and option controls.
        /// </summary>
        private Panel CreateToolbarPanel(
            out TextBox searchBox,
            out Button findBtn,
            out Button findPrevBtn,
            out Button findNextBtn,
            out Label matchCountLbl,
            out Button clearSearchBtn,
            out Button refreshBtn,
            out Button scrollTopBtn,
            out Button scrollBottomBtn,
            out CheckBox autoRefreshCb,
            out CheckBox autoScrollCb)
        {
            var panel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 42,
                BackColor = PanelColor
            };

            // Paint border along the bottom edge
            panel.Paint += (_, e) =>
            {
                using var pen = new Pen(BorderColor);
                e.Graphics.DrawLine(pen, 0, panel.Height - 1, panel.Width, panel.Height - 1);
            };

            var x = 8;

            // ── 🔍 icon ────────────────────────────────────────────────
            panel.Controls.Add(new Label
            {
                Text = "🔍",
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = AccentColor,
                Location = new Point(x, 8),
                BackColor = Color.Transparent
            });
            x += 24;

            // ── Search text box ─────────────────────────────────────────
            var sb = new TextBox
            {
                Width = 156,
                Height = 26,
                Location = new Point(x, 7),
                BackColor = SearchBoxBg,
                ForeColor = TextColor,
                Font = new Font("Consolas", 9.5f),
                BorderStyle = BorderStyle.FixedSingle,
                PlaceholderText = "Search logs...",
                ShortcutsEnabled = true
            };
            panel.Controls.Add(sb);
            searchBox = sb;
            x += 160;

            // ── Find button ─────────────────────────────────────────────
            var fb = CreateToolbarButton("Find", x, "Find matches (Enter)");
            fb.Width = 42;
            fb.Click += (_, _) => ExecuteSearch();
            panel.Controls.Add(fb);
            findBtn = fb;
            x += 46;

            // ── ◀ Previous match ────────────────────────────────────────
            var prevB = CreateToolbarButton("◀", x, "Previous match (Shift+F3)");
            prevB.Width = 26;
            prevB.Click += (_, _) => NavigatePreviousMatch();
            panel.Controls.Add(prevB);
            findPrevBtn = prevB;
            x += 30;

            // ── ▶ Next match ────────────────────────────────────────────
            var nextB = CreateToolbarButton("▶", x, "Next match (F3 / Enter)");
            nextB.Width = 26;
            nextB.Click += (_, _) => NavigateNextMatch();
            panel.Controls.Add(nextB);
            findNextBtn = nextB;
            x += 30;

            // ── Match count label ───────────────────────────────────────
            var countLbl = new Label
            {
                Text = "",
                AutoSize = false,
                Width = 62,
                Height = 26,
                Location = new Point(x, 8),
                ForeColor = DimTextColor,
                Font = new Font("Consolas", 8.5f),
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent
            };
            panel.Controls.Add(countLbl);
            matchCountLbl = countLbl;
            x += 64;

            // ── ✕ Clear search ──────────────────────────────────────────
            var clrBtn = CreateToolbarButton("✕", x, "Clear search (Esc)");
            clrBtn.Width = 26;
            clrBtn.Click += (_, _) => ClearSearch();
            panel.Controls.Add(clrBtn);
            clearSearchBtn = clrBtn;
            x += 34;

            // ── │ separator ─────────────────────────────────────────────
            panel.Controls.Add(new Label { Text = "│", ForeColor = BorderColor, AutoSize = true, Location = new Point(x, 8), BackColor = Color.Transparent });
            x += 16;

            // ── Refresh ─────────────────────────────────────────────────
            var refB = CreateToolbarButton("⟳ Refresh", x, "Reload log file (F5)");
            refB.Width = 78;
            refB.Click += (_, _) => { ClearSearchHighlights(); LoadLogContents(); };
            panel.Controls.Add(refB);
            refreshBtn = refB;
            x += 82;

            // ── ⤒ Top ──────────────────────────────────────────────────
            var topB = CreateToolbarButton("⤒", x, "Scroll to top");
            topB.Width = 28;
            topB.Click += (_, _) => ScrollToTop();
            panel.Controls.Add(topB);
            scrollTopBtn = topB;
            x += 32;

            // ── ⤓ End ──────────────────────────────────────────────────
            var botB = CreateToolbarButton("⤓", x, "Scroll to bottom");
            botB.Width = 28;
            botB.Click += (_, _) => ScrollToBottom();
            panel.Controls.Add(botB);
            scrollBottomBtn = botB;
            x += 36;

            // ── │ separator ─────────────────────────────────────────────
            panel.Controls.Add(new Label { Text = "│", ForeColor = BorderColor, AutoSize = true, Location = new Point(x, 8), BackColor = Color.Transparent });
            x += 16;

            // ── Auto-refresh toggle ─────────────────────────────────────
            var arCb = new CheckBox
            {
                Text = "Auto-↻",
                Checked = true,
                AutoSize = true,
                ForeColor = AccentColor,
                Font = new Font("Segoe UI", 8.5f),
                Location = new Point(x, 8),
                BackColor = PanelColor,
                Appearance = Appearance.Button,
                FlatStyle = FlatStyle.Flat
            };
            arCb.FlatAppearance.BorderColor = BorderColor;
            arCb.FlatAppearance.CheckedBackColor = Color.FromArgb(50, 48, 10);
            arCb.FlatAppearance.MouseOverBackColor = ButtonHoverColor;
            arCb.CheckedChanged += (_, _) =>
            {
                if (arCb.Checked)
                {
                    arCb.ForeColor = AccentColor;
                    if (!_isSearchActive) { _refreshTimer.Start(); }
                }
                else
                {
                    arCb.ForeColor = DimTextColor;
                    _refreshTimer.Stop();
                }
            };
            panel.Controls.Add(arCb);
            autoRefreshCb = arCb;
            x += 72;

            // ── Auto-scroll toggle ──────────────────────────────────────
            var asCb = new CheckBox
            {
                Text = "Auto-↕",
                Checked = true,
                AutoSize = true,
                ForeColor = AccentColor,
                Font = new Font("Segoe UI", 8.5f),
                Location = new Point(x, 8),
                BackColor = PanelColor,
                Appearance = Appearance.Button,
                FlatStyle = FlatStyle.Flat
            };
            asCb.FlatAppearance.BorderColor = BorderColor;
            asCb.FlatAppearance.CheckedBackColor = Color.FromArgb(50, 48, 10);
            asCb.FlatAppearance.MouseOverBackColor = ButtonHoverColor;
            asCb.CheckedChanged += (_, _) =>
            {
                asCb.ForeColor = asCb.Checked ? AccentColor : DimTextColor;
            };
            panel.Controls.Add(asCb);
            autoScrollCb = asCb;

            return panel;
        }

        /// <summary>
        /// Creates a flat-styled toolbar button with hover effects.
        /// </summary>
        private static Button CreateToolbarButton(string text, int x, string tooltip)
        {
            var btn = new Button
            {
                Text = text,
                Height = 26,
                Location = new Point(x, 5),
                FlatStyle = FlatStyle.Flat,
                BackColor = PanelColor,
                ForeColor = AccentColor,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter
            };
            btn.FlatAppearance.BorderColor = BorderColor;
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.MouseOverBackColor = ButtonHoverColor;
            btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(40, 38, 30);

            var tt = new ToolTip();
            tt.SetToolTip(btn, tooltip);

            return btn;
        }

        /// <summary>
        /// Creates the main log display area as a syntax-highlighted RichTextBox.
        /// </summary>
        private RichTextBox CreateLogTextBox()
        {
            var rtb = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BackColor = BackgroundColor,
                ForeColor = TextColor,
                Font = new Font("Consolas", 10f),
                BorderStyle = BorderStyle.None,
                WordWrap = false,
                ScrollBars = RichTextBoxScrollBars.Both,
                DetectUrls = false,
                Padding = new Padding(12),
                Margin = new Padding(8, 4, 8, 4)
            };

            // Remove the border-less gray look on focus
            rtb.Enter += (_, _) => { };

            return rtb;
        }

        /// <summary>
        /// Creates the bottom status bar showing file info and entry count.
        /// </summary>
        private Panel CreateStatusBar(out Label statusLabel)
        {
            var panel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 28,
                BackColor = HeaderColor,
                Padding = new Padding(12, 4, 12, 4)
            };

            // Top border
            var borderLine = new Panel
            {
                Dock = DockStyle.Top,
                Height = 1,
                BackColor = BorderColor
            };
            panel.Controls.Add(borderLine);

            statusLabel = new Label
            {
                Text = "Ready",
                Dock = DockStyle.Fill,
                ForeColor = DimTextColor,
                Font = new Font("Consolas", 8.5f),
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent
            };
            panel.Controls.Add(statusLabel);

            return panel;
        }

        // =================================================================
        //  Core logic – reading, displaying, searching
        // =================================================================

        /// <summary>
        /// Loads the entire log file content and renders it with syntax highlighting.
        /// </summary>
        private void LoadLogContents()
        {
            try
            {
                if (!File.Exists(_logFilePath))
                {
                    RenderPlaceholder("No log file found yet.\n\nLogs will appear here when performance spikes are detected.");
                    UpdateStatus("No log file", 0, 0);
                    return;
                }

                string content;
                using (var stream = new FileStream(_logFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var reader = new StreamReader(stream))
                {
                    content = reader.ReadToEnd();
                }

                var fileInfo = new FileInfo(_logFilePath);
                _lastFileSize = fileInfo.Length;
                _lastModified = fileInfo.LastWriteTime;

                if (string.IsNullOrWhiteSpace(content))
                {
                    RenderPlaceholder("Log file is empty.\n\nEntries will appear here when performance spikes are detected.");
                    UpdateStatus("Empty log", 0, fileInfo.Length);
                    return;
                }

                RenderHighlightedContent(content);

                var lineCount = content.Split('\n').Length;
                UpdateStatus($"Last updated: {fileInfo.LastWriteTime:HH:mm:ss}", lineCount, fileInfo.Length);

                // Re-apply search highlights if a search is active
                if (_isSearchActive)
                {
                    ReapplySearchHighlights();
                }
                else if (_autoScrollCheckBox.Checked)
                {
                    ScrollToBottom();
                }
            }
            catch (Exception ex)
            {
                RenderPlaceholder($"Error reading log file:\n\n{ex.Message}");
                UpdateStatus("Error", 0, 0);
            }
        }

        /// <summary>
        /// Checks if the file has changed and reloads if necessary.
        /// </summary>
        private void RefreshIfChanged()
        {
            if (_disposed || _isSearchActive || !File.Exists(_logFilePath))
            {
                return;
            }

            try
            {
                var fileInfo = new FileInfo(_logFilePath);
                if (fileInfo.Length != _lastFileSize || fileInfo.LastWriteTime != _lastModified)
                {
                    LoadLogContents();
                }
            }
            catch
            {
                // Ignore transient file access issues
            }
        }

        /// <summary>
        /// Renders the log content with syntax highlighting into the RichTextBox.
        /// </summary>
        /// <param name="content">The raw log file content.</param>
        private void RenderHighlightedContent(string content)
        {
            _logTextBox.SuspendLayout();
            _logTextBox.Clear();

            var lines = content.Split('\n');

            foreach (var rawLine in lines)
            {
                var line = rawLine.TrimEnd('\r');

                if (string.IsNullOrWhiteSpace(line))
                {
                    AppendLine(string.Empty, TextColor);
                    continue;
                }

                // ── Date headers:  ===== 2026-02-09 (Monday) ===== ──────
                if (line.TrimStart().StartsWith("=====", StringComparison.Ordinal))
                {
                    AppendLine(line, DateHeaderColor, bold: true);
                    continue;
                }

                // ── "Started:" line ─────────────────────────────────────
                if (line.TrimStart().StartsWith("Started:", StringComparison.Ordinal))
                {
                    AppendLine(line, DimTextColor);
                    continue;
                }

                // ── Separator dashes ────────────────────────────────────
                if (line.TrimStart().StartsWith("---", StringComparison.Ordinal))
                {
                    AppendLine(line, BorderColor);
                    continue;
                }

                // ── Spike detection lines ───────────────────────────────
                if (line.Contains("spike detected", StringComparison.OrdinalIgnoreCase))
                {
                    AppendLine(line, SpikeColor, bold: true);
                    continue;
                }

                // ── "Top … processes:" header ───────────────────────────
                if (line.TrimStart().StartsWith("Top ", StringComparison.Ordinal) &&
                    line.Contains("process", StringComparison.OrdinalIgnoreCase))
                {
                    AppendLine(line, ProcessColor);
                    continue;
                }

                // ── AI Analysis header ──────────────────────────────────
                if (line.TrimStart().StartsWith("AI Analysis", StringComparison.OrdinalIgnoreCase))
                {
                    AppendLine(line, AiColor, bold: true);
                    continue;
                }

                // ── Indented AI summary lines ───────────────────────────
                if (line.StartsWith("  ", StringComparison.Ordinal) &&
                    !line.TrimStart().StartsWith("Top ", StringComparison.Ordinal))
                {
                    AppendLine(line, AiColor);
                    continue;
                }

                // ── Process list lines (contain PID / CPU% / RAM patterns) ──
                if (line.Contains("PID", StringComparison.OrdinalIgnoreCase) ||
                    line.Contains("CPU:", StringComparison.OrdinalIgnoreCase) ||
                    line.Contains("RAM:", StringComparison.OrdinalIgnoreCase) ||
                    line.Contains("Memory:", StringComparison.OrdinalIgnoreCase))
                {
                    AppendLine(line, ProcessColor);
                    continue;
                }

                // ── Default text ────────────────────────────────────────
                AppendLine(line, TextColor);
            }

            _logTextBox.ResumeLayout();
        }

        /// <summary>
        /// Appends a single coloured line to the RichTextBox.
        /// </summary>
        private void AppendLine(string text, Color color, bool bold = false)
        {
            var start = _logTextBox.TextLength;
            _logTextBox.AppendText(text + Environment.NewLine);
            _logTextBox.Select(start, text.Length + Environment.NewLine.Length);
            _logTextBox.SelectionColor = color;

            if (bold)
            {
                _logTextBox.SelectionFont = new Font(_logTextBox.Font, FontStyle.Bold);
            }

            _logTextBox.Select(_logTextBox.TextLength, 0);
        }

        /// <summary>
        /// Shows a placeholder message when no log content is available.
        /// </summary>
        private void RenderPlaceholder(string message)
        {
            _logTextBox.SuspendLayout();
            _logTextBox.Clear();

            // Center-ish the placeholder visually
            AppendLine(string.Empty, DimTextColor);
            AppendLine(string.Empty, DimTextColor);

            foreach (var line in message.Split('\n'))
            {
                AppendLine($"    {line}", DimTextColor);
            }

            _logTextBox.ResumeLayout();
        }

        /// <summary>
        /// Updates the status bar with file information.
        /// </summary>
        private void UpdateStatus(string info, int lineCount, long fileSize)
        {
            var sizeText = fileSize switch
            {
                >= 1_048_576 => $"{fileSize / 1_048_576.0:F1} MB",
                >= 1_024 => $"{fileSize / 1_024.0:F1} KB",
                _ => $"{fileSize} bytes"
            };

            _statusLabel.Text = lineCount > 0
                ? $"  {info}  │  {lineCount:N0} lines  │  {sizeText}  │  Ctrl+F to search"
                : $"  {info}";
        }

        // =================================================================
        //  Search engine
        // =================================================================

        /// <summary>
        /// Returns the current search term, or null if the box has placeholder / is empty.
        /// </summary>
        private string? GetSearchTerm()
        {
            var text = _searchBox.Text;
            return string.IsNullOrWhiteSpace(text) ? null : text;
        }

        /// <summary>
        /// Handles live text changes in the search box with debounce.
        /// </summary>
        private void OnSearchTextChanged(object? sender, EventArgs e)
        {
            if (_suppressSearchTextChanged)
            {
                return;
            }

            var term = GetSearchTerm();
            if (term == null)
            {
                if (_isSearchActive)
                {
                    ClearSearchHighlights();
                }

                return;
            }

            // Restart debounce timer – search fires 300 ms after last keystroke
            _searchDebounceTimer.Stop();
            _searchDebounceTimer.Start();
        }

        /// <summary>
        /// Finds every occurrence of the search term, highlights them, and
        /// navigates to the first match.
        /// </summary>
        private void ExecuteSearch()
        {
            _searchDebounceTimer.Stop();

            var term = GetSearchTerm();
            if (term == null)
            {
                ClearSearchHighlights();
                return;
            }

            var restoreSearchFocus = _searchBox.Focused;

            // Pause auto-refresh so highlights are not wiped
            _refreshTimer.Stop();
            _isSearchActive = true;

            // Collect every match position (case-insensitive)
            _matchPositions.Clear();
            _currentMatchIndex = -1;

            var text = _logTextBox.Text;
            var startPos = 0;

            while (startPos < text.Length)
            {
                var idx = text.IndexOf(term, startPos, StringComparison.OrdinalIgnoreCase);
                if (idx < 0)
                {
                    break;
                }

                _matchPositions.Add(idx);
                startPos = idx + Math.Max(term.Length, 1);
            }

            // Apply dim highlight to every match
            if (_matchPositions.Count > 0)
            {
                _logTextBox.SuspendLayout();

                foreach (var pos in _matchPositions)
                {
                    _logTextBox.Select(pos, term.Length);
                    _logTextBox.SelectionBackColor = HighlightBgColor;
                }

                // Bright highlight on first match and scroll to it
                _currentMatchIndex = 0;
                var firstPos = _matchPositions[0];
                _logTextBox.Select(firstPos, term.Length);
                _logTextBox.SelectionBackColor = CurrentMatchBgColor;
                _logTextBox.ScrollToCaret();

                _logTextBox.ResumeLayout();
            }
            else
            {
                // Deselect
                _logTextBox.Select(_logTextBox.TextLength, 0);
            }

            UpdateMatchCountDisplay();

            if (restoreSearchFocus)
            {
                _searchBox.Focus();
                _searchBox.SelectionStart = _searchBox.TextLength;
                _searchBox.SelectionLength = 0;
            }
        }

        /// <summary>
        /// Re-applies search highlights after the log content has been reloaded.
        /// Called by <see cref="LoadLogContents"/> when <see cref="_isSearchActive"/> is true.
        /// </summary>
        private void ReapplySearchHighlights()
        {
            var term = GetSearchTerm();
            if (term == null)
            {
                return;
            }

            var restoreSearchFocus = _searchBox.Focused;

            _matchPositions.Clear();

            var text = _logTextBox.Text;
            var startPos = 0;

            while (startPos < text.Length)
            {
                var idx = text.IndexOf(term, startPos, StringComparison.OrdinalIgnoreCase);
                if (idx < 0)
                {
                    break;
                }

                _matchPositions.Add(idx);
                startPos = idx + Math.Max(term.Length, 1);
            }

            if (_matchPositions.Count > 0)
            {
                _logTextBox.SuspendLayout();

                foreach (var pos in _matchPositions)
                {
                    _logTextBox.Select(pos, term.Length);
                    _logTextBox.SelectionBackColor = HighlightBgColor;
                }

                // Clamp current index to new match list
                if (_currentMatchIndex < 0 || _currentMatchIndex >= _matchPositions.Count)
                {
                    _currentMatchIndex = 0;
                }

                var curPos = _matchPositions[_currentMatchIndex];
                _logTextBox.Select(curPos, term.Length);
                _logTextBox.SelectionBackColor = CurrentMatchBgColor;
                _logTextBox.ScrollToCaret();

                _logTextBox.ResumeLayout();
            }
            else
            {
                _currentMatchIndex = -1;
            }

            UpdateMatchCountDisplay();

            if (restoreSearchFocus)
            {
                _searchBox.Focus();
                _searchBox.SelectionStart = _searchBox.TextLength;
                _searchBox.SelectionLength = 0;
            }
        }

        /// <summary>
        /// Navigates to the next search match (wraps around).
        /// </summary>
        private void NavigateNextMatch()
        {
            var term = GetSearchTerm();
            if (term == null || _matchPositions.Count == 0)
            {
                return;
            }

            var restoreSearchFocus = _searchBox.Focused;

            // Dim the old current match
            if (_currentMatchIndex >= 0 && _currentMatchIndex < _matchPositions.Count)
            {
                _logTextBox.Select(_matchPositions[_currentMatchIndex], term.Length);
                _logTextBox.SelectionBackColor = HighlightBgColor;
            }

            // Advance (wrap)
            _currentMatchIndex = (_currentMatchIndex + 1) % _matchPositions.Count;

            // Bright-highlight the new current match
            _logTextBox.Select(_matchPositions[_currentMatchIndex], term.Length);
            _logTextBox.SelectionBackColor = CurrentMatchBgColor;
            _logTextBox.ScrollToCaret();

            UpdateMatchCountDisplay();

            if (restoreSearchFocus)
            {
                _searchBox.Focus();
                _searchBox.SelectionStart = _searchBox.TextLength;
                _searchBox.SelectionLength = 0;
            }
        }

        /// <summary>
        /// Navigates to the previous search match (wraps around).
        /// </summary>
        private void NavigatePreviousMatch()
        {
            var term = GetSearchTerm();
            if (term == null || _matchPositions.Count == 0)
            {
                return;
            }

            var restoreSearchFocus = _searchBox.Focused;

            // Dim the old current match
            if (_currentMatchIndex >= 0 && _currentMatchIndex < _matchPositions.Count)
            {
                _logTextBox.Select(_matchPositions[_currentMatchIndex], term.Length);
                _logTextBox.SelectionBackColor = HighlightBgColor;
            }

            // Go back (wrap)
            _currentMatchIndex = (_currentMatchIndex - 1 + _matchPositions.Count) % _matchPositions.Count;

            // Bright-highlight the new current match
            _logTextBox.Select(_matchPositions[_currentMatchIndex], term.Length);
            _logTextBox.SelectionBackColor = CurrentMatchBgColor;
            _logTextBox.ScrollToCaret();

            UpdateMatchCountDisplay();

            if (restoreSearchFocus)
            {
                _searchBox.Focus();
                _searchBox.SelectionStart = _searchBox.TextLength;
                _searchBox.SelectionLength = 0;
            }
        }

        /// <summary>
        /// Updates the match count label (e.g. "3 / 12").
        /// </summary>
        private void UpdateMatchCountDisplay()
        {
            if (_matchPositions.Count == 0)
            {
                var term = GetSearchTerm();
                _matchCountLabel.Text = term != null ? "0 found" : "";
                _matchCountLabel.ForeColor = term != null ? SpikeColor : DimTextColor;
            }
            else
            {
                _matchCountLabel.Text = $"{_currentMatchIndex + 1} / {_matchPositions.Count}";
                _matchCountLabel.ForeColor = AccentColor;
            }
        }

        /// <summary>
        /// Clears search state, resets the search box to placeholder,
        /// and reloads the log to remove all highlights.
        /// </summary>
        private void ClearSearch()
        {
            _searchDebounceTimer.Stop();
            _isSearchActive = false;
            _matchPositions.Clear();
            _currentMatchIndex = -1;
            _matchCountLabel.Text = "";

            _suppressSearchTextChanged = true;
            _searchBox.Text = string.Empty;
            _suppressSearchTextChanged = false;

            LoadLogContents();

            if (_autoRefreshCheckBox.Checked)
            {
                _refreshTimer.Start();
            }
        }

        /// <summary>
        /// Clears search highlights without resetting the search box text.
        /// Used when the user deletes all text from the search box.
        /// </summary>
        private void ClearSearchHighlights()
        {
            _searchDebounceTimer.Stop();
            _isSearchActive = false;
            _matchPositions.Clear();
            _currentMatchIndex = -1;
            _matchCountLabel.Text = "";

            LoadLogContents();

            if (_autoRefreshCheckBox.Checked)
            {
                _refreshTimer.Start();
            }
        }

        // =================================================================
        //  Navigation helpers
        // =================================================================

        /// <summary>
        /// Scrolls the log view to the very top.
        /// </summary>
        private void ScrollToTop()
        {
            if (_logTextBox.TextLength > 0)
            {
                _logTextBox.Select(0, 0);
                _logTextBox.ScrollToCaret();
            }
        }

        /// <summary>
        /// Scrolls the log view to the very bottom.
        /// </summary>
        private void ScrollToBottom()
        {
            if (_logTextBox.TextLength > 0)
            {
                _logTextBox.Select(_logTextBox.TextLength, 0);
                _logTextBox.ScrollToCaret();
            }
        }

        // =================================================================
        //  Keyboard handling
        // =================================================================

        /// <summary>
        /// Handles global keyboard shortcuts for the form.
        /// </summary>
        private void OnFormKeyDown(object? sender, KeyEventArgs e)
        {
            // Ctrl+F → focus search box
            if (e is { Control: true, KeyCode: Keys.F })
            {
                _searchBox.Focus();
                _searchBox.SelectAll();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            // Escape → clear search first, then close
            else if (e.KeyCode == Keys.Escape)
            {
                if (_isSearchActive)
                {
                    ClearSearch();
                }
                else
                {
                    Close();
                }

                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            // F5 → refresh
            else if (e.KeyCode == Keys.F5)
            {
                ClearSearchHighlights();
                LoadLogContents();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            // F3 → next match
            else if (e.KeyCode == Keys.F3 && !e.Shift)
            {
                NavigateNextMatch();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            // Shift+F3 → previous match
            else if (e is { KeyCode: Keys.F3, Shift: true })
            {
                NavigatePreviousMatch();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        /// <summary>
        /// Handles key events in the search box.
        /// </summary>
        private void OnSearchBoxKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                _searchDebounceTimer.Stop();
                if (_isSearchActive && _matchPositions.Count > 0)
                {
                    NavigateNextMatch();
                }
                else
                {
                    ExecuteSearch();
                }

                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Escape)
            {
                ClearSearch();
                _logTextBox.Focus();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.F3 && !e.Shift)
            {
                NavigateNextMatch();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            else if (e is { KeyCode: Keys.F3, Shift: true })
            {
                NavigatePreviousMatch();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        // =================================================================
        //  Utilities
        // =================================================================

        /// <summary>
        /// Loads the application icon from embedded resources or falls back to the executable icon.
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
                    _refreshTimer?.Stop();
                    _refreshTimer?.Dispose();
                    _searchDebounceTimer?.Stop();
                    _searchDebounceTimer?.Dispose();
                }
                catch
                {
                    // Ignore disposal errors
                }
            }

            _disposed = true;
            base.Dispose(disposing);
        }
    }
}
