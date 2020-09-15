using Plus.Configuration;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Text;

namespace Plus
{
    /// <summary>
    /// Class Program.
    /// </summary>
    internal class Program
    {
        /// <summary>
        /// The sc close
        /// </summary>
        public const int ScClose = 61536;

        /// <summary>
        /// The username
        /// </summary>
        private static string username = "root", password = string.Empty;

        /// <summary>
        /// Main Void of Plus.Emulator
        /// </summary>
        /// <param name="args">The arguments.</param>
        [SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.ControlAppDomain)]
        public static void Main(string[] args)
        {
            Console.BackgroundColor = ConsoleColor.Black;

            Console.Clear();
            StartConsoleWindow();

            StartEverything();

            while (Plus.IsLive)
            {
                Console.CursorVisible = true;
                if (Logging.DisabledState)
                    Console.Write("KatyaRP> ");

              ConsoleCommandHandling.InvokeCommand(Console.ReadLine());
            }
        }

        /// <summary>
        /// Initialize the Plus Environment
        /// </summary>
        public static void InitEnvironment()
        {
            if (Plus.IsLive)
                return;

            Console.CursorVisible = false;
            var currentDomain = AppDomain.CurrentDomain;
            currentDomain.UnhandledException += MyHandler;
            Plus.Initialize();
        }

        /// <summary>
        /// Deletes the menu.
        /// </summary>
        /// <param name="hMenu">The h menu.</param>
        /// <param name="nPosition">The n position.</param>
        /// <param name="wFlags">The w flags.</param>
        /// <returns>System.Int32.</returns>
        [DllImport("user32.dll")]
        public static extern int DeleteMenu(IntPtr hMenu, int nPosition, int wFlags);

        /// <summary>
        /// Starts the everything.
        /// </summary>
        private static void StartEverything()
        {
            Console.Clear();
            StartConsoleWindow();
            DeleteMenu(GetSystemMenu(GetConsoleWindow(), false), 61536, 0);
            InitEnvironment();
        }

        /// <summary>
        /// Starts the console window.
        /// </summary>
        public static void StartConsoleWindow()
        {
            Console.Clear();
            Console.SetCursorPosition(0, 0);
            Console.ForegroundColor = ConsoleColor.Green;

        }

        /// <summary>
        /// Mies the handler.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The <see cref="UnhandledExceptionEventArgs"/> instance containing the event data.</param>
        private static void MyHandler(object sender, UnhandledExceptionEventArgs args)
        {
            Logging.DisablePrimaryWriting(true);
            var ex = (Exception)args.ExceptionObject;
            Logging.LogCriticalException(string.Format("SYSTEM CRITICAL EXCEPTION: {0}", ex));
        }

        /// <summary>
        /// Gets the system menu.
        /// </summary>
        /// <param name="hWnd">The h WND.</param>
        /// <param name="bRevert">if set to <c>true</c> [b revert].</param>
        /// <returns>IntPtr.</returns>
        [DllImport("user32.dll")]
        private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

        /// <summary>
        /// Gets the console window.
        /// </summary>
        /// <returns>IntPtr.</returns>
        [DllImport("kernel32.dll", ExactSpelling = true)]
        private static extern IntPtr GetConsoleWindow();
    }
}