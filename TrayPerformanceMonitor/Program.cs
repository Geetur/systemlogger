using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Media;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace TrayPerformanceMonitor
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Application.Run(new TrayAppContext());
        }
    }

    internal sealed class TrayAppContext : ApplicationContext
    {
        private readonly NotifyIcon trayIcon;
        private readonly StatusWindow statusWindow;
        private readonly StreamWriter logFile;
        private readonly string logPath;
        private readonly object logLock = new object();
        private readonly HashSet<string> knownDayHeaders;
        private DateTime lastHeaderDate = DateTime.MinValue;
        private readonly System.Windows.Forms.Timer timer;
        private const int timerIntervalMs = 500;
        private readonly PerformanceCounter cpuCounter;

        private const float CPUThreshold = 80.0f;
        private const float RAMThreshold = 80.0f;
        private double secondsAboveCPUThreshold;
        private double secondsAboveRAMThreshold;

        // Prevent spamming logs: only log once per "spike event"
        private const int spikeTimeThreshold = 10; // seconds
        private bool cpuSpikeActive;
        private bool ramSpikeActive;

        // For top CPU processes (delta-based sampling)
        private readonly Dictionary<int, TimeSpan> lastProcCpu = new();
        private DateTime lastProcSampleAt = DateTime.UtcNow;

        public TrayAppContext()
        {
            trayIcon = new NotifyIcon
            {
                Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath),
                ContextMenuStrip = new ContextMenuStrip(),
                Visible = true,
                Text = "Performance Monitor"
            };

            _ = trayIcon.ContextMenuStrip.Items.Add("Show Performance", null, ShowPerformance);
            trayIcon.ContextMenuStrip.Items.Add("Exit", null, Exit);

            // Create a small status window positioned above the taskbar (bottom-right)
            statusWindow = new StatusWindow(() => ShowPerformance(null, EventArgs.Empty), () => Exit(null, EventArgs.Empty));

            // Ensure the status window stays visible when the tray context menu opens
            try
            {
                trayIcon.ContextMenuStrip.Opening += (_, __) => { try { statusWindow?.EnsurePinned(); } catch { } };
                trayIcon.MouseUp += (s, e) => { if (e.Button == MouseButtons.Right) { try { statusWindow?.EnsurePinned(); } catch { } } };
            }
            catch { }

            // Defer showing until the message loop runs to ensure the window is visible
            Application.Idle += ShowStatusWindowOnce;

            // keep the tray icon visible alongside the taskbar-adjacent status window
            trayIcon.Visible = true;

            // Create a persistent log file on the user's Desktop (single file with per-day headers)
            try
            {
                var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                logPath = Path.Combine(desktop, "TrayPerformanceMonitor_log.txt");
            }
            catch
            {
                // fallback to local rolling file if Desktop isn't writable
                logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TrayPerformanceMonitor_log.txt");
            }

            knownDayHeaders = LoadExistingHeaders(logPath);
            logFile = new StreamWriter(logPath, true) { AutoFlush = true };
            lock (logLock)
            {
                EnsureDailyHeaderLocked();
            }
            cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            _ = cpuCounter.NextValue(); // prime the counter

            // Run every 500ms like your original Sleep(500)
            timer = new System.Windows.Forms.Timer { Interval = timerIntervalMs };
            timer.Tick += (_, __) => UpdatePerformanceMetrics();
            timer.Start();

            UpdatePerformanceMetrics(); // initial update
        }

        private void ShowStatusWindowOnce(object? s, EventArgs e)
        {
            Application.Idle -= ShowStatusWindowOnce;
            try
            {
                statusWindow?.Show();

                // IMPORTANT: keep it always visible without stealing focus
                statusWindow?.EnsurePinned();
            }
            catch { }
        }

        private void ShowPerformance(object? sender, EventArgs e)
        {
            UpdatePerformanceMetrics();
            MessageBox.Show(trayIcon.Text, "Current Performance Metrics");
        }

        private void UpdatePerformanceMetrics()
        {
            var cpuUsage = GetCpuUsage();
            var memoryUsage = GetMemoryUsage();
            trayIcon.Text = $"CPU: {cpuUsage}%\n RAM: {memoryUsage}%";

            try { statusWindow?.UpdateStatus(cpuUsage, memoryUsage); } catch { }

            if (cpuUsage > CPUThreshold)
            {
                secondsAboveCPUThreshold += timerIntervalMs / 1000.0;
                if (secondsAboveCPUThreshold >= spikeTimeThreshold)
                {
                    if (!cpuSpikeActive)
                    {
                        cpuSpikeActive = true;
                        logPerformanceSpike("CPU", cpuUsage);
                    }
                }
            }
            else
            {
                secondsAboveCPUThreshold = 0;
                cpuSpikeActive = false;
            }

            // RAM sustained spike
            if (memoryUsage > RAMThreshold)
            {
                secondsAboveRAMThreshold += timerIntervalMs / 1000.0;
                if (secondsAboveRAMThreshold >= spikeTimeThreshold)
                {
                    if (!ramSpikeActive)
                    {
                        ramSpikeActive = true;
                        logPerformanceSpike("RAM", memoryUsage);
                    }
                }
            }
            else
            {
                secondsAboveRAMThreshold = 0;
                ramSpikeActive = false;
            }
        }

        private float GetCpuUsage()
        {
            try
            {
                var v = cpuCounter.NextValue();
                if (float.IsNaN(v) || float.IsInfinity(v)) return 0;
                if (v > 100.0f) v = 100.0f;
                return v;
            }
            catch
            {
                return 0;
            }
        }

        private float GetMemoryUsage()
        {
            try
            {
                MEMORYSTATUSEX mem = new();
                mem.dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>();
                if (!GlobalMemoryStatusEx(ref mem)) return 0f;

                ulong totalMemory = mem.ullTotalPhys;
                ulong availableMemory = mem.ullAvailPhys;
                if (totalMemory == 0) return 0f;

                return (float)((totalMemory - availableMemory) * 100.0 / totalMemory);
            }
            catch
            {
                return 0f;
            }
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
        }

        private void Exit(object? sender, EventArgs e)
        {
            timer.Stop();

            // keep UI tidy: close the status window and hide the tray icon
            try { statusWindow?.Close(); } catch { }
            trayIcon.Visible = false;

            try { cpuCounter.Dispose(); } catch { }
            try { logFile.Dispose(); } catch { }
            try { trayIcon.Dispose(); } catch { }
            try { timer.Dispose(); } catch { }

            Application.Exit();
        }

        private string getTopCPUProcesses()
        {
            // Sample CPU deltas for this tick; best results after app has been running a moment
            var now = DateTime.UtcNow;
            var elapsedSec = (now - lastProcSampleAt).TotalSeconds;
            if (elapsedSec <= 0) elapsedSec = timerIntervalMs / 1000.0; // since our timer is ms
            lastProcSampleAt = now;

            var top = new List<(string Name, int Pid, double CpuPct)>();

            foreach (var p in Process.GetProcesses())
            {
                try
                {
                    var pid = p.Id;
                    var name = p.ProcessName;

                    var total = p.TotalProcessorTime;
                    lastProcCpu.TryGetValue(pid, out var last);
                    lastProcCpu[pid] = total;

                    var delta = total - last;
                    var cpuPct = (delta.TotalSeconds / (elapsedSec * Environment.ProcessorCount)) * 100.0;
                    if (cpuPct < 0) cpuPct = 0;

                    top.Add((name, pid, cpuPct));
                }
                catch
                {
                    // Access denied / exited
                }
                finally
                {
                    try { p.Dispose(); } catch { }
                }
            }

            var topProcesses = top
                .OrderByDescending(p => p.CpuPct)
                .Take(3);

            return string.Join(Environment.NewLine, topProcesses.Select(p => $"  - {p.Name} (PID {p.Pid}): {p.CpuPct:F1}%"));
        }

        private string getTopRAMProcesses()
        {
            var top = new List<(string Name, int Pid, double RamMb)>();

            foreach (var p in Process.GetProcesses())
            {
                try
                {
                    top.Add((p.ProcessName, p.Id, p.WorkingSet64 / (1024.0 * 1024.0)));
                }
                catch
                {
                    // Access denied / exited
                }
                finally
                {
                    try { p.Dispose(); } catch { }
                }
            }

            var topProcesses = top
                .OrderByDescending(p => p.RamMb)
                .Take(3);

            return string.Join(Environment.NewLine, topProcesses.Select(p => $"  - {p.Name} (PID {p.Pid}): {p.RamMb:F0} MB"));
        }

        private static string BuildDayHeader(DateTime dt)
        {
            return $"===== {dt:yyyy-MM-dd} ({dt:dddd}) =====";
        }

        private static HashSet<string> LoadExistingHeaders(string path)
        {
            var headers = new HashSet<string>(StringComparer.Ordinal);
            try
            {
                if (!File.Exists(path)) return headers;

                foreach (var line in File.ReadLines(path))
                {
                    var trimmed = line?.Trim();
                    if (string.IsNullOrEmpty(trimmed)) continue;
                    if (trimmed.StartsWith("=====", StringComparison.Ordinal) && trimmed.EndsWith("=====") && trimmed.Length > 10)
                    {
                        headers.Add(trimmed);
                    }
                }
            }
            catch
            {
                // If we can't read, we just won't pre-load headers.
            }

            return headers;
        }

        private void EnsureDailyHeaderLocked()
        {
            var today = DateTime.Now.Date;
            var todayHeader = BuildDayHeader(DateTime.Now);

            if (lastHeaderDate == today && knownDayHeaders.Contains(todayHeader))
            {
                return;
            }

            if (!knownDayHeaders.Contains(todayHeader))
            {
                logFile.WriteLine();
                logFile.WriteLine(todayHeader);
                logFile.WriteLine($"Started: {DateTime.Now:HH:mm:ss}  (Local)");
                logFile.WriteLine(new string('-', 48));
                knownDayHeaders.Add(todayHeader);
            }

            lastHeaderDate = today;
        }

        private void logPerformanceSpike(string metric, float value)
        {
            try
            {
                lock (logLock)
                {
                    EnsureDailyHeaderLocked();

                    string logEntry = $"{DateTime.Now:HH:mm:ss} - {metric} spike detected (>= {spikeTimeThreshold}s): {value:F1}%";
                    logFile.WriteLine(logEntry);

                    if (metric == "CPU")
                    {
                        logFile.WriteLine("Top CPU-consuming processes:");
                        logFile.WriteLine(getTopCPUProcesses());
                    }
                    else if (metric == "RAM")
                    {
                        logFile.WriteLine("Top RAM-consuming processes:");
                        logFile.WriteLine(getTopRAMProcesses());
                    }

                    logFile.WriteLine();
                }
            }
            catch
            {
                // Don't crash if disk is locked / permissions / etc.
            }
        }
    }
}
