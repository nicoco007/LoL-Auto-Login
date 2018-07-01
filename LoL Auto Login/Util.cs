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
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;

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
        /// Focus all windows with the specified class & window names
        /// </summary>
        /// <param name="className">Class name of the window(s)</param>
        /// <param name="windowName">Name of the window(s)</param>
        internal static void FocusWindows(string className, string windowName)
        {
            foreach (var window in GetWindows(className, windowName))
                window.Focus();
        }

        internal static List<Window> GetWindows(string className, string windowName)
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

        internal static string GetFriendlyOSVersion()
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
        
        internal static T ReadYaml<T>(string file) where T : YamlNode
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

        internal static void WriteYaml<T>(string file, T yaml) where T : YamlNode
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
        /// <param name="regionOfInterest">Relative region in image on which to search (adjust according to image size from 0.0 to 1.0)</param>
        /// <returns></returns>
        internal static List<Rectangle> FindRectangles(Bitmap source, RectangleF regionOfInterest)
        {
            var img = new Image<Rgb, byte>(source);

            img.ROI = new Rectangle(
                (int)Math.Round(regionOfInterest.X * img.Width),
                (int)Math.Round(regionOfInterest.Y * img.Height),
                (int)Math.Round(regionOfInterest.Width * img.Width),
                (int)Math.Round(regionOfInterest.Height * img.Height)
            );

            int minArea = (int)(8000f * img.Width / 1600); // box area is 8806 on 1600x900

            var gray = img.Canny(180, 120);

            if (Settings.ClientDetectionDebug)
                SaveDebugImage(gray, "canny.png");

            List<Rectangle> boxList = new List<Rectangle>();

            VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();
            CvInvoke.FindContours(gray, contours, null, RetrType.List, ChainApproxMethod.ChainApproxSimple);
            int count = contours.Size;

            for (int i = 0; i < count; i++)
            {
                var contour = contours[i];
                var approxContour = new VectorOfPoint();

                CvInvoke.ApproxPolyDP(contour, approxContour, CvInvoke.ArcLength(contour, true) * 0.05, true);

                if (CvInvoke.ContourArea(approxContour, false) > minArea)
                {
                    if (approxContour.Size == 4)
                    {
                        bool isRectangle = true;
                        Point[] pts = approxContour.ToArray();
                        LineSegment2D[] edges = PointCollection.PolyLine(pts, true);

                        for (int j = 0; j < edges.Length; j++)
                        {
                            double angle = Math.Abs(edges[(j + 1) % edges.Length].GetExteriorAngleDegree(edges[j]));

                            if (angle < 80 || angle > 100)
                            {
                                isRectangle = false;
                                break;
                            }
                        }

                        if (isRectangle)
                            boxList.Add(CvInvoke.MinAreaRect(approxContour).MinAreaRect());
                    }
                }

                contour.Dispose();
                approxContour.Dispose();
            }

            contours.Dispose();

            // order by descending area
            boxList.Sort((a, b) =>
            {
                return (a.Width * a.Height) < (b.Width * b.Height) ? 1 : -1;
            });

            // save image with all found rectangles
            if (Settings.ClientDetectionDebug)
            {
                foreach (var box in boxList)
                    img.Draw(box, new Rgb(255, 0, 0), 1);

                SaveDebugImage(img, "output.png");
            }

            if (regionOfInterest != Rectangle.Empty)
                boxList = boxList.Select(b => new Rectangle(b.X + source.Width - img.Width, b.Y + source.Height - img.Height, b.Width, b.Height)).ToList();

            return boxList;
        }

        internal static bool SimilarSize(Rectangle rect1, Rectangle rect2, int threshold = 5)
        {
            return Math.Abs(rect1.Width - rect2.Width) < threshold && Math.Abs(rect1.Height - rect2.Height) < threshold;
        }

        internal static bool SimilarX(Rectangle rect1, Rectangle rect2, int threshold = 5, int offset = 0)
        {
            return Math.Abs(Math.Abs(rect1.X - rect2.X) - offset) < threshold;
        }

        internal static bool SimilarY(Rectangle rect1, Rectangle rect2, int threshold = 5, int offset = 0)
        {
            return Math.Abs(Math.Abs(rect1.Y - rect2.Y) - offset) < threshold;
        }

        internal static void SaveDebugImage(Image image, string name)
        {
            var now = DateTime.Now.ToString(@"yyyy-MM-dd\THH-mm-ss.fffffff");

            image.Save(Path.Combine(Folders.Debug.FullName, now + "_" + name));
        }

        internal static void SaveDebugImage<TColor, TDepth>(Image<TColor, TDepth> image, string name) where TColor: struct, IColor where TDepth: new()
        {
            var now = DateTime.Now.ToString(@"yyyy-MM-dd\THH-mm-ss.fffffff");

            image.Save(Path.Combine(Folders.Debug.FullName, now + "_" + name));
        }
    }
}
