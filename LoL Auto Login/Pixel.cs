using System;
using System.Drawing;

namespace LoLAutoLogin
{

    class Pixel
    {

        PixelCoord X { get; }
        PixelCoord Y { get; }
        Color FromColor { get; }
        Color ToColor { get; }

        public Pixel(PixelCoord x, PixelCoord y, Color color)
        {

            this.FromColor = color;
            this.X = x;
            this.Y = y;

        }

        public Pixel(PixelCoord x, PixelCoord y, Color fromColor, Color toColor)
        {
            
            this.X = x;
            this.Y = y;
            this.FromColor = fromColor;
            this.ToColor = toColor;

        }

        public bool Match(Bitmap bmp)
        {

            Color pixelColor;

            int pixelX = this.X.Relative ? (int)(this.X.Coordinate * bmp.Width) : (int)this.X.Coordinate;
            int pixelY = this.Y.Relative ? (int)(this.Y.Coordinate * bmp.Height) : (int)this.Y.Coordinate;

            pixelColor = bmp.GetPixel(pixelX, pixelY);

            Log.Verbose(pixelColor.ToString());

            if (this.FromColor != this.ToColor)
                return IsInRange(pixelColor.R, FromColor.R, ToColor.R) && IsInRange(pixelColor.G, FromColor.G, ToColor.G) && IsInRange(pixelColor.B, FromColor.B, ToColor.B);
            else
                return pixelColor == this.FromColor;

        }

        private bool IsInRange(int value, int min, int max)
        {

            return (value >= min && value <= max);

        }

    }

}