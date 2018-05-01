using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Text;

namespace LoLAutoLogin
{
    internal static class WindowUtil
    {
        // debug info
        private static List<ClientWindowMatch> windowsFound = new List<ClientWindowMatch>();

        internal static IntPtr GetSingleWindowFromImage(string className, string windowName, Bitmap image, float tolerance = 0.8f)
        {
            Logger.Debug($"Trying to find window handle for {{ClassName={(className ?? "null")},WindowName={(windowName ?? "null")}}}");

            var hwnd = IntPtr.Zero;
            RECT rect;

            while ((hwnd = NativeMethods.FindWindowEx(IntPtr.Zero, hwnd, className, windowName)) != IntPtr.Zero)
            {
                NativeMethods.GetWindowRect(hwnd, out rect);
                Logger.Trace($"Found window {{Handle={hwnd},Rectangle={rect}}}");

                if (rect.Size.Width > image.Size.Width && rect.Size.Height > image.Size.Height && Util.CompareImage(Util.CaptureWindow(hwnd), image, tolerance) != Rectangle.Empty)
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
