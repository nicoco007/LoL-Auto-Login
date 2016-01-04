using System;
using System.Drawing;

namespace LoLAutoLogin
{

    class Pixel
    {

        PointF Position { get; }
        Color Color { get; }
        Color FromColor { get; }
        Color ToColor { get; }
        bool IsRange { get; }
        bool IsRelativePoint { get; }

        public Pixel(Point position, Color color)
        {

            this.Color = color;
            this.Position = position;
            this.IsRange = false;

        }

        public Pixel(Point position, Color fromColor, Color toColor)
        {

            this.Position = position;
            this.FromColor = fromColor;
            this.ToColor = toColor;
            this.IsRange = true;

        }

        public Pixel(PointF position, Color fromColor, Color toColor, bool isRelativePoint)
        {

            if(isRelativePoint && (position.X > 1 || position.X < 0 || position.Y > 1 || position.Y < 0))
            {

                throw new ArgumentException("Position must be between 0.0f and 1.0f if position is relative.");

            }

            this.Position = position;
            this.FromColor = fromColor;
            this.ToColor = toColor;
            this.IsRange = true;
            this.IsRelativePoint = isRelativePoint;

        }

        public bool Match(Bitmap bmp)
        {

            Color pixelColor;

            if (IsRelativePoint)
                pixelColor = bmp.GetPixel((int)(this.Position.X * bmp.Width), (int)(this.Position.Y * bmp.Height));
            else
                pixelColor = bmp.GetPixel((int)this.Position.X, (int)this.Position.Y);

            Log.Verbose(pixelColor.ToString());

            if (IsRange)
                return IsInRange(pixelColor.R, FromColor.R, ToColor.R) && IsInRange(pixelColor.G, FromColor.G, ToColor.G) && IsInRange(pixelColor.B, FromColor.B, ToColor.B);
            else
                return pixelColor == this.Color;

        }

        private bool IsInRange(int value, int min, int max)
        {

            return (value >= min && value <= max);

        }

    }

}