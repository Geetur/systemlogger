// -----------------------------------------------------------------------
// <copyright file="IAiSummaryService.cs" company="TrayPerformanceMonitor">
//     Copyright (c) TrayPerformanceMonitor. All rights reserved.
// </copyright>
// <summary>
//     Defines the contract for AI-powered spike analysis and recommendations.
// </summary>
// -----------------------------------------------------------------------

namespace TrayPerformanceMonitor.Services.Interfaces
{
    /// <summary>
    /// Defines the contract for a service that generates AI-powered summaries
    /// and recommendations for performance spike events.
    /// </summary>
    public interface IAiSummaryService : IDisposable
    {
        /// <summary>
        /// Gets a value indicating whether the AI model is loaded and ready.
        /// </summary>
        bool IsModelLoaded { get; }

        /// <summary>
        /// Generates a summary and recommended next steps for a performance spike.
        /// </summary>
        /// <param name="metricName">The name of the metric that spiked (e.g., "CPU", "RAM").</param>
        /// <param name="value">The current value of the metric as a percentage.</param>
        /// <param name="topProcessesInfo">Formatted string containing top process information.</param>
        /// <returns>
        /// A summary string containing AI analysis and recommendations,
        /// or an empty string if the model is not available.
        /// </returns>
        string GenerateSpikeSummary(string metricName, float value, string topProcessesInfo);

        /// <summary>
        /// Asynchronously generates a summary and recommended next steps for a performance spike.
        /// </summary>
        /// <param name="metricName">The name of the metric that spiked (e.g., "CPU", "RAM").</param>
        /// <param name="value">The current value of the metric as a percentage.</param>
        /// <param name="topProcessesInfo">Formatted string containing top process information.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>
        /// A task containing a summary string with AI analysis and recommendations,
        /// or an empty string if the model is not available.
        /// </returns>
        Task<string> GenerateSpikeSummaryAsync(string metricName, float value, string topProcessesInfo, CancellationToken cancellationToken = default);

        /// <summary>
        /// Attempts to load the AI model from the specified path.
        /// </summary>
        /// <param name="modelPath">The path to the GGUF model file.</param>
        /// <returns>True if the model was loaded successfully; otherwise, false.</returns>
        bool TryLoadModel(string modelPath);
    }
}
