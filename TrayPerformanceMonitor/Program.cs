// -----------------------------------------------------------------------
// <copyright file="Program.cs" company="TrayPerformanceMonitor">
//     Copyright (c) TrayPerformanceMonitor. All rights reserved.
// </copyright>
// <summary>
//     Application entry point for the Tray Performance Monitor.
// </summary>
// -----------------------------------------------------------------------

namespace TrayPerformanceMonitor
{
    /// <summary>
    /// Contains the application entry point.
    /// </summary>
    internal static class Program
    {
        /// <summary>
        /// Unique identifier for the application mutex to ensure single instance.
        /// </summary>
        private const string MutexName = "TrayPerformanceMonitor_SingleInstance_8A5E2D3F";

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        /// <remarks>
        /// Initializes the Windows Forms application configuration and starts
        /// the main application context which manages the system tray icon
        /// and performance monitoring functionality.
        /// Only one instance of the application can run at a time.
        /// </remarks>
        [STAThread]
        private static void Main()
        {
            // Ensure only one instance of the application runs at a time
            using var mutex = new Mutex(true, MutexName, out bool createdNew);
            
            if (!createdNew)
            {
                // Another instance is already running
                MessageBox.Show(
                    "TrayPerformanceMonitor is already running.\n\nCheck your system tray for the icon.",
                    "Already Running",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            ApplicationConfiguration.Initialize();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Application.Run(new TrayAppContext());
        }
    }
}
