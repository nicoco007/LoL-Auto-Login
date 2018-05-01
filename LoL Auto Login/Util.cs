// Copyright © 2015-2018 Nicolas Gnyra

// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.

// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see http://www.gnu.org/licenses/.

using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Drawing;
using System.IO;

namespace LoLAutoLogin
{
    internal static class Util
    {
        /// <summary>
        /// Opens the folder containing the specified file in the Windows Explorer and focuses the file
        /// </summary>
        /// <param name="filePath">File to show to the user</param>
        internal static void OpenFolderAndSelectFile(string filePath)
        {
            if (filePath == null)
                throw new ArgumentNullException("Parameter filePath cannot be null");

            Logger.Info("Opening " + filePath);

            IntPtr pidl = NativeMethods.ILCreateFromPathW(filePath);
            NativeMethods.SHOpenFolderAndSelectItems(pidl, 0, IntPtr.Zero, 0);
            NativeMethods.ILFree(pidl);
        }

        /// <summary>
        /// Creates an Image object containing a screen shot of a specific window
        /// </summary>
        /// <param name="handle">The handle to the window. (In windows forms, this is obtained by the Handle property)</param>
        /// <returns></returns>
        internal static Bitmap CaptureWindow(IntPtr handle)
        {
            Logger.Debug("Capturing window with handle " + handle);

            // get te hDC of the target window
            var hdcSrc = NativeMethods.GetWindowDC(handle);

            // get the size
            var windowRect = new RECT();
            NativeMethods.GetWindowRect(handle, out windowRect);

            var width = windowRect.Right - windowRect.Left;
            var height = windowRect.Bottom - windowRect.Top;

            // create a device context we can copy to
            var hdcDest = NativeMethods.CreateCompatibleDC(hdcSrc);

            // create a bitmap we can copy it to,
            // using GetDeviceCaps to get the width/height
            var hBitmap = NativeMethods.CreateCompatibleBitmap(hdcSrc, width, height);

            // select the bitmap object
            var hOld = NativeMethods.SelectObject(hdcDest, hBitmap);

            // bitblt over
            NativeMethods.BitBlt(hdcDest, 0, 0, width, height, hdcSrc, 0, 0, NativeMethods.SRCCOPY);

            // restore selection
            NativeMethods.SelectObject(hdcDest, hOld);

            // clean up 
            NativeMethods.DeleteDC(hdcDest);
            NativeMethods.ReleaseDC(handle, hdcSrc);

            // get a .NET image object for it
            Bitmap img = Image.FromHbitmap(hBitmap);

            // free up the Bitmap object
            NativeMethods.DeleteObject(hBitmap);

            return img;
        }

        /// <summary>
        /// Compares two images using their hashes & supplied tolerance
        /// </summary>
        /// <param name="source">Source image</param>
        /// <param name=template">Second image</param>
        /// <param name="tolerance">Percent tolerance for matching the template to a spot in te image</param>
        /// <returns>The rectangle where the template is located in the source based on the tolerance</returns>
        internal static Rectangle CompareImage(Bitmap source, Bitmap template, double tolerance = 0.80)
        {
            var cvSource = new Image<Rgb, byte>(source);
            var cvTemplate = new Image<Rgb, byte>(template);
            var result = Rectangle.Empty;

            using (Image<Gray, float> matches = cvSource.MatchTemplate(cvTemplate, Emgu.CV.CvEnum.TemplateMatchingType.CcoeffNormed))
            {
                double[] minValues, maxValues;
                Point[] minLocations, maxLocations;

                matches.MinMax(out minValues, out maxValues, out minLocations, out maxLocations);

                Logger.Info($"Template matching: wanted {tolerance}, got {maxValues[0]}");

                if (maxValues[0] > tolerance)
                    result = new Rectangle(maxLocations[0], template.Size);
            }

            if (Settings.ClientDetectionDebug)
            {
                try
                {
                    if (!Directory.Exists(Settings.DebugDirectory))
                        Directory.CreateDirectory(Settings.DebugDirectory);

                    var now = DateTime.Now.ToString(@"yyyy-MM-dd.fffffff\THH-mm-ss");
                    
                    cvSource.Save(Path.Combine(Settings.DebugDirectory, now + "_source.png"));

                    if (result != Rectangle.Empty)
                    {
                        var copy = cvSource.Copy();

                        copy.Draw(result, new Rgb(Color.Red));
                        copy.Save(Path.Combine(Settings.DebugDirectory, now + "_matched.png"));
                        copy.Dispose();
                    }

                }
                catch (IOException ex)
                {
                    Logger.Error("Failed to save debug images to " + Settings.DebugDirectory);
                    Logger.PrintException(ex);
                }
            }

            cvSource.Dispose();
            cvTemplate.Dispose();

            return result;
        }

        /// <summary>
        /// Focus all windows with the specified class & window names
        /// </summary>
        /// <param name="className">Class name of the window(s)</param>
        /// <param name="windowName">Name of the window(s)</param>
        internal static void FocusWindows(string className, string windowName)
        {
            var hwnd = IntPtr.Zero;

            while ((hwnd = NativeMethods.FindWindowEx(IntPtr.Zero, hwnd, className, windowName)) != IntPtr.Zero)
                NativeMethods.SetForegroundWindow(hwnd);
        }

        /// <summary>
        /// Fetches a window handle with the specified class and window names that contains the specified image
        /// </summary>
        /// <param name="className">Class name of the window(s)</param>
        /// <param name="windowName">Name of the window(s)</param>
        /// <param name="image">Image that the window must contain</param>
        /// <param name="tolerance">Matching tolerance</param>
        /// <returns></returns>
        internal static IntPtr GetSingleWindowFromImage(string className, string windowName, Bitmap image, float tolerance = 0.8f)
        {
            Logger.Debug($"Trying to find window handle for {{ClassName={(className ?? "null")},WindowName={(windowName ?? "null")}}}");

            var hwnd = IntPtr.Zero;
            RECT rect;

            while ((hwnd = NativeMethods.FindWindowEx(IntPtr.Zero, hwnd, className, windowName)) != IntPtr.Zero)
            {
                NativeMethods.GetWindowRect(hwnd, out rect);
                Logger.Trace($"Found window {{Handle={hwnd},Rectangle={rect}}}");

                if (rect.Size.Width > image.Size.Width && rect.Size.Height > image.Size.Height && CompareImage(CaptureWindow(hwnd), image, tolerance) != Rectangle.Empty)
                    return hwnd;
            }

            return IntPtr.Zero;
        }
    }
}
