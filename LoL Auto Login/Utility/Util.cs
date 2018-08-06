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

using Accord;
using Accord.Imaging;
using Accord.Imaging.Filters;
using Accord.Math.Geometry;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;

namespace LoLAutoLogin
{
    public static class Util
    {
        /// <summary>
        /// Opens the folder containing the specified file in the Windows Explorer and focuses the file
        /// </summary>
        /// <param name="filePath">File to show to the user</param>
        public static void OpenFolderAndSelectFile(string filePath)
        {
            if (filePath == null)
                throw new ArgumentNullException("Parameter filePath cannot be null");

            Logger.Info("Opening " + filePath);

            IntPtr pidl = NativeMethods.ILCreateFromPathW(filePath);
            NativeMethods.SHOpenFolderAndSelectItems(pidl, 0, IntPtr.Zero, 0);
            NativeMethods.ILFree(pidl);

            Process.Start(Path.GetDirectoryName(filePath));
        }

        /// <summary>
        /// Creates an Image object containing a screen shot of a specific window
        /// </summary>
        /// <param name="handle">The handle to the window. (In windows forms, this is obtained by the Handle property)</param>
        /// <returns></returns>
        public static Bitmap CaptureWindow(IntPtr handle)
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
            Bitmap img = System.Drawing.Image.FromHbitmap(hBitmap);

            // free up the Bitmap object
            NativeMethods.DeleteObject(hBitmap);

            return img;
        }

        /// <summary>
        /// Focus all windows with the specified class & window names
        /// </summary>
        /// <param name="className">Class name of the window(s)</param>
        /// <param name="windowName">Name of the window(s)</param>
        public static void FocusWindows(string className, string windowName)
        {
            foreach (var window in GetWindows(className, windowName))
                window.Focus();
        }

        public static List<Window> GetWindows(string className, string windowName)
        {
            Logger.Debug($"Trying to find window handles for {{ClassName={(className ?? "null")},WindowName={(windowName ?? "null")}}}");

            var hwnd = IntPtr.Zero;
            var windows = new List<Window>();

            while ((hwnd = NativeMethods.FindWindowEx(IntPtr.Zero, hwnd, className, windowName)) != IntPtr.Zero)
            {
                Window window = new Window(hwnd, className, windowName);
                windows.Add(window);

                Logger.Trace("Found window " + window);
            }

            Logger.Debug($"Found {windows.Count} windows");

            return windows;
        }

        public static string GetFriendlyOSVersion()
        {
            string productName = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ProductName", "").ToString();
            string csdVersion = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion", "CSDVersion", "").ToString();
            string releaseId = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ReleaseId", "").ToString();

            if (string.IsNullOrEmpty(productName))
                return "Unknown";

            StringBuilder name = new StringBuilder();

            if (!productName.StartsWith("Microsoft"))
                name.Append("Microsoft ");

            name.Append(productName);

            if (!string.IsNullOrEmpty(csdVersion)) {
                name.Append(" ");
                name.Append(csdVersion);
            }

            if (!string.IsNullOrEmpty(releaseId))
            {
                name.Append(" release ");
                name.Append(releaseId);
            }

            name.Append(Environment.Is64BitOperatingSystem ? " x64" : " x86");

            return name.ToString();
        }
        
        public static T ReadYaml<T>(string file) where T : YamlNode
        {
            T read = null;

            using (var reader = new StreamReader(file))
            {
                var deserializer = new Deserializer();
                var parser = new Parser(reader);
                read = deserializer.Deserialize<T>(parser);
            }

            return read;
        }
        
        public static void WriteYaml<T>(string file, T yaml) where T : YamlNode
        {
            using (var writer = new StreamWriter(file))
            {
                var serializer = new SerializerBuilder().EnsureRoundtrip().Build();
                writer.Write(serializer.Serialize(yaml));
            }
        }

        /// <summary>
        /// Some funky stuff to get rectangles out of an image
        /// </summary>
        /// <param name="source">Source image</param>
        /// <returns></returns>
        public static List<Rectangle> FindRectangles(Bitmap source)
        {
            Bitmap canny = Grayscale.CommonAlgorithms.RMY.Apply(source);
            CannyEdgeDetector edgeDetector = new CannyEdgeDetector(Config.GetByteValue("login-detection.low-threshold", 0), Config.GetByteValue("login-detection.high-threshold", 20));
            edgeDetector.ApplyInPlace(canny);

            if (Config.GetBooleanValue("login-detection.debug", false))
                SaveDebugImage(canny, "canny.png");

            BlobCounter blobCounter = new BlobCounter
            {
                FilterBlobs = true,
                MinWidth = 10,
                MinHeight = 10
            };

            blobCounter.ProcessImage(canny);
            Blob[] blobs = blobCounter.GetObjectsInformation();
            List<Rectangle> rectangles = new List<Rectangle>();

            SimpleShapeChecker shapeChecker = new SimpleShapeChecker();

            for (int i = 0; i < blobs.Length; i++)
            {
                List<IntPoint> edgePoints = blobCounter.GetBlobsEdgePoints(blobs[i]);

                if (shapeChecker.IsConvexPolygon(edgePoints, out List<IntPoint> corners))
                    rectangles.Add(blobs[i].Rectangle);
            }
            
            // order by descending area
            rectangles.Sort((a, b) =>
            {
                if (a.Width * a.Height > b.Width * b.Height)
                    return 1;
                else if (a.Width * a.Height < b.Width * b.Height)
                    return -1;
                return 0;
            });

            // save image with all found rectangles
            if (Config.GetBooleanValue("login-detection.debug", false))
            {
                Bitmap output = new Bitmap(source);
                Graphics graphics = Graphics.FromImage(output);
                Pen red = new Pen(Color.Red, 2);

                foreach (var rect in rectangles)
                    graphics.DrawRectangle(red, rect);

                SaveDebugImage(output, "output.png");

                red.Dispose();
                graphics.Dispose();
                output.Dispose();
            }

            return rectangles;
        }

        public static void SaveDebugImage(System.Drawing.Image image, string name)
        {
            var now = DateTime.Now.ToString(@"yyyy-MM-dd\THH-mm-ss.fffffff");

            image.Save(Path.Combine(Folders.Debug, now + "_" + name));
        }
    }
}
