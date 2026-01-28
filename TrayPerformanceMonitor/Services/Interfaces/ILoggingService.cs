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
        /// Ensures the log file has a header for the current day.
        /// </summary>
        void EnsureDailyHeader();
    }
}
