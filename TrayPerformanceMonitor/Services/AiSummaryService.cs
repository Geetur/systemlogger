// -----------------------------------------------------------------------
// <copyright file="AiSummaryService.cs" company="TrayPerformanceMonitor">
//     Copyright (c) TrayPerformanceMonitor. All rights reserved.
// </copyright>
// <summary>
//     Provides AI-powered analysis and recommendations for performance spikes
//     using a local LLaMA model.
// </summary>
// -----------------------------------------------------------------------

using LLama;
using LLama.Common;
using LLama.Sampling;
using TrayPerformanceMonitor.Configuration;
using TrayPerformanceMonitor.Services.Interfaces;

namespace TrayPerformanceMonitor.Services
{
    /// <summary>
    /// Provides AI-powered summaries and recommendations for performance spike events
    /// using a locally-running LLaMA model via LLamaSharp.
    /// </summary>
    /// <remarks>
    /// This service uses a small LLaMA-compatible model (such as Phi-3-mini, TinyLlama, or Llama 3.2 1B)
    /// that can run efficiently on a laptop CPU. The model generates brief, actionable
    /// summaries explaining what caused the spike and what steps the user should take.
    /// If the model is not available, the service gracefully degrades and returns empty summaries.
    /// </remarks>
    public sealed class AiSummaryService : IAiSummaryService
    {
        private readonly object _modelLock = new();
        private LLamaWeights? _model;
        private LLamaContext? _context;
        private bool _disposed;

        /// <summary>
        /// The system prompt that instructs the AI on how to respond.
        /// </summary>
        private const string SystemPrompt = """
            You are a helpful system performance assistant. When given information about a performance spike (high CPU or RAM usage), 
            provide a brief 2-3 sentence summary explaining the likely cause and 2-3 actionable next steps. 
            Be concise and practical. Focus on the top processes shown.
            """;

        /// <inheritdoc/>
        public bool IsModelLoaded => _model != null && _context != null;

        /// <inheritdoc/>
        public bool TryLoadModel(string modelPath)
        {
            if (string.IsNullOrWhiteSpace(modelPath))
            {
                return false;
            }

            if (!File.Exists(modelPath))
            {
                return false;
            }

            try
            {
                lock (_modelLock)
                {
                    // Dispose existing model if any
                    _context?.Dispose();
                    _model?.Dispose();

                    // Configure model parameters for efficient CPU inference
                    var parameters = new ModelParams(modelPath)
                    {
                        ContextSize = AppConfiguration.AiContextSize,
                        GpuLayerCount = 0, // CPU only for laptop compatibility
                        Threads = Math.Max(1, Environment.ProcessorCount / 2), // Use half the cores
                        BatchSize = 512
                    };

                    _model = LLamaWeights.LoadFromFile(parameters);
                    _context = _model.CreateContext(parameters);

                    return true;
                }
            }
            catch (Exception)
            {
                // Model loading failed - service will operate in degraded mode
                _context?.Dispose();
                _model?.Dispose();
                _context = null;
                _model = null;
                return false;
            }
        }

        /// <inheritdoc/>
        public string GenerateSpikeSummary(string metricName, float value, string topProcessesInfo)
        {
            if (!IsModelLoaded)
            {
                return string.Empty;
            }

            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(AppConfiguration.AiTimeoutSeconds));
                return GenerateSpikeSummaryAsync(metricName, value, topProcessesInfo, cts.Token)
                    .GetAwaiter()
                    .GetResult();
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        /// <inheritdoc/>
        public async Task<string> GenerateSpikeSummaryAsync(
            string metricName,
            float value,
            string topProcessesInfo,
            CancellationToken cancellationToken = default)
        {
            if (!IsModelLoaded || _context == null)
            {
                return string.Empty;
            }

            try
            {
                var prompt = BuildPrompt(metricName, value, topProcessesInfo);

                return await Task.Run(() =>
                {
                    lock (_modelLock)
                    {
                        if (_context == null)
                        {
                            return string.Empty;
                        }

                        var executor = new StatelessExecutor(_model!, _context.Params);
                        
                        var inferenceParams = new InferenceParams
                        {
                            MaxTokens = AppConfiguration.AiMaxTokens,
                            AntiPrompts = new List<string> { "\n\n", "User:", "Human:", "###" },
                            SamplingPipeline = new DefaultSamplingPipeline
                            {
                                Temperature = 0.7f
                            }
                        };

                        var result = new System.Text.StringBuilder();

                        foreach (var text in executor.InferAsync(prompt, inferenceParams, cancellationToken).ToBlockingEnumerable(cancellationToken))
                        {
                            if (cancellationToken.IsCancellationRequested)
                            {
                                break;
                            }

                            result.Append(text);

                            // Stop if we've generated enough
                            if (result.Length > 500)
                            {
                                break;
                            }
                        }

                        var summary = result.ToString().Trim();

                        // Clean up the response
                        summary = CleanupResponse(summary);

                        return string.IsNullOrWhiteSpace(summary) ? string.Empty : summary;
                    }
                }, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return string.Empty;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Builds the prompt for the AI model.
        /// </summary>
        private static string BuildPrompt(string metricName, float value, string topProcessesInfo)
        {
            return $"""
                {SystemPrompt}

                Performance Alert:
                - {metricName} usage: {value:F1}%
                - Duration: Sustained for {AppConfiguration.SpikeTimeThresholdSeconds}+ seconds
                
                Top {metricName}-consuming processes:
                {topProcessesInfo}

                Provide a brief summary and recommended next steps:
                """;
        }

        /// <summary>
        /// Cleans up the AI response by removing artifacts and trimming.
        /// </summary>
        private static string CleanupResponse(string response)
        {
            if (string.IsNullOrWhiteSpace(response))
            {
                return string.Empty;
            }

            // Remove common AI artifacts
            response = response
                .Replace("Assistant:", "")
                .Replace("AI:", "")
                .Trim();

            // Take only the first meaningful paragraph if response is too long
            var lines = response.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length > 6)
            {
                response = string.Join("\n", lines.Take(6));
            }

            return response;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            lock (_modelLock)
            {
                _context?.Dispose();
                _model?.Dispose();
                _context = null;
                _model = null;
            }

            _disposed = true;
        }
    }
}
