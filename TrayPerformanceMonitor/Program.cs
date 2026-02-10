// -----------------------------------------------------------------------
// <copyright file="Program.cs" company="TrayPerformanceMonitor">
//     Copyright (c) TrayPerformanceMonitor. All rights reserved.
// </copyright>
// <summary>
//     Application entry point for the Tray Performance Monitor.
// </summary>
// -----------------------------------------------------------------------

using System.IO.Pipes;

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
        /// Named pipe used to signal the running instance to show the main hub.
        /// </summary>
        internal const string PipeName = "TrayPerformanceMonitor_ShowHub_8A5E2D3F";

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        /// <remarks>
        /// Initializes the Windows Forms application configuration and starts
        /// the main application context which manages the system tray icon
        /// and performance monitoring functionality.
        /// Only one instance of the application can run at a time.
        /// If a second instance is launched it signals the first to show
        /// its main hub window, then exits silently.
        /// </remarks>
        [STAThread]
        private static void Main()
        {
            // Ensure only one instance of the application runs at a time
            using var mutex = new Mutex(true, MutexName, out bool createdNew);
            
            if (!createdNew)
            {
                // Another instance is already running – signal it to show its hub
                SignalRunningInstance();
                return;
            }

            ApplicationConfiguration.Initialize();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Application.Run(new TrayAppContext());
        }

        /// <summary>
        /// Sends a "show" command to the already-running instance via a named pipe.
        /// </summary>
        private static void SignalRunningInstance()
        {
            try
            {
                using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
                client.Connect(timeout: 2000);

                using var writer = new StreamWriter(client) { AutoFlush = true };
                writer.WriteLine("SHOW");
            }
            catch
            {
                // If the pipe isn't available fall back to a friendly message
                MessageBox.Show(
                    "TrayPerformanceMonitor is already running.\n\nCheck your system tray for the icon.",
                    "Already Running",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }
    }
}
