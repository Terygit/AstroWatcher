using System;
using System.Runtime.InteropServices;

namespace AstroWatcher
{
    internal static class ProgramHelpers
    {
        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll", CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        private static extern bool ShowWindow([In] IntPtr hWnd, [In] int nCmdShow);

        internal static void HideConsole()
        {
            ShowWindow(GetConsoleWindow(), 6);
        }

        internal static void HideConsoleAfter(int msec)
        {
            System.Timers.Timer timer = new();
            timer.Interval = msec;
            timer.Elapsed += (sender, e) => HideConsole();
            timer.Start();
        }

    }
}