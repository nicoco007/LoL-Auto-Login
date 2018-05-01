using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoLAutoLogin
{
    internal static class WindowUtil
    {
        // debug info
        private static List<ClientWindowMatch> windowsFound = new List<ClientWindowMatch>();

        /// <summary>
        /// Gets a window with specified size using class name and window name.
        /// </summary>
        /// <param name="lpClassName">Class name</param>
        /// <param name="lpWindowName">Window name</param>
        /// <param name="width">Window minimum width</param>
        /// <param name="height">Window minimum height</param>
        /// <returns>The specified window's handle</returns>
        internal static IntPtr GetSingleWindowFromSize(string lpClassName, string lpWindowName, int width, int height)
        {
            Logger.Debug($"Trying to find window handle {{ClassName={(lpClassName ?? "null")},WindowName={(lpWindowName ?? "null")},Size={new Size(width, height)}}}");

            // try to get window handle and rectangle using specified arguments
            var hwnd = NativeMethods.FindWindow(lpClassName, lpWindowName);
            RECT rect;
            NativeMethods.GetWindowRect(hwnd, out rect);

            // check if handle is nothing
            if (hwnd == IntPtr.Zero)
            {
                Logger.Trace("Failed to find window with specified arguments!");

                return IntPtr.Zero;
            }
            
            Logger.Trace($"Found window {{Handle={hwnd},Rectangle={rect}}}");

            if (rect.Size.Width >= width && rect.Size.Height >= height)
            {
                Logger.Trace("Correct window handle found!");

                AddFoundWindow(hwnd, rect, lpClassName, lpWindowName);

                return hwnd;
            }

            while (NativeMethods.FindWindowEx(IntPtr.Zero, hwnd, lpClassName, lpWindowName) != IntPtr.Zero)
            {
                hwnd = NativeMethods.FindWindowEx(IntPtr.Zero, hwnd, lpClassName, lpWindowName);
                NativeMethods.GetWindowRect(hwnd, out rect);

                Logger.Trace($"Found window {{Handle={hwnd},Rectangle={rect}}}");

                if (rect.Size.Width < width || rect.Size.Height < height) continue;

                Logger.Trace("Correct window handle found!");

                AddFoundWindow(hwnd, rect, lpClassName, lpWindowName);

                return hwnd;
            }

            return IntPtr.Zero;
        }

        /// <summary>
        /// Add a window handle to the found windows list, and log it if it is new.
        /// </summary>
        /// <param name="handle">Window handle</param>
        /// <param name="rect">Window rectangle</param>
        /// <param name="className">Window class name (optional)</param>
        /// <param name="name">Window name/text (optional)</param>
        internal static void AddFoundWindow(IntPtr handle, RECT rect, string className = null, string name = null)
        {
            // check if class name is defined
            if (string.IsNullOrEmpty(className))
            {
                // create stringbuilder
                StringBuilder sb = new StringBuilder(255);

                // get class name
                NativeMethods.GetClassName(handle, sb, sb.MaxCapacity);

                // set class name to stringbuilder text
                className = sb.ToString();
            }

            // check if window name/text is set
            if (string.IsNullOrEmpty(name))
            {
                // create stringbuilder
                StringBuilder sb = new StringBuilder(255);

                // get window name/text
                NativeMethods.GetWindowText(handle, sb, sb.MaxCapacity);

                // set window name/text
                name = sb.ToString();
            }

            // create instance of ClientWindowMatch with specified info
            ClientWindowMatch window = new ClientWindowMatch(handle, name, className, rect);

            // check if window is not already in list
            if (!windowsFound.Contains(window))
            {
                Logger.Info("Found new/resized window: " + window);

                // add window to list
                windowsFound.Add(window);
            }
        }

        /// <summary>
        /// Kills every process with the specified name.
        /// </summary>
        /// <param name="pName">Name of process(es) to kill</param>
        internal static void KillProcessesByName(string pName)
        {
            Logger.Trace($"Killing all {pName} processes.");

            foreach (var p in Process.GetProcessesByName(pName)) p.Kill();
        }
    }
}
