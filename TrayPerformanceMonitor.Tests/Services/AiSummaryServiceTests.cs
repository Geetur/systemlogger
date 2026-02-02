// -----------------------------------------------------------------------
// <copyright file="AiSummaryServiceTests.cs" company="TrayPerformanceMonitor">
//     Copyright (c) TrayPerformanceMonitor. All rights reserved.
// </copyright>
// <summary>
//     Unit tests for the AiSummaryService class.
// </summary>
// -----------------------------------------------------------------------

using TrayPerformanceMonitor.Services;

namespace TrayPerformanceMonitor.Tests.Services
{
    /// <summary>
    /// Contains unit tests for the <see cref="AiSummaryService"/> class.
    /// </summary>
    public sealed class AiSummaryServiceTests : IDisposable
    {
        private readonly AiSummaryService _sut;

        /// <summary>
        /// Initializes a new instance of the <see cref="AiSummaryServiceTests"/> class.
        /// </summary>
        public AiSummaryServiceTests()
        {
            _sut = new AiSummaryService();
        }

        /// <summary>
        /// Verifies that IsModelLoaded returns false when no model is loaded.
        /// </summary>
        [Fact]
        public void IsModelLoaded_WhenNoModelLoaded_ReturnsFalse()
        {
            // Assert
            Assert.False(_sut.IsModelLoaded);
        }

        /// <summary>
        /// Verifies that TryLoadModel returns false for non-existent path.
        /// </summary>
        [Fact]
        public void TryLoadModel_WithNonExistentPath_ReturnsFalse()
        {
            // Arrange
            var fakePath = @"C:\nonexistent\fake_model.gguf";

            // Act
            var result = _sut.TryLoadModel(fakePath);

            // Assert
            Assert.False(result);
            Assert.False(_sut.IsModelLoaded);
        }

        /// <summary>
        /// Verifies that TryLoadModel returns false for null or empty path.
        /// </summary>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void TryLoadModel_WithNullOrEmptyPath_ReturnsFalse(string? path)
        {
            // Act
            var result = _sut.TryLoadModel(path!);

            // Assert
            Assert.False(result);
        }

        /// <summary>
        /// Verifies that GenerateSpikeSummary returns empty string when model not loaded.
        /// </summary>
        [Fact]
        public void GenerateSpikeSummary_WhenModelNotLoaded_ReturnsEmptyString()
        {
            // Arrange - model is not loaded

            // Act
            var result = _sut.GenerateSpikeSummary("CPU", 95.0f, "  - test (PID 123): 50%");

            // Assert
            Assert.Equal(string.Empty, result);
        }

        /// <summary>
        /// Verifies that Dispose can be called multiple times without throwing.
        /// </summary>
        [Fact]
        public void Dispose_CalledMultipleTimes_DoesNotThrow()
        {
            // Act & Assert
            var exception = Record.Exception(() =>
            {
                _sut.Dispose();
                _sut.Dispose();
            });

            Assert.Null(exception);
        }

        /// <summary>
        /// Verifies that the service can be instantiated without throwing.
        /// </summary>
        [Fact]
        public void Constructor_CreatesInstanceSuccessfully()
        {
            // Act & Assert
            using var service = new AiSummaryService();
            Assert.NotNull(service);
            Assert.False(service.IsModelLoaded);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _sut.Dispose();
        }
    }
}
