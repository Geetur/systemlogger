// -----------------------------------------------------------------------
// <copyright file="PerformanceService.cs" company="TrayPerformanceMonitor">
//     Copyright (c) TrayPerformanceMonitor. All rights reserved.
// </copyright>
// <summary>
//     Provides system performance monitoring using native Windows APIs.
// </summary>
// -----------------------------------------------------------------------

using System.Diagnostics;
using System.Runtime.InteropServices;
using TrayPerformanceMonitor.Services.Interfaces;

namespace TrayPerformanceMonitor.Services
{
    /// <summary>
    /// Provides system performance monitoring capabilities using Windows Performance Counters
    /// and native Windows APIs for accurate CPU and memory usage metrics.
    /// </summary>
    /// <remarks>
    /// This service uses the Windows Performance Counter infrastructure for CPU metrics
    /// and the GlobalMemoryStatusEx API for memory metrics. The performance counter
    /// requires a priming read before returning accurate values.
    /// </remarks>
    public sealed class PerformanceService : IPerformanceService
    {
        private readonly PerformanceCounter _cpuCounter;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="PerformanceService"/> class.
        /// </summary>
        /// <remarks>
        /// The constructor primes the CPU performance counter with an initial read,
        /// as the first value returned is always zero.
        /// </remarks>
        public PerformanceService()
        {
            _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");

            // Prime the counter - first reading is always 0
            _ = _cpuCounter.NextValue();
        }

        /// <inheritdoc/>
        public PerformanceMetrics GetCurrentMetrics()
        {
            return new PerformanceMetrics(GetCpuUsage(), GetRamUsage());
        }

        /// <inheritdoc/>
        public float GetCpuUsage()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            try
            {
                var value = _cpuCounter.NextValue();

                // Handle edge cases where the counter returns invalid values
                if (float.IsNaN(value) || float.IsInfinity(value))
                {
                    return 0f;
                }

                // Clamp to valid percentage range
                return Math.Min(value, 100f);
            }
            catch (Exception)
            {
                // Return 0 if the counter fails (e.g., access denied)
                return 0f;
            }
        }

        /// <inheritdoc/>
        public float GetRamUsage()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            try
            {
                var memoryStatus = new NativeMethods.MEMORYSTATUSEX
                {
                    dwLength = (uint)Marshal.SizeOf<NativeMethods.MEMORYSTATUSEX>()
                };

                if (!NativeMethods.GlobalMemoryStatusEx(ref memoryStatus))
                {
                    return 0f;
                }

                ulong totalMemory = memoryStatus.ullTotalPhys;
                ulong availableMemory = memoryStatus.ullAvailPhys;

                if (totalMemory == 0)
                {
                    return 0f;
                }

                return (float)((totalMemory - availableMemory) * 100.0 / totalMemory);
            }
            catch (Exception)
            {
                return 0f;
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _cpuCounter?.Dispose();
            _disposed = true;
        }

        /// <summary>
        /// Contains P/Invoke declarations for native Windows APIs.
        /// </summary>
        private static class NativeMethods
        {
            /// <summary>
            /// Retrieves information about the system's current usage of both physical and virtual memory.
            /// </summary>
            /// <param name="lpBuffer">A pointer to a MEMORYSTATUSEX structure that receives information about current memory availability.</param>
            /// <returns>True if the function succeeds; otherwise, false.</returns>
            [DllImport("kernel32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

            /// <summary>
            /// Contains information about the current state of both physical and virtual memory.
            /// </summary>
            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
            public struct MEMORYSTATUSEX
            {
                /// <summary>The size of the structure, in bytes.</summary>
                public uint dwLength;

                /// <summary>A number between 0 and 100 that specifies the approximate percentage of physical memory that is in use.</summary>
                public uint dwMemoryLoad;

                /// <summary>The amount of actual physical memory, in bytes.</summary>
                public ulong ullTotalPhys;

                /// <summary>The amount of physical memory currently available, in bytes.</summary>
                public ulong ullAvailPhys;

                /// <summary>The current committed memory limit for the system or the current process, whichever is smaller, in bytes.</summary>
                public ulong ullTotalPageFile;

                /// <summary>The maximum amount of memory the current process can commit, in bytes.</summary>
                public ulong ullAvailPageFile;

                /// <summary>The size of the user-mode portion of the virtual address space of the calling process, in bytes.</summary>
                public ulong ullTotalVirtual;

                /// <summary>The amount of unreserved and uncommitted memory currently in the user-mode portion of the virtual address space of the calling process, in bytes.</summary>
                public ulong ullAvailVirtual;

                /// <summary>Reserved. This value is always 0.</summary>
                public ulong ullAvailExtendedVirtual;
            }
        }
    }
}
