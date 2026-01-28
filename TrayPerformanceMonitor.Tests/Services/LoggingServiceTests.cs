// -----------------------------------------------------------------------
// <copyright file="LoggingServiceTests.cs" company="TrayPerformanceMonitor">
//     Copyright (c) TrayPerformanceMonitor. All rights reserved.
// </copyright>
// <summary>
//     Unit tests for the LoggingService class.
// </summary>
// -----------------------------------------------------------------------

using TrayPerformanceMonitor.Configuration;
using TrayPerformanceMonitor.Services;
using TrayPerformanceMonitor.Services.Interfaces;

namespace TrayPerformanceMonitor.Tests.Services
{
    /// <summary>
    /// Contains unit tests for the <see cref="LoggingService"/> class.
    /// </summary>
    public sealed class LoggingServiceTests : IDisposable
    {
        private readonly string _testLogPath;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoggingServiceTests"/> class.
        /// </summary>
        public LoggingServiceTests()
        {
            // Get the actual log path that LoggingService will use
            var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            _testLogPath = !string.IsNullOrEmpty(desktopPath) && Directory.Exists(desktopPath)
                ? Path.Combine(desktopPath, AppConfiguration.LogFileName)
                : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, AppConfiguration.LogFileName);
        }

        /// <summary>
        /// Verifies that LogRetentionDays configuration value is set to 7.
        /// </summary>
        [Fact]
        public void LogRetentionDays_DefaultValue_IsSevenDays()
        {
            // Assert
            Assert.Equal(7, AppConfiguration.LogRetentionDays);
        }

        /// <summary>
        /// Verifies that LogRetentionDays is a positive value.
        /// </summary>
        [Fact]
        public void LogRetentionDays_IsPositive()
        {
            // Assert
            Assert.True(AppConfiguration.LogRetentionDays > 0, "LogRetentionDays must be a positive value");
        }

        /// <summary>
        /// Verifies that Dispose can be called multiple times without throwing.
        /// </summary>
        [Fact]
        public void Dispose_CalledMultipleTimes_DoesNotThrow()
        {
            // Arrange
            using var service = new LoggingService();

            // Act & Assert
            var exception = Record.Exception(() =>
            {
                service.Dispose();
                service.Dispose();
            });

            Assert.Null(exception);
        }

        /// <summary>
        /// Verifies that PruneOldEntries throws ObjectDisposedException after disposal.
        /// </summary>
        [Fact]
        public void PruneOldEntries_AfterDispose_ThrowsObjectDisposedException()
        {
            // Arrange
            var service = new LoggingService();
            service.Dispose();

            // Act & Assert
            Assert.Throws<ObjectDisposedException>(() => service.PruneOldEntries());
        }

        /// <summary>
        /// Verifies that EnsureDailyHeader throws ObjectDisposedException after disposal.
        /// </summary>
        [Fact]
        public void EnsureDailyHeader_AfterDispose_ThrowsObjectDisposedException()
        {
            // Arrange
            var service = new LoggingService();
            service.Dispose();

            // Act & Assert
            Assert.Throws<ObjectDisposedException>(() => service.EnsureDailyHeader());
        }

        /// <summary>
        /// Verifies that LogPerformanceSpike throws ObjectDisposedException after disposal.
        /// </summary>
        [Fact]
        public void LogPerformanceSpike_AfterDispose_ThrowsObjectDisposedException()
        {
            // Arrange
            var service = new LoggingService();
            service.Dispose();

            // Act & Assert
            Assert.Throws<ObjectDisposedException>(() => service.LogPerformanceSpike("CPU", 85.0f, "Test process info"));
        }

        /// <summary>
        /// Verifies that the logging service can be instantiated without throwing.
        /// </summary>
        [Fact]
        public void Constructor_CreatesInstanceSuccessfully()
        {
            // Act & Assert
            var exception = Record.Exception(() =>
            {
                using var service = new LoggingService();
            });

            Assert.Null(exception);
        }

        /// <summary>
        /// Verifies that PruneOldEntries can be called without throwing on a valid service.
        /// </summary>
        [Fact]
        public void PruneOldEntries_OnValidService_DoesNotThrow()
        {
            // Arrange
            using var service = new LoggingService();

            // Act & Assert
            var exception = Record.Exception(() => service.PruneOldEntries());

            Assert.Null(exception);
        }

        /// <summary>
        /// Verifies that EnsureDailyHeader can be called without throwing on a valid service.
        /// </summary>
        [Fact]
        public void EnsureDailyHeader_OnValidService_DoesNotThrow()
        {
            // Arrange
            using var service = new LoggingService();

            // Act & Assert
            var exception = Record.Exception(() => service.EnsureDailyHeader());

            Assert.Null(exception);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
            }
        }
    }
}
