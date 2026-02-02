// -----------------------------------------------------------------------
// <copyright file="TrayAppContext.cs" company="TrayPerformanceMonitor">
//     Copyright (c) TrayPerformanceMonitor. All rights reserved.
// </copyright>
// <summary>
//     Provides the main application context for the system tray performance monitor.
// </summary>
// -----------------------------------------------------------------------

using TrayPerformanceMonitor.Configuration;
using TrayPerformanceMonitor.Services;
using TrayPerformanceMonitor.Services.Interfaces;
using TrayPerformanceMonitor.UI;

namespace TrayPerformanceMonitor
{
    /// <summary>
    /// The main application context that manages the system tray icon, status window,
    /// and performance monitoring lifecycle.
    /// </summary>
    /// <remarks>
    /// This class coordinates between the various services to monitor system performance,
    /// display metrics in a status window, and log sustained performance spikes to disk.
    /// AI-powered summaries are generated for each spike if a model is available.
    /// </remarks>
    internal sealed class TrayAppContext : ApplicationContext
    {
        private readonly NotifyIcon _trayIcon;
        private readonly StatusWindow _statusWindow;
        private readonly System.Windows.Forms.Timer _timer;
        private readonly IPerformanceService _performanceService;
        private readonly ILoggingService _loggingService;
        private readonly IProcessAnalyzer _processAnalyzer;
        private readonly IAiSummaryService? _aiSummaryService;

        private readonly SpikeTracker _cpuSpikeTracker;
        private readonly SpikeTracker _ramSpikeTracker;

        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="TrayAppContext"/> class.
        /// </summary>
        public TrayAppContext()
            : this(new PerformanceService(), new LoggingService(), new ProcessAnalyzer(), CreateAiService())
        {
        }

        /// <summary>
        /// Creates and initializes the AI summary service if a model is available.
        /// </summary>
        /// <returns>An initialized AI service, or null if no model is found.</returns>
        private static AiSummaryService? CreateAiService()
        {
            // Check if AI summaries are enabled - this is a compile-time constant
            // but we suppress the warning as it allows easy toggling via config
#pragma warning disable CS0162 // Unreachable code detected
            if (!AppConfiguration.AiSummaryEnabled)
            {
                return null;
            }
#pragma warning restore CS0162

            var aiService = new AiSummaryService();

            // Try to find and load the model from common locations
            var modelPaths = new[]
            {
                // Application directory
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, AppConfiguration.AiModelFileName),
                // User's Desktop
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), AppConfiguration.AiModelFileName),
                // User's Documents
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), AppConfiguration.AiModelFileName),
                // Models subdirectory
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Models", AppConfiguration.AiModelFileName)
            };

            foreach (var modelPath in modelPaths)
            {
                if (aiService.TryLoadModel(modelPath))
                {
                    return aiService;
                }
            }

            // No model found - dispose and return null
            aiService.Dispose();
            return null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TrayAppContext"/> class
        /// with the specified services for dependency injection and testing.
        /// </summary>
        /// <param name="performanceService">The performance monitoring service.</param>
        /// <param name="loggingService">The logging service.</param>
        /// <param name="processAnalyzer">The process analyzer service.</param>
        /// <param name="aiSummaryService">The optional AI summary service.</param>
        public TrayAppContext(
            IPerformanceService performanceService,
            ILoggingService loggingService,
            IProcessAnalyzer processAnalyzer,
            IAiSummaryService? aiSummaryService = null)
        {
            ArgumentNullException.ThrowIfNull(performanceService);
            ArgumentNullException.ThrowIfNull(loggingService);
            ArgumentNullException.ThrowIfNull(processAnalyzer);

            _performanceService = performanceService;
            _loggingService = loggingService;
            _processAnalyzer = processAnalyzer;
            _aiSummaryService = aiSummaryService;

            // Initialize spike trackers
            _cpuSpikeTracker = new SpikeTracker(
                AppConfiguration.CpuThreshold,
                AppConfiguration.SpikeTimeThresholdSeconds);

            _ramSpikeTracker = new SpikeTracker(
                AppConfiguration.RamThreshold,
                AppConfiguration.SpikeTimeThresholdSeconds);

            // Initialize tray icon
            _trayIcon = CreateTrayIcon();

            // Initialize status window
            _statusWindow = new StatusWindow(
                () => ShowPerformanceDialog(),
                () => ExitApplication());

            SetupStatusWindowEventHandlers();

            // Defer showing until the message loop runs
            Application.Idle += ShowStatusWindowOnce;

            // Initialize and start the monitoring timer
            _timer = CreateTimer();

            // Perform initial update
            UpdatePerformanceMetrics();
        }

        /// <summary>
        /// Creates and configures the system tray icon.
        /// </summary>
        /// <returns>The configured <see cref="NotifyIcon"/>.</returns>
        private NotifyIcon CreateTrayIcon()
        {
            var icon = new NotifyIcon
            {
                Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath),
                ContextMenuStrip = new ContextMenuStrip(),
                Visible = true,
                Text = AppConfiguration.ApplicationName
            };

            icon.ContextMenuStrip.Items.Add("Show Performance", null, (_, _) => ShowPerformanceDialog());
            icon.ContextMenuStrip.Items.Add("Exit", null, (_, _) => ExitApplication());

            return icon;
        }

        /// <summary>
        /// Sets up event handlers to keep the status window visible when the context menu opens.
        /// </summary>
        private void SetupStatusWindowEventHandlers()
        {
            try
            {
                _trayIcon.ContextMenuStrip!.Opening += (_, _) =>
                {
                    try
                    {
                        _statusWindow?.EnsurePinned();
                    }
                    catch (Exception)
                    {
                        // Ignore exceptions during pin
                    }
                };

                _trayIcon.MouseUp += (_, e) =>
                {
                    if (e.Button == MouseButtons.Right)
                    {
                        try
                        {
                            _statusWindow?.EnsurePinned();
                        }
                        catch (Exception)
                        {
                            // Ignore exceptions during pin
                        }
                    }
                };
            }
            catch (Exception)
            {
                // Ignore setup failures
            }
        }

        /// <summary>
        /// Creates and configures the performance monitoring timer.
        /// </summary>
        /// <returns>The configured timer.</returns>
        private System.Windows.Forms.Timer CreateTimer()
        {
            var timer = new System.Windows.Forms.Timer
            {
                Interval = AppConfiguration.TimerIntervalMs
            };

            timer.Tick += (_, _) => UpdatePerformanceMetrics();
            timer.Start();

            return timer;
        }

        /// <summary>
        /// Shows the status window once the application's message loop is running.
        /// </summary>
        private void ShowStatusWindowOnce(object? sender, EventArgs e)
        {
            Application.Idle -= ShowStatusWindowOnce;

            try
            {
                _statusWindow?.Show();
                _statusWindow?.EnsurePinned();
            }
            catch (Exception)
            {
                // Ignore display failures
            }
        }

        /// <summary>
        /// Shows a dialog with the current performance metrics.
        /// </summary>
        private void ShowPerformanceDialog()
        {
            UpdatePerformanceMetrics();
            MessageBox.Show(_trayIcon.Text, "Current Performance Metrics");
        }

        /// <summary>
        /// Updates the displayed performance metrics and checks for sustained spikes.
        /// </summary>
        private void UpdatePerformanceMetrics()
        {
            var metrics = _performanceService.GetCurrentMetrics();

            // Update tray icon tooltip
            _trayIcon.Text = $"CPU: {metrics.CpuUsagePercent:F0}%\nRAM: {metrics.RamUsagePercent:F0}%";

            // Update status window
            try
            {
                _statusWindow?.UpdateStatus(metrics.CpuUsagePercent, metrics.RamUsagePercent);
            }
            catch (Exception)
            {
                // Ignore update failures
            }

            // Check for CPU spike
            CheckAndLogSpike(
                _cpuSpikeTracker,
                metrics.CpuUsagePercent,
                "CPU",
                () => _processAnalyzer.GetTopCpuProcesses(AppConfiguration.TopProcessCount));

            // Check for RAM spike
            CheckAndLogSpike(
                _ramSpikeTracker,
                metrics.RamUsagePercent,
                "RAM",
                () => _processAnalyzer.GetTopRamProcesses(AppConfiguration.TopProcessCount));
        }

        /// <summary>
        /// Checks if a performance metric has sustained a spike and logs it if necessary.
        /// </summary>
        /// <param name="tracker">The spike tracker for the metric.</param>
        /// <param name="currentValue">The current value of the metric.</param>
        /// <param name="metricName">The name of the metric (e.g., "CPU", "RAM").</param>
        /// <param name="getTopProcesses">Function to get the top processes consuming this resource.</param>
        private void CheckAndLogSpike(
            SpikeTracker tracker,
            float currentValue,
            string metricName,
            Func<string> getTopProcesses)
        {
            if (tracker.UpdateAndCheckForNewSpike(currentValue, AppConfiguration.TimerIntervalMs))
            {
                var topProcesses = getTopProcesses();
                var spikeTime = DateTime.Now;
                
                // Log immediately without AI summary
                _loggingService.LogPerformanceSpike(metricName, currentValue, topProcesses);

                // Generate AI summary asynchronously in the background
                if (_aiSummaryService?.IsModelLoaded == true)
                {
                    _ = GenerateAndAppendAiSummaryAsync(metricName, currentValue, topProcesses, spikeTime);
                }
            }
        }

        /// <summary>
        /// Generates an AI summary asynchronously and appends it to the log file.
        /// This runs in the background to avoid blocking the UI.
        /// </summary>
        /// <param name="metricName">The name of the metric.</param>
        /// <param name="value">The metric value.</param>
        /// <param name="topProcessesInfo">Information about top processes.</param>
        /// <param name="spikeTime">The timestamp when the spike was detected.</param>
        private async Task GenerateAndAppendAiSummaryAsync(string metricName, float value, string topProcessesInfo, DateTime spikeTime)
        {
            try
            {
                var aiSummary = await _aiSummaryService!.GenerateSpikeSummaryAsync(metricName, value, topProcessesInfo);
                
                if (!string.IsNullOrWhiteSpace(aiSummary))
                {
                    // Append the AI summary to the log with reference to the original spike
                    _loggingService.AppendAiSummary(metricName, spikeTime, aiSummary);
                }
            }
            catch (Exception)
            {
                // AI generation failed - spike was already logged, so no action needed
            }
        }

        /// <summary>
        /// Exits the application gracefully, disposing of all resources.
        /// </summary>
        private void ExitApplication()
        {
            _timer.Stop();

            // Close and dispose UI components
            try
            {
                _statusWindow?.Close();
            }
            catch (Exception)
            {
                // Ignore close failures
            }

            _trayIcon.Visible = false;

            // Dispose all resources
            DisposeResources();

            Application.Exit();
        }

        /// <summary>
        /// Disposes all managed resources.
        /// </summary>
        private void DisposeResources()
        {
            try
            {
                _performanceService?.Dispose();
            }
            catch (Exception)
            {
                // Ignore disposal failures
            }

            try
            {
                _loggingService?.Dispose();
            }
            catch (Exception)
            {
                // Ignore disposal failures
            }

            try
            {
                _aiSummaryService?.Dispose();
            }
            catch (Exception)
            {
                // Ignore disposal failures
            }

            try
            {
                _trayIcon?.Dispose();
            }
            catch (Exception)
            {
                // Ignore disposal failures
            }

            try
            {
                _timer?.Dispose();
            }
            catch (Exception)
            {
                // Ignore disposal failures
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
                DisposeResources();
            }

            _disposed = true;
            base.Dispose(disposing);
        }

        /// <summary>
        /// Tracks sustained performance spikes for a single metric.
        /// </summary>
        /// <remarks>
        /// A spike is considered "sustained" when the metric exceeds the threshold
        /// for a specified duration. Once logged, the spike is not logged again
        /// until the metric drops below the threshold and rises again.
        /// </remarks>
        private sealed class SpikeTracker
        {
            private readonly float _threshold;
            private readonly int _durationThresholdSeconds;
            private double _secondsAboveThreshold;
            private bool _spikeActive;

            /// <summary>
            /// Initializes a new instance of the <see cref="SpikeTracker"/> class.
            /// </summary>
            /// <param name="threshold">The threshold value that triggers spike detection.</param>
            /// <param name="durationThresholdSeconds">The number of seconds the metric must exceed the threshold.</param>
            public SpikeTracker(float threshold, int durationThresholdSeconds)
            {
                _threshold = threshold;
                _durationThresholdSeconds = durationThresholdSeconds;
            }

            /// <summary>
            /// Updates the spike tracker with the current metric value.
            /// </summary>
            /// <param name="currentValue">The current value of the metric.</param>
            /// <param name="intervalMs">The interval in milliseconds since the last update.</param>
            /// <returns>True if this update triggered a new spike event that should be logged; otherwise, false.</returns>
            public bool UpdateAndCheckForNewSpike(float currentValue, int intervalMs)
            {
                if (currentValue > _threshold)
                {
                    _secondsAboveThreshold += intervalMs / 1000.0;

                    if (_secondsAboveThreshold >= _durationThresholdSeconds && !_spikeActive)
                    {
                        _spikeActive = true;
                        return true; // New spike detected
                    }
                }
                else
                {
                    // Reset when below threshold
                    _secondsAboveThreshold = 0;
                    _spikeActive = false;
                }

                return false;
            }
        }
    }
}
