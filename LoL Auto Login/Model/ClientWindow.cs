﻿using System;
using System.Collections.Generic;
using System.Drawing;

namespace LoLAutoLogin
{
    public enum ClientStatus
    {
        OnLoginScreen,
        DialogVisible,
        Unknown
    }

    public class ClientWindow : Window
    {
        public bool IsMatch { get { return Status != ClientStatus.Unknown; } }

        public ClientStatus Status { get; private set; }
        public Rectangle UsernameBox { get; private set; }
        public Rectangle PasswordBox { get; private set; }
        public Rectangle DialogBox { get; private set; }

        private ClientWindow(IntPtr handle, string className, string windowName) : base(handle, className, windowName) { }

        public void RefreshStatus()
        {
            Status = ClientStatus.Unknown;
            UsernameBox = Rectangle.Empty;
            PasswordBox = Rectangle.Empty;
            DialogBox = Rectangle.Empty;

            Bitmap capture = Capture();

            List<Rectangle> rectangles = Util.FindRectangles(capture);

            // all measurements are based on 1600×900 window
            double scale = capture.Width / 1600f;
            int boxSeparation = (int)Math.Round(70 * scale);    // distance between username and password boxes
            int sidebarLeft = (int)Math.Round(1320 * scale);    // start location of sidebar

            var windowCenter = new Point(capture.Width / 2, capture.Height / 2);
            var windowRectangle = new Rectangle(0, 0, capture.Width, capture.Height);

            // iterate in pairs through all found rectangles and try to find two that look like they're the login boxes
            for (int i = 0; i < rectangles.Count; i++)
            {
                var rect1 = rectangles[i];

                var rectCenter = new Point(rect1.X + rect1.Width / 2, rect1.Y + rect1.Height / 2);

                // check if this rectangle is a dialog
                if (Util.SimilarPoint(windowCenter, rectCenter) && !Util.SimilarRectangle(windowRectangle, rect1, 30))
                {
                    int count = 0;

                    foreach (var rect in rectangles)
                        if (Util.ApproxContains(rect1, rect) && Util.SimilarValue(rect1.Bottom, rect.Bottom))
                            count++;

                    if (count > 0)
                    {
                        Status = ClientStatus.DialogVisible;
                        DialogBox = rect1;
                        break;
                    }
                }

                // check if rectangle is on sidebar
                if (rect1.X >= sidebarLeft)
                {
                    // check if rectangle pair is username and password boxes
                    for (int j = i + 1; j < rectangles.Count; j++)
                    {
                        var rect2 = rectangles[j];

                        if (Util.SimilarSize(rect1.Size, rect2.Size, 2) && Util.SimilarValue(rect1.X, rect2.X, 2) && Util.SimilarValue(rect1.Y, rect2.Y, 15, boxSeparation))
                        {
                            Status = ClientStatus.OnLoginScreen;

                            if (rect1.Top < rect2.Top)
                            {
                                UsernameBox = rect1;
                                PasswordBox = rect2;
                            }
                            else
                            {
                                UsernameBox = rect2;
                                PasswordBox = rect1;
                            }

                            break;
                        }
                    }
                }
            }

            if (Config.GetBooleanValue("login-detection.debug", false))
            {
                Bitmap output = new Bitmap(capture);

                using (var graphics = Graphics.FromImage(output))
                {
                    graphics.DrawRectangle(new Pen(Color.Red, 1), UsernameBox);
                    graphics.DrawRectangle(new Pen(Color.Green, 1), PasswordBox);
                    graphics.DrawRectangle(new Pen(Color.Yellow, 1), DialogBox);
                }

                Util.SaveDebugImage(output, "password-box.png");
            }

            capture.Dispose();
        }

        public static ClientWindow FromWindow(Window window)
        {
            var result = new ClientWindow(window.Handle, window.ClassName, window.Name);

            try
            {
                result.RefreshStatus();
            }
            catch (Exception ex)
            {
                Logger.PrintException(ex);
            }

            return result;
        }
    }
}