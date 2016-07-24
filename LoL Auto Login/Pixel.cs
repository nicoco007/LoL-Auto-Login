using System.Drawing;

/// LoL Auto Login - Automatic Login for League of Legends
/// Copyright © 2015-2016 nicoco007
///
/// This program is free software: you can redistribute it and/or modify
/// it under the terms of the GNU Affero General Public License as published
/// by the Free Software Foundation, either version 3 of the License, or
/// (at your option) any later version.
///
/// This program is distributed in the hope that it will be useful,
/// but WITHOUT ANY WARRANTY; without even the implied warranty of
/// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
/// GNU Affero General Public License for more details.
///
/// You should have received a copy of the GNU Affero General Public License
/// along with this program. If not, see http://www.gnu.org/licenses/.
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