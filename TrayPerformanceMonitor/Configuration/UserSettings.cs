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
    }
}
