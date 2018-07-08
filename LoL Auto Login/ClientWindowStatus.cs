using System.Collections.Generic;
using System.Drawing;

namespace LoLAutoLogin
{
    public enum ClientStatusType
    {
        OnLoginScreen,
        DialogVisible,
        Unknown
    }

    public class ClientStatus
    {
        public ClientStatusType Type { get; }
        public Rectangle UsernameBox { get; }
        public Rectangle PasswordBox { get; }
        public Rectangle DialogBox { get; }

        private ClientStatus(ClientStatusType type, Rectangle usernameBox, Rectangle passwordBox, Rectangle dialogBox)
        {
            Type = type;
            UsernameBox = usernameBox;
            PasswordBox = passwordBox;
            DialogBox = dialogBox;
        }

        public static ClientStatus FromWindow(Window window)
        {
            Bitmap capture = window.Capture();

            var result = FromWindowCapture(capture);

            capture.Dispose();

            return result;
        }

        public static ClientStatus FromWindowCapture(Bitmap image)
        {
            var type = ClientStatusType.Unknown;
            var usernameBox = Rectangle.Empty;
            var passwordBox = Rectangle.Empty;
            var dialogBox = Rectangle.Empty;

            List<Rectangle> rectangles = Util.FindRectangles(image);

            // distance between username and password boxes
            int ydist = (int)(70f * image.Width / 1600);

            var windowCenter = new Point(image.Width / 2, image.Height / 2);
            var windowRectangle = new Rectangle(0, 0, image.Width, image.Height);

            // iterate in pairs through all found rectangles and try to find two that look like they're the login boxes
            for (int i = 0; i < rectangles.Count; i++)
            {
                var rect1 = rectangles[i];

                var rectCenter = new Point(rect1.X + rect1.Width / 2, rect1.Y + rect1.Height / 2);

                if (Util.SimilarPoint(windowCenter, rectCenter) && !Util.SimilarRectangle(windowRectangle, rect1, 30))
                {
                    int count = 0;

                    foreach (var rect in rectangles)
                        if (Util.ApproxContains(rect1, rect) && Util.SimilarValue(rect1.Bottom, rect.Bottom))
                            count++;

                    if (count > 0)
                    {
                        type = ClientStatusType.DialogVisible;
                        dialogBox = rect1;
                        break;
                    }
                }

                for (int j = i + 1; j < rectangles.Count; j++)
                {
                    var rect2 = rectangles[j];

                    if (Util.SimilarSize(rect1.Size, rect2.Size) && Util.SimilarValue(rect1.X, rect2.X) && Util.SimilarValue(rect1.Y, rect2.Y, 15, ydist))
                    {
                        type = ClientStatusType.OnLoginScreen;

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

            if (Config.GetBooleanValue("login-detection.debug", false))
            {
                Bitmap output = new Bitmap(image);

                using (var graphics = Graphics.FromImage(output))
                {
                    graphics.DrawRectangle(new Pen(Color.Red, 1), usernameBox);
                    graphics.DrawRectangle(new Pen(Color.Green, 1), passwordBox);
                    graphics.DrawRectangle(new Pen(Color.Yellow, 1), dialogBox);
                }

                Util.SaveDebugImage(output, "password-box.png");
            }

            return new ClientStatus(type, usernameBox, passwordBox, dialogBox);
        }
    }
}
