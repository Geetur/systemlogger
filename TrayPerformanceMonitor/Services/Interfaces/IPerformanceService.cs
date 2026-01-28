// -----------------------------------------------------------------------
// <copyright file="IPerformanceService.cs" company="TrayPerformanceMonitor">
//     Copyright (c) TrayPerformanceMonitor. All rights reserved.
// </copyright>
// <summary>
//     Defines the contract for performance monitoring operations.
// </summary>
// -----------------------------------------------------------------------

namespace TrayPerformanceMonitor.Services.Interfaces
{
    /// <summary>
    /// Represents performance metrics captured at a point in time.
    /// </summary>
    /// <param name="CpuUsagePercent">The current CPU usage percentage (0-100).</param>
    /// <param name="RamUsagePercent">The current RAM usage percentage (0-100).</param>
    public readonly record struct PerformanceMetrics(float CpuUsagePercent, float RamUsagePercent);

    /// <summary>
    /// Defines the contract for a service that monitors system performance metrics.
    /// </summary>
    public interface IPerformanceService : IDisposable
    {
        /// <summary>
        /// Gets the current system performance metrics.
        /// </summary>
        /// <returns>A <see cref="PerformanceMetrics"/> containing current CPU and RAM usage.</returns>
        PerformanceMetrics GetCurrentMetrics();

        /// <summary>
        /// Gets the current CPU usage percentage.
        /// </summary>
        /// <returns>CPU usage as a percentage (0-100).</returns>
        float GetCpuUsage();

        /// <summary>
        /// Gets the current RAM usage percentage.
        /// </summary>
        /// <returns>RAM usage as a percentage (0-100).</returns>
        float GetRamUsage();
    }
}
