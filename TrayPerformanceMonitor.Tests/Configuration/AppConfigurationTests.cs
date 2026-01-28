// -----------------------------------------------------------------------
// <copyright file="AppConfigurationTests.cs" company="TrayPerformanceMonitor">
//     Copyright (c) TrayPerformanceMonitor. All rights reserved.
// </copyright>
// <summary>
//     Unit tests for the AppConfiguration class.
// </summary>
// -----------------------------------------------------------------------

using TrayPerformanceMonitor.Configuration;

namespace TrayPerformanceMonitor.Tests.Configuration
{
    /// <summary>
    /// Contains unit tests for the <see cref="AppConfiguration"/> class.
    /// </summary>
    public sealed class AppConfigurationTests
    {
        /// <summary>
        /// Verifies that TimerIntervalMs is a positive value.
        /// </summary>
        [Fact]
        public void TimerIntervalMs_IsPositive()
        {
            Assert.True(AppConfiguration.TimerIntervalMs > 0);
        }

        /// <summary>
        /// Verifies that CpuThreshold is within valid percentage range.
        /// </summary>
        [Fact]
        public void CpuThreshold_IsWithinValidRange()
        {
            Assert.InRange(AppConfiguration.CpuThreshold, 0f, 100f);
        }

        /// <summary>
        /// Verifies that RamThreshold is within valid percentage range.
        /// </summary>
        [Fact]
        public void RamThreshold_IsWithinValidRange()
        {
            Assert.InRange(AppConfiguration.RamThreshold, 0f, 100f);
        }

        /// <summary>
        /// Verifies that SpikeTimeThresholdSeconds is a positive value.
        /// </summary>
        [Fact]
        public void SpikeTimeThresholdSeconds_IsPositive()
        {
            Assert.True(AppConfiguration.SpikeTimeThresholdSeconds > 0);
        }

        /// <summary>
        /// Verifies that TopProcessCount is a positive value.
        /// </summary>
        [Fact]
        public void TopProcessCount_IsPositive()
        {
            Assert.True(AppConfiguration.TopProcessCount > 0);
        }

        /// <summary>
        /// Verifies that KeepPinnedIntervalMs is a positive value.
        /// </summary>
        [Fact]
        public void KeepPinnedIntervalMs_IsPositive()
        {
            Assert.True(AppConfiguration.KeepPinnedIntervalMs > 0);
        }

        /// <summary>
        /// Verifies that LogFileName is not null or empty.
        /// </summary>
        [Fact]
        public void LogFileName_IsNotNullOrEmpty()
        {
            Assert.False(string.IsNullOrWhiteSpace(AppConfiguration.LogFileName));
        }

        /// <summary>
        /// Verifies that LogFileName has expected extension.
        /// </summary>
        [Fact]
        public void LogFileName_HasTxtExtension()
        {
            Assert.EndsWith(".txt", AppConfiguration.LogFileName, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Verifies that ApplicationName is not null or empty.
        /// </summary>
        [Fact]
        public void ApplicationName_IsNotNullOrEmpty()
        {
            Assert.False(string.IsNullOrWhiteSpace(AppConfiguration.ApplicationName));
        }

        /// <summary>
        /// Verifies that LogRetentionDays is a positive value.
        /// </summary>
        [Fact]
        public void LogRetentionDays_IsPositive()
        {
            Assert.True(AppConfiguration.LogRetentionDays > 0);
        }

        /// <summary>
        /// Verifies that LogRetentionDays defaults to 7 days.
        /// </summary>
        [Fact]
        public void LogRetentionDays_IsSevenDays()
        {
            Assert.Equal(7, AppConfiguration.LogRetentionDays);
        }
    }
}
