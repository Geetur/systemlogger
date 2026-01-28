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
        /// The main entry point for the application.
        /// </summary>
        /// <remarks>
        /// Initializes the Windows Forms application configuration and starts
        /// the main application context which manages the system tray icon
        /// and performance monitoring functionality.
        /// </remarks>
        [STAThread]
        private static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Application.Run(new TrayAppContext());
        }
    }
}
