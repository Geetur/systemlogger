// -----------------------------------------------------------------------
// <copyright file="IProcessAnalyzer.cs" company="TrayPerformanceMonitor">
//     Copyright (c) TrayPerformanceMonitor. All rights reserved.
// </copyright>
// <summary>
//     Defines the contract for analyzing process resource usage.
// </summary>
// -----------------------------------------------------------------------

namespace TrayPerformanceMonitor.Services.Interfaces
{
    /// <summary>
    /// Represents information about a process's resource usage.
    /// </summary>
    /// <param name="ProcessName">The name of the process.</param>
    /// <param name="ProcessId">The process identifier.</param>
    /// <param name="UsageValue">The resource usage value (CPU percentage or RAM in MB).</param>
    public readonly record struct ProcessResourceInfo(string ProcessName, int ProcessId, double UsageValue);

    /// <summary>
    /// Defines the contract for a service that analyzes process resource consumption.
    /// </summary>
    public interface IProcessAnalyzer
    {
        /// <summary>
        /// Gets the top CPU-consuming processes with their usage percentages.
        /// </summary>
        /// <param name="count">The number of top processes to return.</param>
        /// <returns>A formatted string listing the top CPU-consuming processes.</returns>
        string GetTopCpuProcesses(int count);

        /// <summary>
        /// Gets the top RAM-consuming processes with their memory usage.
        /// </summary>
        /// <param name="count">The number of top processes to return.</param>
        /// <returns>A formatted string listing the top RAM-consuming processes.</returns>
        string GetTopRamProcesses(int count);
    }
}
