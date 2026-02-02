// -----------------------------------------------------------------------
// <copyright file="ILoggingService.cs" company="TrayPerformanceMonitor">
//     Copyright (c) TrayPerformanceMonitor. All rights reserved.
// </copyright>
// <summary>
//     Defines the contract for performance spike logging operations.
// </summary>
// -----------------------------------------------------------------------

namespace TrayPerformanceMonitor.Services.Interfaces
{
    /// <summary>
    /// Defines the contract for a service that logs performance spike events to persistent storage.
    /// </summary>
    public interface ILoggingService : IDisposable
    {
        /// <summary>
        /// Logs a performance spike event with associated process information.
        /// </summary>
        /// <param name="metricName">The name of the metric that spiked (e.g., "CPU", "RAM").</param>
        /// <param name="value">The current value of the metric.</param>
        /// <param name="topProcessesInfo">Formatted string containing top process information.</param>
        void LogPerformanceSpike(string metricName, float value, string topProcessesInfo);

        /// <summary>
        /// Logs a performance spike event with associated process information and an AI-generated summary.
        /// </summary>
        /// <param name="metricName">The name of the metric that spiked (e.g., "CPU", "RAM").</param>
        /// <param name="value">The current value of the metric.</param>
        /// <param name="topProcessesInfo">Formatted string containing top process information.</param>
        /// <param name="aiSummary">An AI-generated summary with analysis and recommendations.</param>
        void LogPerformanceSpike(string metricName, float value, string topProcessesInfo, string? aiSummary);

        /// <summary>
        /// Appends an AI-generated summary to the log file.
        /// Used when AI summary is generated asynchronously after the initial spike log entry.
        /// </summary>
        /// <param name="metricName">The name of the metric the summary is for.</param>
        /// <param name="spikeTime">The timestamp of the spike this summary relates to.</param>
        /// <param name="aiSummary">The AI-generated summary to append.</param>
        void AppendAiSummary(string metricName, DateTime spikeTime, string aiSummary);

        /// <summary>
        /// Ensures the log file has a header for the current day.
        /// </summary>
        void EnsureDailyHeader();

        /// <summary>
        /// Removes log entries older than the configured retention period.
        /// </summary>
        /// <remarks>
        /// This method reads the existing log file, filters out entries from dates
        /// older than <see cref="Configuration.AppConfiguration.LogRetentionDays"/>,
        /// and rewrites the file with only the retained entries.
        /// </remarks>
        void PruneOldEntries();
    }
}
