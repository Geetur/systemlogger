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
    /// The file is opened with <see cref="FileShare.ReadWrite"/> to allow users to open
    /// the file in any text editor while the application continues logging. If writing fails
    /// (e.g., file locked by another application), entries are cached in memory
    /// and flushed when the file becomes available.
    /// </remarks>
    public sealed class LoggingService : ILoggingService
    {
        private const int MaxCachedEntries = 1000;

        private readonly string _logPath;
        private readonly object _logLock = new();
        private readonly List<string> _memoryCache = new();
        private HashSet<string> _knownDayHeaders;
        private FileStream? _fileStream;
        private StreamWriter? _logFile;
        private DateTime _lastHeaderDate = DateTime.MinValue;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoggingService"/> class.
        /// </summary>
        /// <remarks>
        /// Creates or opens the log file on the user's Desktop. If the Desktop
        /// is not accessible, falls back to the application's base directory.
        /// Old log entries are pruned on startup based on <see cref="AppConfiguration.LogRetentionDays"/>.
        /// The file is opened with read-sharing enabled so users can view it.
        /// </remarks>
        public LoggingService()
        {
            _logPath = DetermineLogFilePath();

            // Prune old entries before opening the log file for writing
            PruneOldEntriesInternal();

            _knownDayHeaders = LoadExistingHeaders(_logPath);
            
            // Try to open the file with read-sharing enabled
            TryOpenLogFile();

            // Ensure daily header is written on startup
            lock (_logLock)
            {
                EnsureDailyHeaderLocked();
            }
        }

        /// <inheritdoc/>
        public void LogPerformanceSpike(string metricName, float value, string topProcessesInfo)
        {
            LogPerformanceSpike(metricName, value, topProcessesInfo, aiSummary: null);
        }

        /// <inheritdoc/>
        public void LogPerformanceSpike(string metricName, float value, string topProcessesInfo, string? aiSummary)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(metricName);
            ArgumentException.ThrowIfNullOrWhiteSpace(topProcessesInfo);
            ObjectDisposedException.ThrowIf(_disposed, this);

            var entries = new List<string>
            {
                $"{DateTime.Now:HH:mm:ss} - {metricName} spike detected (>= {AppConfiguration.SpikeTimeThresholdSeconds}s): {value:F1}%",
                $"Top {metricName}-consuming processes:",
                topProcessesInfo
            };

            // Add AI summary if provided
            if (!string.IsNullOrWhiteSpace(aiSummary))
            {
                entries.Add(string.Empty);
                entries.Add("AI Analysis:");
                // Indent each line of the AI summary for better readability
                foreach (var line in aiSummary.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                {
                    entries.Add($"  {line.Trim()}");
                }
            }

            entries.Add(string.Empty);

            lock (_logLock)
            {
                // Try to flush any cached entries first
                FlushMemoryCacheLocked();
                
                // Ensure daily header exists
                EnsureDailyHeaderLocked();

                // Try to write directly to file
                if (!TryWriteLinesToFileLocked(entries))
                {
                    // File is locked, cache the entries
                    CacheEntriesLocked(entries);
                }
            }
        }

        /// <inheritdoc/>
        public void AppendAiSummary(string metricName, DateTime spikeTime, string aiSummary)
        {
            if (string.IsNullOrWhiteSpace(aiSummary))
            {
                return;
            }

            ObjectDisposedException.ThrowIf(_disposed, this);

            var entries = new List<string>
            {
                $"AI Analysis (for {metricName} spike at {spikeTime:HH:mm:ss}):",
            };

            // Indent each line of the AI summary for better readability
            foreach (var line in aiSummary.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                entries.Add($"  {line.Trim()}");
            }

            entries.Add(string.Empty);

            lock (_logLock)
            {
                // Try to flush any cached entries first
                FlushMemoryCacheLocked();

                // Try to write directly to file
                if (!TryWriteLinesToFileLocked(entries))
                {
                    // File is locked, cache the entries
                    CacheEntriesLocked(entries);
                }
            }
        }

        /// <inheritdoc/>
        public void EnsureDailyHeader()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            lock (_logLock)
            {
                // Try to flush cached entries when checking for daily header
                FlushMemoryCacheLocked();
                EnsureDailyHeaderLocked();
            }
        }

        /// <summary>
        /// Gets the number of entries currently cached in memory.
        /// </summary>
        /// <remarks>
        /// Entries are cached when the log file is locked by another application.
        /// </remarks>
        public int CachedEntryCount
        {
            get
            {
                lock (_logLock)
                {
                    return _memoryCache.Count;
                }
            }
        }

        /// <summary>
        /// Attempts to flush any cached entries to the log file.
        /// </summary>
        /// <returns><c>true</c> if all cached entries were flushed; otherwise, <c>false</c>.</returns>
        public bool TryFlushCache()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            lock (_logLock)
            {
                return FlushMemoryCacheLocked();
            }
        }

        /// <inheritdoc/>
        public void PruneOldEntries()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            lock (_logLock)
            {
                // Flush cache before pruning
                FlushMemoryCacheLocked();

                // Close the current file before pruning
                CloseLogFileLocked();

                PruneOldEntriesInternal();

                // Reopen the log file and reload headers
                _knownDayHeaders = LoadExistingHeaders(_logPath);
                TryOpenLogFile();
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            lock (_logLock)
            {
                // Try one final flush before disposing
                FlushMemoryCacheLocked();
                CloseLogFileLocked();
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
        /// Attempts to open the log file with read-write sharing enabled.
        /// </summary>
        /// <returns><c>true</c> if the file was opened successfully; otherwise, <c>false</c>.</returns>
        private bool TryOpenLogFile()
        {
            try
            {
                if (_logFile != null)
                {
                    return true;
                }

                // Ensure the file is not marked as read-only
                EnsureFileWritable(_logPath);

                // Open with FileShare.ReadWrite to allow users to open the file
                // in any text editor while we continue writing to it
                _fileStream = new FileStream(
                    _logPath,
                    FileMode.OpenOrCreate,
                    FileAccess.Write,
                    FileShare.ReadWrite);

                _fileStream.Seek(0, SeekOrigin.End);
                _logFile = new StreamWriter(_fileStream) { AutoFlush = true };

                return true;
            }
            catch (Exception)
            {
                // File might be locked by another application
                _fileStream?.Dispose();
                _fileStream = null;
                _logFile = null;
                return false;
            }
        }

        /// <summary>
        /// Ensures the file is writable by removing the read-only attribute if set.
        /// </summary>
        /// <param name="filePath">The path to the file.</param>
        private static void EnsureFileWritable(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    return;
                }

                var attributes = File.GetAttributes(filePath);
                if ((attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                {
                    File.SetAttributes(filePath, attributes & ~FileAttributes.ReadOnly);
                }
            }
            catch (Exception)
            {
                // If we can't modify attributes, the file open will fail and we'll cache instead
            }
        }

        /// <summary>
        /// Closes the log file if it's open.
        /// </summary>
        private void CloseLogFileLocked()
        {
            try
            {
                _logFile?.Dispose();
                _fileStream?.Dispose();
            }
            catch (Exception)
            {
                // Swallow disposal exceptions
            }
            finally
            {
                _logFile = null;
                _fileStream = null;
            }
        }

        /// <summary>
        /// Attempts to write lines to the log file.
        /// </summary>
        /// <param name="lines">The lines to write.</param>
        /// <returns><c>true</c> if the lines were written successfully; otherwise, <c>false</c>.</returns>
        private bool TryWriteLinesToFileLocked(IEnumerable<string> lines)
        {
            try
            {
                // Try to reopen the file if it's not open
                if (_logFile == null && !TryOpenLogFile())
                {
                    return false;
                }

                foreach (var line in lines)
                {
                    _logFile!.WriteLine(line);
                }

                return true;
            }
            catch (Exception)
            {
                // File write failed, close the file handle
                CloseLogFileLocked();
                return false;
            }
        }

        /// <summary>
        /// Caches log entries in memory when file writing is unavailable.
        /// </summary>
        /// <param name="entries">The entries to cache.</param>
        private void CacheEntriesLocked(IEnumerable<string> entries)
        {
            foreach (var entry in entries)
            {
                if (_memoryCache.Count >= MaxCachedEntries)
                {
                    // Remove oldest entries to prevent unbounded memory growth
                    _memoryCache.RemoveAt(0);
                }

                _memoryCache.Add(entry);
            }
        }

        /// <summary>
        /// Attempts to flush cached entries to the log file.
        /// </summary>
        /// <returns><c>true</c> if all cached entries were flushed; otherwise, <c>false</c>.</returns>
        private bool FlushMemoryCacheLocked()
        {
            if (_memoryCache.Count == 0)
            {
                return true;
            }

            try
            {
                // Try to reopen the file if needed
                if (_logFile == null && !TryOpenLogFile())
                {
                    return false;
                }

                // Write all cached entries
                foreach (var entry in _memoryCache)
                {
                    _logFile!.WriteLine(entry);
                }

                _memoryCache.Clear();
                return true;
            }
            catch (Exception)
            {
                // File still locked, keep entries in cache
                CloseLogFileLocked();
                return false;
            }
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

                // Use FileShare.ReadWrite to allow reading even if another process has the file open
                using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(stream);
                
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    var trimmed = line.Trim();

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
                var headerLines = new List<string>
                {
                    string.Empty,
                    todayHeader,
                    $"Started: {DateTime.Now:HH:mm:ss}  (Local)",
                    new string('-', 48)
                };

                if (!TryWriteLinesToFileLocked(headerLines))
                {
                    // Cache header lines if file is locked
                    CacheEntriesLocked(headerLines);
                }

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

                // Ensure the file is not marked as read-only before any operations
                EnsureFileWritable(_logPath);

                var cutoffDate = DateTime.Now.Date.AddDays(-AppConfiguration.LogRetentionDays);
                
                // Read all lines using shared access
                List<string> allLines;
                using (var readStream = new FileStream(_logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var reader = new StreamReader(readStream))
                {
                    allLines = new List<string>();
                    string? line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        allLines.Add(line);
                    }
                }
                
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
                if (retainedLines.Count < allLines.Count)
                {
                    // Remove leading empty lines from retained content
                    while (retainedLines.Count > 0 && string.IsNullOrWhiteSpace(retainedLines[0]))
                    {
                        retainedLines.RemoveAt(0);
                    }

                    // Ensure writable before writing
                    EnsureFileWritable(_logPath);

                    // Write using shared access
                    using var writeStream = new FileStream(_logPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
                    using var writer = new StreamWriter(writeStream);
                    foreach (var line in retainedLines)
                    {
                        writer.WriteLine(line);
                    }
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
