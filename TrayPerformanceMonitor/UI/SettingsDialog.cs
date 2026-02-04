// -----------------------------------------------------------------------
// <copyright file="SettingsDialog.cs" company="TrayPerformanceMonitor">
//     Copyright (c) TrayPerformanceMonitor. All rights reserved.
// </copyright>
// <summary>
//     Provides a settings dialog for configuring AI model and process count options.
// </summary>
// -----------------------------------------------------------------------

using System.Diagnostics;
using TrayPerformanceMonitor.Configuration;

namespace TrayPerformanceMonitor.UI
{
    /// <summary>
    /// A dialog for configuring application settings including AI model selection
    /// and top process count.
    /// </summary>
    internal sealed class SettingsDialog : Form
    {
        private readonly ComboBox _modelTypeComboBox;
        private readonly NumericUpDown _processCountNumeric;
        private readonly Label _modelStatusLabel;
        private readonly Button _downloadButton;
        private readonly Button _saveButton;
        private readonly Button _cancelButton;
        private readonly ProgressBar _downloadProgress;
        private readonly Label _downloadStatusLabel;

        private bool _isDownloading;
        private CancellationTokenSource? _downloadCts;

        /// <summary>
        /// Event raised when the model needs to be reloaded after a change.
        /// </summary>
        public event EventHandler? ModelReloadRequested;

        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsDialog"/> class.
        /// </summary>
        public SettingsDialog()
        {
            Text = "Performance Monitor Settings";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(450, 380);
            Font = new Font("Segoe UI", 9);

            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(15),
                RowCount = 7,
                ColumnCount = 2
            };

            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 35)); // Title
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 45)); // Model selection
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30)); // Model status
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 45)); // Download button
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 45)); // Process count
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 60)); // Download progress
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // Buttons

            // Title
            var titleLabel = new Label
            {
                Text = "⚙️ Application Settings",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                AutoSize = true,
                Padding = new Padding(0, 0, 0, 10)
            };
            mainPanel.Controls.Add(titleLabel, 0, 0);
            mainPanel.SetColumnSpan(titleLabel, 2);

            // AI Model Selection
            var modelLabel = new Label
            {
                Text = "🤖 AI Model:",
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Padding = new Padding(0, 8, 0, 0)
            };
            mainPanel.Controls.Add(modelLabel, 0, 1);

            _modelTypeComboBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Width = 200
            };
            _modelTypeComboBox.Items.Add("Full Model (TinyLlama 1.1B - ~640 MB)");
            _modelTypeComboBox.Items.Add("Lite Model (Qwen2 0.5B - ~320 MB)");
            _modelTypeComboBox.SelectedIndex = UserSettings.Instance.ValidatedModelType == "lite" ? 1 : 0;
            _modelTypeComboBox.SelectedIndexChanged += ModelTypeComboBox_SelectedIndexChanged;
            mainPanel.Controls.Add(_modelTypeComboBox, 1, 1);

            // Model status
            _modelStatusLabel = new Label
            {
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                ForeColor = Color.Gray
            };
            mainPanel.Controls.Add(_modelStatusLabel, 1, 2);

            // Download button (must be created before UpdateModelStatus is called)
            _downloadButton = new Button
            {
                Text = "📥 Download / Switch Model",
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Padding = new Padding(10, 5, 10, 5)
            };
            _downloadButton.Click += DownloadButton_Click;
            mainPanel.Controls.Add(_downloadButton, 1, 3);

            // Now update model status (after _downloadButton is created)
            UpdateModelStatus();

            // Process Count
            var processCountLabel = new Label
            {
                Text = "📊 Top Processes to Log:",
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Padding = new Padding(0, 8, 0, 0)
            };
            mainPanel.Controls.Add(processCountLabel, 0, 4);

            _processCountNumeric = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 10,
                Value = UserSettings.Instance.ValidatedTopProcessCount,
                Width = 60,
                Anchor = AnchorStyles.Left
            };
            mainPanel.Controls.Add(_processCountNumeric, 1, 4);

            // Download progress panel
            var progressPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Visible = false
            };

            _downloadProgress = new ProgressBar
            {
                Style = ProgressBarStyle.Marquee,
                MarqueeAnimationSpeed = 30,
                Dock = DockStyle.Top,
                Height = 20
            };
            progressPanel.Controls.Add(_downloadProgress);

            _downloadStatusLabel = new Label
            {
                Text = "Downloading model...",
                AutoSize = true,
                Dock = DockStyle.Bottom,
                ForeColor = Color.Blue
            };
            progressPanel.Controls.Add(_downloadStatusLabel);

            mainPanel.Controls.Add(progressPanel, 0, 5);
            mainPanel.SetColumnSpan(progressPanel, 2);

            // Buttons panel
            var buttonsPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.RightToLeft,
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 10, 0, 0)
            };

            _cancelButton = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Width = 80
            };
            buttonsPanel.Controls.Add(_cancelButton);

            _saveButton = new Button
            {
                Text = "Save",
                Width = 80
            };
            _saveButton.Click += SaveButton_Click;
            buttonsPanel.Controls.Add(_saveButton);

            mainPanel.Controls.Add(buttonsPanel, 0, 6);
            mainPanel.SetColumnSpan(buttonsPanel, 2);

            Controls.Add(mainPanel);

            AcceptButton = _saveButton;
            CancelButton = _cancelButton;

            // Update download button state based on script availability
            UpdateDownloadButtonState();
        }

        /// <summary>
        /// Updates the model status label based on installed models.
        /// </summary>
        private void UpdateModelStatus()
        {
            var (fullInstalled, liteInstalled) = UserSettings.GetInstalledModels();
            var selectedType = _modelTypeComboBox.SelectedIndex == 0 ? "full" : "lite";
            var isSelectedInstalled = selectedType == "full" ? fullInstalled : liteInstalled;

            var statusParts = new List<string>();
            if (fullInstalled) statusParts.Add("Full ✓");
            if (liteInstalled) statusParts.Add("Lite ✓");

            if (!fullInstalled && !liteInstalled)
            {
                _modelStatusLabel.Text = "⚠️ No models installed";
                _modelStatusLabel.ForeColor = Color.Orange;
                _downloadButton.Text = "📥 Download Model";
            }
            else if (isSelectedInstalled)
            {
                _modelStatusLabel.Text = $"✅ Ready to use | Installed: {string.Join(", ", statusParts)}";
                _modelStatusLabel.ForeColor = Color.Green;
                _downloadButton.Text = "📥 Re-download Model";
            }
            else
            {
                _modelStatusLabel.Text = $"📥 Need to download | Installed: {string.Join(", ", statusParts)}";
                _modelStatusLabel.ForeColor = Color.Blue;
                _downloadButton.Text = "📥 Download Model";
            }
        }

        /// <summary>
        /// Updates the download button state based on script availability.
        /// </summary>
        private void UpdateDownloadButtonState()
        {
            var selectedType = _modelTypeComboBox.SelectedIndex == 0 ? "full" : "lite";
            var isSelectedInstalled = UserSettings.IsModelInstalled(selectedType);

            _downloadButton.Enabled = !_isDownloading && UserSettings.IsDownloadScriptAvailable();

            if (!UserSettings.IsDownloadScriptAvailable())
            {
                _downloadButton.Text = "📥 Download Script Not Found";
            }
        }

        /// <summary>
        /// Handles model type selection changes.
        /// </summary>
        private void ModelTypeComboBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
            UpdateModelStatus();
            UpdateDownloadButtonState();
        }

        /// <summary>
        /// Handles download button click.
        /// </summary>
        private async void DownloadButton_Click(object? sender, EventArgs e)
        {
            if (_isDownloading)
            {
                return;
            }

            var selectedType = _modelTypeComboBox.SelectedIndex == 0 ? "full" : "lite";
            var isSelectedInstalled = UserSettings.IsModelInstalled(selectedType);

            if (isSelectedInstalled)
            {
                var result = MessageBox.Show(
                    $"The {(selectedType == "full" ? "Full" : "Lite")} model is already installed.\n\nDo you want to re-download it?",
                    "Model Already Installed",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result != DialogResult.Yes)
                {
                    return;
                }
            }

            await DownloadModelAsync(selectedType);
        }

        /// <summary>
        /// Downloads the specified model type.
        /// </summary>
        private async Task DownloadModelAsync(string modelType)
        {
            _isDownloading = true;
            _downloadButton.Enabled = false;
            _saveButton.Enabled = false;
            _modelTypeComboBox.Enabled = false;

            var progressPanel = (Panel)_downloadProgress.Parent!;
            progressPanel.Visible = true;
            _downloadStatusLabel.Text = $"Downloading {(modelType == "full" ? "Full" : "Lite")} model...";

            try
            {
                _downloadCts = new CancellationTokenSource();

                var scriptPath = UserSettings.GetDownloadScriptPath();
                // Use model-type-specific path so both models can coexist
                var modelPath = UserSettings.GetModelFilePath(modelType);

                var startInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-ExecutionPolicy Bypass -File \"{scriptPath}\" -DestinationPath \"{modelPath}\" -ModelType {modelType}",
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Normal
                };

                var process = Process.Start(startInfo);

                if (process != null)
                {
                    _downloadStatusLabel.Text = "Download window opened. Please wait for it to complete...";

                    await Task.Run(() =>
                    {
                        try
                        {
                            process.WaitForExit();
                        }
                        catch
                        {
                            // Process may have been terminated
                        }
                    }, _downloadCts.Token);

                    if (process.ExitCode == 0)
                    {
                        _downloadStatusLabel.Text = "✅ Download complete!";
                        _downloadStatusLabel.ForeColor = Color.Green;

                        // Update settings with the new model type and save
                        UserSettings.Instance.ModelType = modelType;
                        UserSettings.Instance.Save();

                        // Request model reload
                        ModelReloadRequested?.Invoke(this, EventArgs.Empty);

                        MessageBox.Show(
                            $"The {(modelType == "full" ? "Full" : "Lite")} model has been downloaded successfully!\n\nAI analysis will use the new model.",
                            "Download Complete",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                    }
                    else
                    {
                        _downloadStatusLabel.Text = "⚠️ Download may have failed. Check the download window.";
                        _downloadStatusLabel.ForeColor = Color.Orange;
                    }
                }
            }
            catch (Exception ex)
            {
                _downloadStatusLabel.Text = $"❌ Error: {ex.Message}";
                _downloadStatusLabel.ForeColor = Color.Red;

                MessageBox.Show(
                    $"Failed to start the download:\n\n{ex.Message}",
                    "Download Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                _isDownloading = false;
                _downloadButton.Enabled = true;
                _saveButton.Enabled = true;
                _modelTypeComboBox.Enabled = true;
                progressPanel.Visible = false;
                _downloadCts?.Dispose();
                _downloadCts = null;

                UpdateModelStatus();
                UpdateDownloadButtonState();
            }
        }

        /// <summary>
        /// Handles the save button click.
        /// </summary>
        private void SaveButton_Click(object? sender, EventArgs e)
        {
            var settings = UserSettings.Instance;
            var oldModelType = settings.ValidatedModelType;
            var newModelType = _modelTypeComboBox.SelectedIndex == 0 ? "full" : "lite";

            settings.TopProcessCount = (int)_processCountNumeric.Value;
            settings.ModelType = newModelType;
            settings.Save();

            // Check if the selected model is installed
            var isNewModelInstalled = UserSettings.IsModelInstalled(newModelType);
            
            if (!isNewModelInstalled)
            {
                // Model not installed - ask if they want to download
                var result = MessageBox.Show(
                    $"The {(newModelType == "full" ? "Full" : "Lite")} model is not installed yet.\n\n" +
                    "Would you like to download it now?",
                    "Model Not Installed",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    _ = DownloadModelAsync(newModelType);
                    return; // Don't close the dialog yet
                }
            }
            else if (newModelType != oldModelType)
            {
                // Model is installed and different from before - trigger reload
                ModelReloadRequested?.Invoke(this, EventArgs.Empty);
                
                MessageBox.Show(
                    $"Switched to the {(newModelType == "full" ? "Full" : "Lite")} model.\n\nAI analysis will now use the new model.",
                    "Model Switched",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }

            DialogResult = DialogResult.OK;
            Close();
        }

        /// <inheritdoc/>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_isDownloading)
            {
                var result = MessageBox.Show(
                    "A download is in progress. Are you sure you want to close?",
                    "Download In Progress",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result != DialogResult.Yes)
                {
                    e.Cancel = true;
                    return;
                }

                _downloadCts?.Cancel();
            }

            base.OnFormClosing(e);
        }
    }
}
