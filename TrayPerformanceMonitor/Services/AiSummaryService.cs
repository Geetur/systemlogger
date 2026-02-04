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
        private string _currentModelType = "full"; // Tracks current model type for prompt formatting

        /// <summary>
        /// The system prompt that instructs the AI on how to respond.
        /// This is the enhanced prompt designed for detailed, user-friendly output.
        /// </summary>
        private const string SystemPrompt = """
            You are a friendly Windows PC performance expert helping everyday users understand why their computer is running slow. Your job is to analyze performance spikes and explain them in simple, non-technical language that anyone can understand.

            IMPORTANT GUIDELINES:
            1. NEVER use technical jargon (avoid terms like "PID", "thread", "memory leak", "CPU cycles")
            2. Use the actual application NAMES, not process file names (e.g., say "Google Chrome" not "chrome.exe")
            3. Explain what each problematic app actually does and why it might be using resources
            4. Give specific, actionable steps that a non-technical person can follow
            5. Be encouraging and reassuring - don't make the user feel like something is seriously wrong
            6. Use bullet points for easy reading
            7. If a browser is the issue, mention that having many tabs open is often the cause

            YOUR RESPONSE FORMAT:
            📋 What's Happening:
            [1-2 sentences explaining the situation in plain English]

            🔍 The Main Culprits:
            [List the top apps causing the issue with brief explanations]

            ✅ What You Can Do:
            [3-4 simple action steps anyone can follow]

            💡 Quick Tip:
            [One helpful tip to prevent this in the future]
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

                    // Detect model type based on file size
                    var fileInfo = new FileInfo(modelPath);
                    var sizeInMB = fileInfo.Length / (1024.0 * 1024.0);
                    _currentModelType = sizeInMB > 400 ? "full" : "lite";

                    // Configure model parameters for efficient CPU inference
                    // Adjust context size based on model type - lite models work better with smaller context
                    var contextSize = _currentModelType == "lite" 
                        ? Math.Min(AppConfiguration.AiContextSize, 1024u) 
                        : AppConfiguration.AiContextSize;

                    var parameters = new ModelParams(modelPath)
                    {
                        ContextSize = contextSize,
                        GpuLayerCount = 0, // CPU only for laptop compatibility
                        Threads = Math.Max(1, Environment.ProcessorCount / 2), // Use half the cores
                        BatchSize = _currentModelType == "lite" ? 256u : 512u
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
                        
                        // Adjust inference parameters based on model type
                        // Lite models need different anti-prompts for ChatML format
                        var antiPrompts = _currentModelType == "lite"
                            ? new List<string> { "<|im_end|>", "<|im_start|>", "\n\n\n" }
                            : new List<string> { "</s>", "<|user|>", "<|system|>", "\n\n\n" };

                        // Increase max tokens to allow for detailed responses
                        var maxTokens = _currentModelType == "lite" 
                            ? Math.Min(AppConfiguration.AiMaxTokens, 200) 
                            : Math.Max(AppConfiguration.AiMaxTokens, 300);

                        var inferenceParams = new InferenceParams
                        {
                            MaxTokens = maxTokens,
                            AntiPrompts = antiPrompts,
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

                            // Stop if we've generated enough content
                            // Allow more content for better responses
                            if (result.Length > 1500)
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
        /// Builds the prompt for the AI model, using the appropriate format for the model type.
        /// </summary>
        private string BuildPrompt(string metricName, float value, string topProcessesInfo)
        {
            // Parse process info to make it more user-friendly
            var friendlyProcessInfo = FormatProcessInfoForUser(topProcessesInfo);
            
            var userMessage = $"""
                My computer's {metricName} usage just spiked to {value:F0}% and stayed high for over {AppConfiguration.SpikeTimeThresholdSeconds} seconds.

                Here are the applications using the most {metricName}:
                {friendlyProcessInfo}

                Please help me understand what's happening and what I should do about it.
                """;

            // Use different prompt formats based on model type
            // Qwen2 (lite) uses ChatML format, TinyLlama (full) uses Llama format
            if (_currentModelType == "lite")
            {
                // ChatML format for Qwen2
                return $"""
                    <|im_start|>system
                    {SystemPrompt}<|im_end|>
                    <|im_start|>user
                    {userMessage}<|im_end|>
                    <|im_start|>assistant
                    """;
            }
            else
            {
                // Llama/TinyLlama format
                return $"""
                    <|system|>
                    {SystemPrompt}</s>
                    <|user|>
                    {userMessage}</s>
                    <|assistant|>
                    """;
            }
        }

        /// <summary>
        /// Formats process information to be more user-friendly by converting process names to app names.
        /// </summary>
        private static string FormatProcessInfoForUser(string topProcessesInfo)
        {
            if (string.IsNullOrWhiteSpace(topProcessesInfo))
            {
                return "No specific applications identified";
            }

            // Common process name to friendly name mappings
            var friendlyNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "chrome", "Google Chrome (web browser)" },
                { "firefox", "Mozilla Firefox (web browser)" },
                { "msedge", "Microsoft Edge (web browser)" },
                { "brave", "Brave Browser (web browser)" },
                { "opera", "Opera Browser (web browser)" },
                { "iexplore", "Internet Explorer (web browser)" },
                { "code", "Visual Studio Code (code editor)" },
                { "devenv", "Visual Studio (development environment)" },
                { "teams", "Microsoft Teams (chat & meetings)" },
                { "slack", "Slack (team messaging)" },
                { "discord", "Discord (chat & gaming)" },
                { "zoom", "Zoom (video meetings)" },
                { "spotify", "Spotify (music streaming)" },
                { "explorer", "Windows Explorer (file manager)" },
                { "searchhost", "Windows Search (file indexing)" },
                { "searchindexer", "Windows Search Indexer (file indexing)" },
                { "antimalware", "Windows Defender (antivirus scanning)" },
                { "msmpeng", "Windows Defender Antivirus (security scanning)" },
                { "windowsdefender", "Windows Defender (security)" },
                { "onedrive", "OneDrive (cloud storage sync)" },
                { "dropbox", "Dropbox (cloud storage sync)" },
                { "outlook", "Microsoft Outlook (email)" },
                { "word", "Microsoft Word (document editing)" },
                { "excel", "Microsoft Excel (spreadsheets)" },
                { "powerpoint", "Microsoft PowerPoint (presentations)" },
                { "photoshop", "Adobe Photoshop (image editing)" },
                { "premiere", "Adobe Premiere (video editing)" },
                { "aftereffects", "Adobe After Effects (motion graphics)" },
                { "acrobat", "Adobe Acrobat (PDF reader)" },
                { "steam", "Steam (gaming platform)" },
                { "epicgames", "Epic Games Launcher (gaming platform)" },
                { "origin", "EA Origin (gaming platform)" },
                { "battle.net", "Battle.net (gaming platform)" },
                { "nvidia", "NVIDIA Driver/Software (graphics)" },
                { "amd", "AMD Driver/Software (graphics)" },
                { "dwm", "Desktop Window Manager (Windows graphics)" },
                { "svchost", "Windows System Service" },
                { "system", "Windows System Process" },
                { "wuauserv", "Windows Update Service" },
                { "node", "Node.js (JavaScript runtime)" },
                { "python", "Python (programming language)" },
                { "java", "Java Application" },
                { "sqlserver", "SQL Server (database)" },
                { "mysqld", "MySQL (database)" },
                { "postgres", "PostgreSQL (database)" },
                { "docker", "Docker (container platform)" },
                { "vmware", "VMware (virtual machine)" },
                { "virtualbox", "VirtualBox (virtual machine)" },
                { "obs", "OBS Studio (streaming/recording)" },
                { "vlc", "VLC Media Player (video player)" },
                { "itunes", "iTunes (media player)" },
                { "thunderbird", "Mozilla Thunderbird (email)" },
                { "notepad++", "Notepad++ (text editor)" },
                { "winrar", "WinRAR (file compression)" },
                { "7zfm", "7-Zip (file compression)" }
            };

            var lines = topProcessesInfo.Split('\n', '\r');
            var result = new System.Text.StringBuilder();

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var friendlyLine = line;
                foreach (var mapping in friendlyNames)
                {
                    if (line.Contains(mapping.Key, StringComparison.OrdinalIgnoreCase))
                    {
                        // Try to extract the percentage or usage info and append it to the friendly name
                        friendlyLine = line.Replace(mapping.Key, mapping.Value, StringComparison.OrdinalIgnoreCase);
                        break;
                    }
                }
                result.Append(System.Globalization.CultureInfo.InvariantCulture, $"• {friendlyLine.Trim()}");
                result.AppendLine();
            }

            return result.ToString().Trim();
        }

        /// <summary>
        /// Cleans up the AI response by removing artifacts, chat tokens, and trimming.
        /// </summary>
        private static string CleanupResponse(string response)
        {
            if (string.IsNullOrWhiteSpace(response))
            {
                return string.Empty;
            }

            // Remove ChatML tokens (for Qwen2/lite model)
            response = response
                .Replace("<|im_start|>", "")
                .Replace("<|im_end|>", "")
                .Replace("<|im_sep|>", "");

            // Remove Llama tokens (for TinyLlama/full model)
            response = response
                .Replace("<|system|>", "")
                .Replace("<|user|>", "")
                .Replace("<|assistant|>", "")
                .Replace("</s>", "")
                .Replace("<s>", "");

            // Remove common AI artifacts and labels
            response = response
                .Replace("Assistant:", "")
                .Replace("AI:", "")
                .Replace("assistant", "")
                .Replace("system", "")
                .Replace("user", "")
                .Trim();

            // Remove any lines that are just whitespace or very short (likely artifacts)
            var lines = response.Split('\n')
                .Where(line => !string.IsNullOrWhiteSpace(line) && line.Trim().Length > 2)
                .ToList();

            // Keep a reasonable number of lines for the formatted response
            // Allow more lines since we're using a structured format with sections
            if (lines.Count > 20)
            {
                lines = lines.Take(20).ToList();
            }

            response = string.Join("\n", lines).Trim();

            // If response is empty after cleanup, return empty
            if (string.IsNullOrWhiteSpace(response))
            {
                return string.Empty;
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
