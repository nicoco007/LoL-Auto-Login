using System;
using System.Diagnostics;
using System.Drawing;

namespace LoLAutoLogin
{
    internal static class WindowUtil
    {
        internal static void FocusWindows(string className, string windowName)
        {
            var hwnd = IntPtr.Zero;

            while ((hwnd = NativeMethods.FindWindowEx(IntPtr.Zero, hwnd, className, windowName)) != IntPtr.Zero)
                NativeMethods.SetForegroundWindow(hwnd);
        }

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
