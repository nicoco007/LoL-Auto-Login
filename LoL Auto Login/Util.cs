using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Drawing;
using System.IO;

namespace LoLAutoLogin
{
    internal static class Util
    {
        internal static void OpenFolderAndSelectFile(string filePath)
        {
            if (filePath == null)
                throw new ArgumentNullException("Parameter filePath cannot be null");

            IntPtr pidl = NativeMethods.ILCreateFromPathW(filePath);
            NativeMethods.SHOpenFolderAndSelectItems(pidl, 0, IntPtr.Zero, 0);
            NativeMethods.ILFree(pidl);
        }

        /// <summary>
        /// Creates an Image object containing a screen shot of a specific window
        /// </summary>
        /// <param name="handle">The handle to the window. (In windows forms, this is obtained by the Handle property)</param>
        /// <returns></returns>
        public static Bitmap CaptureWindow(IntPtr handle)
        {
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
        /// <param name="a">First image</param>
        /// <param name="b">Second image</param>
        /// <param name="matchTolerance">Percent tolerance of similar pixel count versus total pixel count</param>
        /// <returns>Whether the images are similar or not</returns>
        public static Rectangle CompareImage(Bitmap source, Bitmap template, double matchTolerance = 0.80)
        {
            var cvSource = new Image<Rgb, byte>(source);
            var cvTemplate = new Image<Rgb, byte>(template);
            var result = Rectangle.Empty;

            using (Image<Gray, float> matches = cvSource.MatchTemplate(cvTemplate, Emgu.CV.CvEnum.TemplateMatchingType.CcoeffNormed))
            {
                double[] minValues, maxValues;
                Point[] minLocations, maxLocations;

                matches.MinMax(out minValues, out maxValues, out minLocations, out maxLocations);

                Logger.Info($"Template matching: needed {matchTolerance}, got {maxValues[0]}");

                if (maxValues[0] > matchTolerance)
                {
                    result = new Rectangle(maxLocations[0], template.Size);
                }
            }

            if (Settings.ClientDetectionDebug)
            {
                try
                {
                    if (!Directory.Exists(Settings.DebugDirectory))
                        Directory.CreateDirectory(Settings.DebugDirectory);

                    var now = DateTime.Now.ToString(@"yyyy-MM-dd\THH-mm-ss");
                    var copy = cvSource.Copy();

                    copy.Draw(result, new Rgb(Color.Red));

                    cvSource.Save(Path.Combine(Settings.DebugDirectory, now + "_source.png"));
                    copy.Save(Path.Combine(Settings.DebugDirectory, now + "_matched.png"));

                    copy.Dispose();
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
    }
}
