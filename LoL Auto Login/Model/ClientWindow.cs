using System;
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
        public ClientStatus Status { get; }
        public Rectangle UsernameBox { get; }
        public Rectangle PasswordBox { get; }
        public Rectangle DialogBox { get; }

        private ClientWindow(IntPtr handle, string className, string windowName, ClientStatus status, Rectangle usernameBox, Rectangle passwordBox, Rectangle dialogBox) : base(handle, className, windowName)
        {
            Status = status;
            UsernameBox = usernameBox;
            PasswordBox = passwordBox;
            DialogBox = dialogBox;
        }

        public static ClientWindow FromWindow(Window window)
        {
            Bitmap capture = window.Capture();

            var status = ClientStatus.Unknown;
            var usernameBox = Rectangle.Empty;
            var passwordBox = Rectangle.Empty;
            var dialogBox = Rectangle.Empty;

            List<Rectangle> rectangles = Util.FindRectangles(capture);

            // distance between username and password boxes
            int ydist = (int)(70f * capture.Width / 1600);

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
                        status = ClientStatus.DialogVisible;
                        dialogBox = rect1;
                        break;
                    }
                }

                // check if rectangle is on sidebar
                if (rect1.X >= 1320 * 1600 / capture.Width)
                {
                    // check if rectangle pair is username and password boxes
                    for (int j = i + 1; j < rectangles.Count; j++)
                    {
                        var rect2 = rectangles[j];

                        if (Util.SimilarSize(rect1.Size, rect2.Size, 2) && Util.SimilarValue(rect1.X, rect2.X, 2) && Util.SimilarValue(rect1.Y, rect2.Y, 15, ydist))
                        {
                            status = ClientStatus.OnLoginScreen;

                            if (rect1.Top < rect2.Top)
                            {
                                usernameBox = rect1;
                                passwordBox = rect2;
                            }
                            else
                            {
                                usernameBox = rect2;
                                passwordBox = rect1;
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
                    graphics.DrawRectangle(new Pen(Color.Red, 1), usernameBox);
                    graphics.DrawRectangle(new Pen(Color.Green, 1), passwordBox);
                    graphics.DrawRectangle(new Pen(Color.Yellow, 1), dialogBox);
                }

                Util.SaveDebugImage(output, "password-box.png");
            }

            var result = new ClientWindow(window.Handle, window.ClassName, window.Name, status, usernameBox, passwordBox, dialogBox);

            capture.Dispose();

            return result;
        }
    }
}
