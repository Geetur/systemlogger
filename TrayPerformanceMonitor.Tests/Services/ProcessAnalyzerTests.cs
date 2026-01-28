// -----------------------------------------------------------------------
// <copyright file="ProcessAnalyzerTests.cs" company="TrayPerformanceMonitor">
//     Copyright (c) TrayPerformanceMonitor. All rights reserved.
// </copyright>
// <summary>
//     Unit tests for the ProcessAnalyzer class.
// </summary>
// -----------------------------------------------------------------------

using TrayPerformanceMonitor.Services;

namespace TrayPerformanceMonitor.Tests.Services
{
    /// <summary>
    /// Contains unit tests for the <see cref="ProcessAnalyzer"/> class.
    /// </summary>
    public sealed class ProcessAnalyzerTests
    {
        private readonly ProcessAnalyzer _sut;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessAnalyzerTests"/> class.
        /// </summary>
        public ProcessAnalyzerTests()
        {
            _sut = new ProcessAnalyzer();
        }

        /// <summary>
        /// Verifies that GetTopCpuProcesses returns a non-empty string.
        /// </summary>
        [Fact]
        public void GetTopCpuProcesses_WithValidCount_ReturnsNonEmptyString()
        {
            // Arrange & Act
            var result = _sut.GetTopCpuProcesses(3);

            // Assert
            Assert.False(string.IsNullOrWhiteSpace(result));
        }

        /// <summary>
        /// Verifies that GetTopRamProcesses returns a non-empty string.
        /// </summary>
        [Fact]
        public void GetTopRamProcesses_WithValidCount_ReturnsNonEmptyString()
        {
            // Arrange & Act
            var result = _sut.GetTopRamProcesses(3);

            // Assert
            Assert.False(string.IsNullOrWhiteSpace(result));
        }

        /// <summary>
        /// Verifies that GetTopCpuProcesses returns the correct number of processes.
        /// </summary>
        [Theory]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(5)]
        public void GetTopCpuProcesses_ReturnsRequestedNumberOfProcesses(int count)
        {
            // Arrange & Act
            var result = _sut.GetTopCpuProcesses(count);
            var lines = result.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

            // Assert
            Assert.True(lines.Length <= count, $"Expected at most {count} processes, got {lines.Length}");
        }

        /// <summary>
        /// Verifies that GetTopRamProcesses returns the correct number of processes.
        /// </summary>
        [Theory]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(5)]
        public void GetTopRamProcesses_ReturnsRequestedNumberOfProcesses(int count)
        {
            // Arrange & Act
            var result = _sut.GetTopRamProcesses(count);
            var lines = result.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

            // Assert
            Assert.True(lines.Length <= count, $"Expected at most {count} processes, got {lines.Length}");
        }

        /// <summary>
        /// Verifies that GetTopCpuProcesses throws for invalid count.
        /// </summary>
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void GetTopCpuProcesses_WithInvalidCount_ThrowsArgumentOutOfRangeException(int count)
        {
            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => _sut.GetTopCpuProcesses(count));
        }

        /// <summary>
        /// Verifies that GetTopRamProcesses throws for invalid count.
        /// </summary>
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void GetTopRamProcesses_WithInvalidCount_ThrowsArgumentOutOfRangeException(int count)
        {
            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => _sut.GetTopRamProcesses(count));
        }

        /// <summary>
        /// Verifies that GetTopRamProcesses output contains expected format markers.
        /// </summary>
        [Fact]
        public void GetTopRamProcesses_OutputContainsExpectedFormat()
        {
            // Arrange & Act
            var result = _sut.GetTopRamProcesses(3);

            // Assert - Each line should have the format "  - ProcessName (PID X): Y MB"
            Assert.Contains("(PID", result);
            Assert.Contains("MB", result);
        }

        /// <summary>
        /// Verifies that calling GetTopCpuProcesses multiple times returns valid data each time.
        /// </summary>
        [Fact]
        public void GetTopCpuProcesses_MultipleCalls_ReturnsValidDataEachTime()
        {
            // Act
            var result1 = _sut.GetTopCpuProcesses(3);
            var result2 = _sut.GetTopCpuProcesses(3);
            var result3 = _sut.GetTopCpuProcesses(3);

            // Assert - All results should be non-empty
            Assert.False(string.IsNullOrWhiteSpace(result1));
            Assert.False(string.IsNullOrWhiteSpace(result2));
            Assert.False(string.IsNullOrWhiteSpace(result3));
        }
    }
}
