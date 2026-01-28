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
    }
}
