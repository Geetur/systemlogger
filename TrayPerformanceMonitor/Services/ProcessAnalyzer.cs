// -----------------------------------------------------------------------
// <copyright file="ProcessAnalyzer.cs" company="TrayPerformanceMonitor">
//     Copyright (c) TrayPerformanceMonitor. All rights reserved.
// </copyright>
// <summary>
//     Provides analysis of process resource consumption.
// </summary>
// -----------------------------------------------------------------------

using System.Diagnostics;
using TrayPerformanceMonitor.Configuration;
using TrayPerformanceMonitor.Services.Interfaces;

namespace TrayPerformanceMonitor.Services
{
    /// <summary>
    /// Analyzes and reports on process resource consumption using delta-based CPU sampling
    /// for accurate CPU usage calculations and working set for memory usage.
    /// </summary>
    /// <remarks>
    /// CPU usage is calculated using delta-based sampling, which measures the actual
    /// CPU time consumed between two sample points. This provides more accurate readings
    /// than instantaneous snapshots.
    /// </remarks>
    public sealed class ProcessAnalyzer : IProcessAnalyzer
    {
        private readonly Dictionary<int, TimeSpan> _lastProcessCpuTimes = new();
        private DateTime _lastSampleTime = DateTime.UtcNow;
        private readonly object _sampleLock = new();

        /// <inheritdoc/>
        public string GetTopCpuProcesses(int count)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);

            var processInfoList = new List<ProcessResourceInfo>();

            lock (_sampleLock)
            {
                var currentTime = DateTime.UtcNow;
                var elapsedSeconds = (currentTime - _lastSampleTime).TotalSeconds;

                // Use timer interval as minimum to avoid division by zero
                if (elapsedSeconds <= 0)
                {
                    elapsedSeconds = AppConfiguration.TimerIntervalMs / 1000.0;
                }

                _lastSampleTime = currentTime;

                foreach (var process in Process.GetProcesses())
                {
                    try
                    {
                        var processId = process.Id;
                        var processName = process.ProcessName;
                        var currentCpuTime = process.TotalProcessorTime;

                        // Get previous CPU time for this process (defaults to TimeSpan.Zero)
                        _lastProcessCpuTimes.TryGetValue(processId, out var previousCpuTime);
                        _lastProcessCpuTimes[processId] = currentCpuTime;

                        // Calculate CPU percentage based on delta
                        var cpuTimeDelta = currentCpuTime - previousCpuTime;
                        var cpuPercentage = (cpuTimeDelta.TotalSeconds / (elapsedSeconds * Environment.ProcessorCount)) * 100.0;

                        // Clamp to non-negative values
                        cpuPercentage = Math.Max(0, cpuPercentage);

                        processInfoList.Add(new ProcessResourceInfo(processName, processId, cpuPercentage));
                    }
                    catch (Exception)
                    {
                        // Process may have exited or access may be denied
                    }
                    finally
                    {
                        try
                        {
                            process.Dispose();
                        }
                        catch (Exception)
                        {
                            // Ignore disposal errors
                        }
                    }
                }

                // Clean up stale entries for processes that no longer exist
                CleanupStaleProcessEntries();
            }

            return FormatProcessList(processInfoList.OrderByDescending(p => p.UsageValue).Take(count), "{0:F1}%");
        }

        /// <inheritdoc/>
        public string GetTopRamProcesses(int count)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);

            var processInfoList = new List<ProcessResourceInfo>();

            foreach (var process in Process.GetProcesses())
            {
                try
                {
                    var memoryMb = process.WorkingSet64 / (1024.0 * 1024.0);
                    processInfoList.Add(new ProcessResourceInfo(process.ProcessName, process.Id, memoryMb));
                }
                catch (Exception)
                {
                    // Process may have exited or access may be denied
                }
                finally
                {
                    try
                    {
                        process.Dispose();
                    }
                    catch (Exception)
                    {
                        // Ignore disposal errors
                    }
                }
            }

            return FormatProcessList(processInfoList.OrderByDescending(p => p.UsageValue).Take(count), "{0:F0} MB");
        }

        /// <summary>
        /// Formats a collection of process information into a human-readable string.
        /// </summary>
        /// <param name="processes">The processes to format.</param>
        /// <param name="valueFormat">The format string for the usage value (e.g., "{0:F1}%" for CPU, "{0:F0} MB" for RAM).</param>
        /// <returns>A formatted string listing the processes.</returns>
        private static string FormatProcessList(IEnumerable<ProcessResourceInfo> processes, string valueFormat)
        {
            return string.Join(
                Environment.NewLine,
                processes.Select(p => $"  - {p.ProcessName} (PID {p.ProcessId}): {string.Format(valueFormat, p.UsageValue)}"));
        }

        /// <summary>
        /// Removes entries from the CPU time dictionary for processes that no longer exist.
        /// </summary>
        private void CleanupStaleProcessEntries()
        {
            var currentProcessIds = new HashSet<int>();

            try
            {
                foreach (var process in Process.GetProcesses())
                {
                    try
                    {
                        currentProcessIds.Add(process.Id);
                    }
                    finally
                    {
                        try
                        {
                            process.Dispose();
                        }
                        catch (Exception)
                        {
                            // Ignore disposal errors
                        }
                    }
                }
            }
            catch (Exception)
            {
                // If we can't get the process list, skip cleanup
                return;
            }

            // Remove entries for processes that no longer exist
            var staleIds = _lastProcessCpuTimes.Keys.Where(id => !currentProcessIds.Contains(id)).ToList();
            foreach (var id in staleIds)
            {
                _lastProcessCpuTimes.Remove(id);
            }
        }
    }
}
