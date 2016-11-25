using System.Drawing;

/// Copyright © 2015-2016 nicoco007
///
/// Licensed under the Apache License, Version 2.0 (the "License");
/// you may not use this file except in compliance with the License.
/// You may obtain a copy of the License at
///
///     http://www.apache.org/licenses/LICENSE-2.0
///
/// Unless required by applicable law or agreed to in writing, software
/// distributed under the License is distributed on an "AS IS" BASIS,
/// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
/// See the License for the specific language governing permissions and
/// limitations under the License.
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