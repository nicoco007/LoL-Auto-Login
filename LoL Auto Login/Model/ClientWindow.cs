// Copyright © 2015-2019 Nicolas Gnyra

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

using LoLAutoLogin.Native;
using LoLAutoLogin.Utility;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace LoLAutoLogin.Model
{
    public enum ClientStatus
    {
        OnLoginScreen,
        DialogVisible,
        Unknown
    }

    public class ClientWindow : Window
    {
        public bool IsMatch => Status != ClientStatus.Unknown;

        public ClientStatus Status { get; private set; }
        public Rectangle UsernameBox { get; private set; }
        public Rectangle PasswordBox { get; private set; }
        public Rectangle DialogBox { get; private set; }
        public Window InnerWindow { get; }

        private ClientWindow(IntPtr handle, Window parent, string className, string windowName, Window innerWindow) : base(handle, parent, className, windowName)
        {
            InnerWindow = innerWindow;
        }

        public static ClientWindow FromWindow(Window window, Window innerWindow)
        {
            var result = new ClientWindow(window.Handle, window.Parent, window.ClassName, window.Name, innerWindow);

            try
            {
                result.RefreshStatus();
            }
            catch (Exception ex)
            {
                Logger.PrintException("Failed to refresh client window status", ex);
            }

            return result;
        }

        public void RefreshStatus()
        {
            Logger.Trace($"Refreshing window {Handle} status");

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

            var windowRectangle = new Rectangle(0, 0, capture.Width, capture.Height);
            var windowCenter = new Point(windowRectangle.Width / 2, windowRectangle.Height / 2);

            // iterate in pairs through all found rectangles and try to find two that look like they're the login boxes
            for (int i = 0; i < rectangles.Count; i++)
            {
                var rect1 = rectangles[i];

                var rectCenter = new Point(rect1.X + rect1.Width / 2, rect1.Y + rect1.Height / 2);

                // check if this rectangle is a dialog
                if (windowCenter.SimilarTo(rectCenter) && !windowRectangle.SimilarTo(rect1, 30))
                {
                    int count = rectangles.Count((otherRect) => rect1.ApproxContains(otherRect) && rect1.Bottom.SimilarTo(otherRect.Bottom));

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

                        if (rect1.Size.SimilarTo(rect2.Size, 2) && rect1.X.SimilarTo(rect2.X, 2) && rect1.Y.SimilarTo(rect2.Y, 15, boxSeparation))
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

                Util.SaveDebugImage(output, "matches.png");

                output.Dispose();
            }

            capture.Dispose();

            Logger.Trace($"Window {Handle} status refreshed");

            GC.Collect();
        }

        public bool HasStatusChanged()
        {
            var previousStatus = Status;
            RefreshStatus();
            return Status != previousStatus;
        }

        public void SignIn(string username, string password)
        {
            if (!string.IsNullOrEmpty(username)) EnterUsername(username);

            EnterPassword(password);

            InnerWindow.Parent.SendKey(VirtualKeyCode.RETURN);
        }

        private void EnterUsername(string username)
        {
            for (int i = 0; i < 3; i++) InnerWindow.SendMouseClick(UsernameBox.Left + UsernameBox.Width / 2, UsernameBox.Top + UsernameBox.Height / 2);

            InnerWindow.Parent.SendKey(VirtualKeyCode.BACK);
            InnerWindow.Parent.SendText(username);
        }

        private void EnterPassword(string password)
        {
            for (int i = 0; i < 3; i++) InnerWindow.SendMouseClick(PasswordBox.Left + PasswordBox.Width / 2, PasswordBox.Top + PasswordBox.Height / 2);

            InnerWindow.Parent.SendKey(VirtualKeyCode.BACK);
            InnerWindow.Parent.SendText(password);
        }
    }
}
