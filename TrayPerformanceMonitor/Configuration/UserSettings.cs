// -----------------------------------------------------------------------
// <copyright file="UserSettings.cs" company="TrayPerformanceMonitor">
//     Copyright (c) TrayPerformanceMonitor. All rights reserved.
// </copyright>
// <summary>
//     Provides user-configurable settings loaded from a JSON file.
// </summary>
// -----------------------------------------------------------------------

using System.Text.Json;

namespace TrayPerformanceMonitor.Configuration
{
    /// <summary>
    /// Represents user-configurable settings that can be modified via the installer
    /// or by editing the settings.json file directly.
    /// </summary>
    public sealed class UserSettings
    {
        private static readonly string SettingsFilePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "settings.json");

        private static UserSettings? _instance;
        private static readonly object _lock = new();

        /// <summary>
        /// Gets the singleton instance of UserSettings.
        /// </summary>
        public static UserSettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= Load();
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// Gets or sets the number of top processes to display when logging a spike event.
        /// Valid range: 1-10. Default: 3.
        /// </summary>
        public int TopProcessCount { get; set; } = 3;

        /// <summary>
        /// Gets the validated TopProcessCount, clamped to valid range.
        /// </summary>
        public int ValidatedTopProcessCount => Math.Clamp(TopProcessCount, 1, 10);

        /// <summary>
        /// Gets or sets the model type ("full" or "lite"). Default: "full".
        /// </summary>
        public string ModelType { get; set; } = "full";

        /// <summary>
        /// Gets the validated model type (always "full" or "lite").
        /// </summary>
        public string ValidatedModelType => ModelType?.ToLowerInvariant() == "lite" ? "lite" : "full";

        /// <summary>
        /// Event raised when settings are changed and saved.
        /// </summary>
        public static event EventHandler? SettingsChanged;

        /// <summary>
        /// Loads settings from the JSON file, or returns default settings if file doesn't exist.
        /// </summary>
        /// <returns>The loaded or default UserSettings instance.</returns>
        private static UserSettings Load()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    var json = File.ReadAllText(SettingsFilePath);
                    var settings = JsonSerializer.Deserialize<UserSettings>(json);
                    if (settings != null)
                    {
                        return settings;
                    }
                }
            }
            catch
            {
                // If loading fails, use defaults
            }

            return new UserSettings();
        }

        /// <summary>
        /// Saves the current settings to the JSON file.
        /// </summary>
        public void Save()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(this, options);
                File.WriteAllText(SettingsFilePath, json);
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
            catch
            {
                // Silently fail if we can't save settings
            }
        }

        /// <summary>
        /// Reloads settings from disk.
        /// </summary>
        public static void Reload()
        {
            lock (_lock)
            {
                _instance = Load();
            }
        }

        /// <summary>
        /// Gets the path to the Models directory where AI models should be stored.
        /// </summary>
        public static string GetModelsDirectory()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Models");
        }

        /// <summary>
        /// Gets the full path to a specific model file by type.
        /// </summary>
        /// <param name="modelType">The model type ("full" or "lite").</param>
        /// <returns>The path to the model-specific file.</returns>
        public static string GetModelFilePath(string modelType)
        {
            var filename = modelType?.ToLowerInvariant() == "lite" ? "model-lite.gguf" : "model-full.gguf";
            return Path.Combine(GetModelsDirectory(), filename);
        }

        /// <summary>
        /// Gets the full path to the currently selected model file based on user settings.
        /// </summary>
        public static string GetActiveModelFilePath()
        {
            return GetModelFilePath(Instance.ValidatedModelType);
        }

        /// <summary>
        /// Gets the legacy model file path (for backwards compatibility).
        /// </summary>
        public static string GetLegacyModelFilePath()
        {
            return Path.Combine(GetModelsDirectory(), AppConfiguration.AiModelFileName);
        }

        /// <summary>
        /// Gets the path to the download script.
        /// </summary>
        public static string GetDownloadScriptPath()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Scripts", "DownloadModel.ps1");
        }

        /// <summary>
        /// Checks if the model download script exists.
        /// </summary>
        public static bool IsDownloadScriptAvailable()
        {
            return File.Exists(GetDownloadScriptPath());
        }

        /// <summary>
        /// Checks if the specified model type is installed.
        /// </summary>
        /// <param name="modelType">The model type to check ("full" or "lite").</param>
        /// <returns>True if the model is installed.</returns>
        public static bool IsModelInstalled(string modelType)
        {
            return File.Exists(GetModelFilePath(modelType));
        }

        /// <summary>
        /// Checks if the currently selected model is installed.
        /// </summary>
        public static bool IsActiveModelInstalled()
        {
            return IsModelInstalled(Instance.ValidatedModelType);
        }

        /// <summary>
        /// Gets a summary of which models are installed.
        /// </summary>
        /// <returns>A tuple indicating (fullInstalled, liteInstalled).</returns>
        public static (bool fullInstalled, bool liteInstalled) GetInstalledModels()
        {
            return (IsModelInstalled("full"), IsModelInstalled("lite"));
        }

        /// <summary>
        /// Migrates a legacy model.gguf file to the new naming scheme if needed.
        /// </summary>
        public static void MigrateLegacyModel()
        {
            var legacyPath = GetLegacyModelFilePath();
            if (!File.Exists(legacyPath))
            {
                return;
            }

            // Check if we already have model-specific files
            var (fullInstalled, liteInstalled) = GetInstalledModels();
            if (fullInstalled || liteInstalled)
            {
                // Already migrated, can delete legacy file if desired
                return;
            }

            try
            {
                var fileInfo = new FileInfo(legacyPath);
                var sizeInMB = fileInfo.Length / (1024.0 * 1024.0);

                // Determine model type based on size and move to appropriate location
                var modelType = sizeInMB > 400 ? "full" : "lite";
                var newPath = GetModelFilePath(modelType);

                File.Move(legacyPath, newPath);
            }
            catch
            {
                // Migration failed - user can re-download
            }
        }
    }
}
