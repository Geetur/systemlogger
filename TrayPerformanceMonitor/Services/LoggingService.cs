// -----------------------------------------------------------------------
// <copyright file="LoggingService.cs" company="TrayPerformanceMonitor">
//     Copyright (c) TrayPerformanceMonitor. All rights reserved.
// </copyright>
// <summary>
//     Provides file-based logging for performance spike events.
// </summary>
// -----------------------------------------------------------------------

using TrayPerformanceMonitor.Configuration;
using TrayPerformanceMonitor.Services.Interfaces;

namespace TrayPerformanceMonitor.Services
{
    /// <summary>
    /// Provides file-based logging functionality for performance spike events.
    /// Maintains a persistent log file with daily headers for organized event tracking.
    /// </summary>
    /// <remarks>
    /// This service is thread-safe and uses a lock to ensure consistent file writes.
    /// The log file is stored on the user's Desktop by default, with a fallback to
    /// the application's base directory if the Desktop is not accessible.
    /// Log entries older than <see cref="AppConfiguration.LogRetentionDays"/> are
    /// automatically pruned on startup.
    /// </remarks>
    public sealed class LoggingService : ILoggingService
    {
        private readonly string _logPath;
        private readonly object _logLock = new();
        private HashSet<string> _knownDayHeaders;
        private StreamWriter _logFile;
        private DateTime _lastHeaderDate = DateTime.MinValue;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoggingService"/> class.
        /// </summary>
        /// <remarks>
        /// Creates or opens the log file on the user's Desktop. If the Desktop
        /// is not accessible, falls back to the application's base directory.
        /// Old log entries are pruned on startup based on <see cref="AppConfiguration.LogRetentionDays"/>.
        /// </remarks>
        public LoggingService()
        {
            _logPath = DetermineLogFilePath();

            // Prune old entries before opening the log file for writing
            PruneOldEntriesInternal();

            _knownDayHeaders = LoadExistingHeaders(_logPath);
            _logFile = new StreamWriter(_logPath, append: true) { AutoFlush = true };

            // Ensure daily header is written on startup
            lock (_logLock)
            {
                EnsureDailyHeaderLocked();
            }
        }

        /// <inheritdoc/>
        public void LogPerformanceSpike(string metricName, float value, string topProcessesInfo)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(metricName);
            ArgumentException.ThrowIfNullOrWhiteSpace(topProcessesInfo);
            ObjectDisposedException.ThrowIf(_disposed, this);

            try
            {
                lock (_logLock)
                {
                    EnsureDailyHeaderLocked();

                    string logEntry = $"{DateTime.Now:HH:mm:ss} - {metricName} spike detected (>= {AppConfiguration.SpikeTimeThresholdSeconds}s): {value:F1}%";
                    _logFile.WriteLine(logEntry);
                    _logFile.WriteLine($"Top {metricName}-consuming processes:");
                    _logFile.WriteLine(topProcessesInfo);
                    _logFile.WriteLine();
                }
            }
            catch (Exception)
            {
                // Don't crash if disk is locked, permissions denied, etc.
                // In a production app, we might want to log to an alternative location
            }
        }

        /// <inheritdoc/>
        public void EnsureDailyHeader()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            lock (_logLock)
            {
                EnsureDailyHeaderLocked();
            }
        }

        /// <inheritdoc/>
        public void PruneOldEntries()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            lock (_logLock)
            {
                // Close the current file before pruning
                _logFile?.Dispose();

                PruneOldEntriesInternal();

                // Reopen the log file and reload headers
                _knownDayHeaders = LoadExistingHeaders(_logPath);
                _logFile = new StreamWriter(_logPath, append: true) { AutoFlush = true };
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            try
            {
                _logFile?.Dispose();
            }
            catch (Exception)
            {
                // Swallow disposal exceptions
            }

            _disposed = true;
        }

        /// <summary>
        /// Determines the appropriate log file path based on available locations.
        /// </summary>
        /// <returns>The full path to the log file.</returns>
        private static string DetermineLogFilePath()
        {
            try
            {
                var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

                if (!string.IsNullOrEmpty(desktopPath) && Directory.Exists(desktopPath))
                {
                    return Path.Combine(desktopPath, AppConfiguration.LogFileName);
                }
            }
            catch (Exception)
            {
                // Desktop path not available, fall through to base directory
            }

            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, AppConfiguration.LogFileName);
        }

        /// <summary>
        /// Loads existing day headers from the log file to avoid duplicate entries.
        /// </summary>
        /// <param name="path">The path to the log file.</param>
        /// <returns>A set of existing header strings found in the file.</returns>
        private static HashSet<string> LoadExistingHeaders(string path)
        {
            var headers = new HashSet<string>(StringComparer.Ordinal);

            try
            {
                if (!File.Exists(path))
                {
                    return headers;
                }

                foreach (var line in File.ReadLines(path))
                {
                    var trimmed = line?.Trim();

                    if (string.IsNullOrEmpty(trimmed))
                    {
                        continue;
                    }

                    // Match lines like: "===== 2026-01-27 (Monday) ====="
                    if (trimmed.StartsWith("=====", StringComparison.Ordinal) &&
                        trimmed.EndsWith("=====", StringComparison.Ordinal) &&
                        trimmed.Length > 10)
                    {
                        headers.Add(trimmed);
                    }
                }
            }
            catch (Exception)
            {
                // If we can't read, return empty set - headers will be rewritten if needed
            }

            return headers;
        }

        /// <summary>
        /// Builds a formatted header string for a given date.
        /// </summary>
        /// <param name="dateTime">The date to build the header for.</param>
        /// <returns>A formatted header string.</returns>
        private static string BuildDayHeader(DateTime dateTime)
        {
            return $"===== {dateTime:yyyy-MM-dd} ({dateTime:dddd}) =====";
        }

        /// <summary>
        /// Ensures the daily header is written if it hasn't been already.
        /// Must be called within a lock on <see cref="_logLock"/>.
        /// </summary>
        private void EnsureDailyHeaderLocked()
        {
            var today = DateTime.Now.Date;
            var todayHeader = BuildDayHeader(DateTime.Now);

            // Skip if we've already written today's header
            if (_lastHeaderDate == today && _knownDayHeaders.Contains(todayHeader))
            {
                return;
            }

            // Write new header if not already in file
            if (!_knownDayHeaders.Contains(todayHeader))
            {
                _logFile.WriteLine();
                _logFile.WriteLine(todayHeader);
                _logFile.WriteLine($"Started: {DateTime.Now:HH:mm:ss}  (Local)");
                _logFile.WriteLine(new string('-', 48));
                _knownDayHeaders.Add(todayHeader);
            }

            _lastHeaderDate = today;
        }

        /// <summary>
        /// Internal implementation of log pruning that removes entries older than the retention period.
        /// </summary>
        /// <remarks>
        /// This method reads the entire log file, identifies date headers, and removes
        /// all content associated with dates older than <see cref="AppConfiguration.LogRetentionDays"/>.
        /// The file is rewritten with only the retained entries.
        /// </remarks>
        private void PruneOldEntriesInternal()
        {
            try
            {
                if (!File.Exists(_logPath))
                {
                    return;
                }

                var cutoffDate = DateTime.Now.Date.AddDays(-AppConfiguration.LogRetentionDays);
                var allLines = File.ReadAllLines(_logPath);
                var retainedLines = new List<string>();
                var currentSectionDate = DateTime.MinValue;
                var isRetainingCurrentSection = true;

                foreach (var line in allLines)
                {
                    var parsedDate = TryParseDateHeader(line);

                    if (parsedDate.HasValue)
                    {
                        currentSectionDate = parsedDate.Value;
                        isRetainingCurrentSection = currentSectionDate >= cutoffDate;
                    }

                    if (isRetainingCurrentSection)
                    {
                        retainedLines.Add(line);
                    }
                }

                // Only rewrite if we actually removed something
                if (retainedLines.Count < allLines.Length)
                {
                    // Remove leading empty lines from retained content
                    while (retainedLines.Count > 0 && string.IsNullOrWhiteSpace(retainedLines[0]))
                    {
                        retainedLines.RemoveAt(0);
                    }

                    File.WriteAllLines(_logPath, retainedLines);
                }
            }
            catch (Exception)
            {
                // If pruning fails, continue with existing file
                // The application should not crash due to log maintenance issues
            }
        }

        /// <summary>
        /// Attempts to parse a date from a header line.
        /// </summary>
        /// <param name="line">The line to parse.</param>
        /// <returns>The parsed date if the line is a valid header; otherwise, <c>null</c>.</returns>
        private static DateTime? TryParseDateHeader(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                return null;
            }

            var trimmed = line.Trim();

            // Match lines like: "===== 2026-01-27 (Monday) ====="
            if (!trimmed.StartsWith("=====", StringComparison.Ordinal) ||
                !trimmed.EndsWith("=====", StringComparison.Ordinal) ||
                trimmed.Length < 25)
            {
                return null;
            }

            // Extract the date portion: "2026-01-27"
            var startIndex = trimmed.IndexOf(' ') + 1;
            var endIndex = trimmed.IndexOf(' ', startIndex);

            if (startIndex <= 0 || endIndex <= startIndex)
            {
                return null;
            }

            var dateString = trimmed[startIndex..endIndex];

            return DateTime.TryParse(dateString, out var result) ? result.Date : null;
        }
    }
}
