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
    internal class Pixel
    {
        private PixelCoord X { get; }
        private PixelCoord Y { get; }
        private Color FromColor { get; }
        private Color ToColor { get; }

        public Pixel(PixelCoord x, PixelCoord y, Color color)
        {

            FromColor = color;
            X = x;
            Y = y;

        }

        public Pixel(PixelCoord x, PixelCoord y, Color fromColor, Color toColor)
        {
            
            X = x;
            Y = y;
            FromColor = fromColor;
            ToColor = toColor;

        }

        public bool Match(Bitmap bmp)
        {
            var pixelX = X.Relative ? (int)(X.Coordinate * bmp.Width) : (int)X.Coordinate;
            var pixelY = Y.Relative ? (int)(Y.Coordinate * bmp.Height) : (int)Y.Coordinate;

            var pixelColor = bmp.GetPixel(pixelX, pixelY);

            Log.Verbose(pixelColor.ToString());

            if (FromColor != ToColor)
                return IsInRange(pixelColor.R, FromColor.R, ToColor.R) && IsInRange(pixelColor.G, FromColor.G, ToColor.G) && IsInRange(pixelColor.B, FromColor.B, ToColor.B);

            return pixelColor == this.FromColor;
        }

        private static bool IsInRange(int value, int min, int max) => value >= min && value <= max;
    }

}