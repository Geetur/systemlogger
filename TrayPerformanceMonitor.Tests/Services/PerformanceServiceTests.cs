// -----------------------------------------------------------------------
// <copyright file="PerformanceServiceTests.cs" company="TrayPerformanceMonitor">
//     Copyright (c) TrayPerformanceMonitor. All rights reserved.
// </copyright>
// <summary>
//     Unit tests for the PerformanceService class.
// </summary>
// -----------------------------------------------------------------------

using TrayPerformanceMonitor.Services;
using TrayPerformanceMonitor.Services.Interfaces;

namespace TrayPerformanceMonitor.Tests.Services
{
    /// <summary>
    /// Contains unit tests for the <see cref="PerformanceService"/> class.
    /// </summary>
    public sealed class PerformanceServiceTests : IDisposable
    {
        private readonly PerformanceService _sut;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="PerformanceServiceTests"/> class.
        /// </summary>
        public PerformanceServiceTests()
        {
            _sut = new PerformanceService();
        }

        /// <summary>
        /// Verifies that GetCpuUsage returns a value within the valid percentage range.
        /// </summary>
        [Fact]
        public void GetCpuUsage_ReturnsValueWithinValidRange()
        {
            // Arrange & Act
            var result = _sut.GetCpuUsage();

            // Assert
            Assert.InRange(result, 0f, 100f);
        }

        /// <summary>
        /// Verifies that GetRamUsage returns a value within the valid percentage range.
        /// </summary>
        [Fact]
        public void GetRamUsage_ReturnsValueWithinValidRange()
        {
            // Arrange & Act
            var result = _sut.GetRamUsage();

            // Assert
            Assert.InRange(result, 0f, 100f);
        }

        /// <summary>
        /// Verifies that GetCurrentMetrics returns a valid PerformanceMetrics struct.
        /// </summary>
        [Fact]
        public void GetCurrentMetrics_ReturnsValidMetrics()
        {
            // Arrange & Act
            var result = _sut.GetCurrentMetrics();

            // Assert
            Assert.InRange(result.CpuUsagePercent, 0f, 100f);
            Assert.InRange(result.RamUsagePercent, 0f, 100f);
        }

        /// <summary>
        /// Verifies that GetRamUsage returns a non-zero value on a running system.
        /// </summary>
        [Fact]
        public void GetRamUsage_OnRunningSystem_ReturnsNonZeroValue()
        {
            // Arrange & Act
            var result = _sut.GetRamUsage();

            // Assert - A running system should always have some RAM in use
            Assert.True(result > 0f, "RAM usage should be greater than 0 on a running system");
        }

        /// <summary>
        /// Verifies that Dispose can be called multiple times without throwing.
        /// </summary>
        [Fact]
        public void Dispose_CalledMultipleTimes_DoesNotThrow()
        {
            // Arrange
            var service = new PerformanceService();

            // Act & Assert
            var exception = Record.Exception(() =>
            {
                service.Dispose();
                service.Dispose();
            });

            Assert.Null(exception);
        }

        /// <summary>
        /// Verifies that GetCpuUsage throws ObjectDisposedException after disposal.
        /// </summary>
        [Fact]
        public void GetCpuUsage_AfterDispose_ThrowsObjectDisposedException()
        {
            // Arrange
            var service = new PerformanceService();
            service.Dispose();

            // Act & Assert
            Assert.Throws<ObjectDisposedException>(() => service.GetCpuUsage());
        }

        /// <summary>
        /// Verifies that GetRamUsage throws ObjectDisposedException after disposal.
        /// </summary>
        [Fact]
        public void GetRamUsage_AfterDispose_ThrowsObjectDisposedException()
        {
            // Arrange
            var service = new PerformanceService();
            service.Dispose();

            // Act & Assert
            Assert.Throws<ObjectDisposedException>(() => service.GetRamUsage());
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (!_disposed)
            {
                _sut?.Dispose();
                _disposed = true;
            }
        }
    }
}
