// -----------------------------------------------------------------------
// <copyright file="AppConfiguration.cs" company="TrayPerformanceMonitor">
//     Copyright (c) TrayPerformanceMonitor. All rights reserved.
// </copyright>
// <summary>
//     Contains application-wide configuration constants and settings.
// </summary>
// -----------------------------------------------------------------------

namespace TrayPerformanceMonitor.Configuration
{
    /// <summary>
    /// Provides centralized configuration constants for the application.
    /// All threshold values and timing intervals are defined here for easy maintenance.
    /// </summary>
    public static class AppConfiguration
    {
        /// <summary>
        /// Gets the timer interval in milliseconds for performance metric updates.
        /// </summary>
        public const int TimerIntervalMs = 500;

        /// <summary>
        /// Gets the CPU usage threshold percentage that triggers spike detection.
        /// </summary>
        public const float CpuThreshold = 80.0f;

        /// <summary>
        /// Gets the RAM usage threshold percentage that triggers spike detection.
        /// </summary>
        public const float RamThreshold = 80.0f;

        /// <summary>
        /// Gets the number of seconds a metric must exceed the threshold 
        /// before being logged as a sustained spike.
        /// </summary>
        public const int SpikeTimeThresholdSeconds = 10;

        /// <summary>
        /// Gets the number of top processes to display when logging a spike event.
        /// </summary>
        public const int TopProcessCount = 3;

        /// <summary>
        /// Gets the interval in milliseconds for the status window keep-pinned timer.
        /// </summary>
        public const int KeepPinnedIntervalMs = 250;

        /// <summary>
        /// Gets the log file name stored on the user's Desktop.
        /// </summary>
        public const string LogFileName = "TrayPerformanceMonitor_log.txt";

        /// <summary>
        /// Gets the application display name shown in the tray icon tooltip.
        /// </summary>
        public const string ApplicationName = "Performance Monitor";

        /// <summary>
        /// Gets the number of days of log entries to retain in the log file.
        /// Log entries older than this threshold will be pruned on startup.
        /// </summary>
        public const int LogRetentionDays = 7;

        // =====================================================================
        // AI Summary Configuration
        // =====================================================================

        /// <summary>
        /// Gets the default filename for the AI model (GGUF format).
        /// Recommended models for laptops: TinyLlama-1.1B, Phi-3-mini, or Llama-3.2-1B.
        /// Place the model file in the application directory or specify full path.
        /// </summary>
        public const string AiModelFileName = "model.gguf";

        /// <summary>
        /// Gets the context size for AI inference.
        /// Smaller values use less RAM but limit input length.
        /// </summary>
        public const uint AiContextSize = 2048;

        /// <summary>
        /// Gets the maximum number of tokens the AI can generate per response.
        /// </summary>
        public const int AiMaxTokens = 150;

        /// <summary>
        /// Gets the timeout in seconds for AI inference.
        /// If the AI takes longer than this, it will be cancelled.
        /// </summary>
        public const int AiTimeoutSeconds = 30;

        /// <summary>
        /// Gets whether AI summaries are enabled by default.
        /// Can be disabled if no model is available or for performance reasons.
        /// </summary>
        public const bool AiSummaryEnabled = true;
    }
}
