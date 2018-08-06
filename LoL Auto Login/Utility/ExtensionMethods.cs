// Copyright © 2015-2018 Nicolas Gnyra

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

using System;
using System.Drawing;

namespace LoLAutoLogin.Utility
{
    static class ExtensionMethods
    {
        public static bool SimilarTo(this int val1, int val2, int tolerance = 5, int offset = 0)
        {
            return Math.Abs(Math.Abs(val1 - val2) - offset) < tolerance;
        }

        public static bool SimilarTo(this Size s1, Size s2, int tolerance = 5)
        {
            return Math.Abs(s1.Width - s2.Width) < tolerance && Math.Abs(s1.Height - s2.Height) < tolerance;
        }

        public static bool SimilarTo(this Point p1, Point p2, int tolerance = 5)
        {
            return Math.Abs(p1.X - p2.X) < tolerance && Math.Abs(p1.Y - p2.Y) < tolerance;
        }

        public static bool SimilarTo(this Rectangle rect1, Rectangle rect2, int tolerance = 5)
        {
            return rect1.Size.SimilarTo(rect2.Size, tolerance) && rect1.Location.SimilarTo(rect2.Location, tolerance);
        }

        public static bool ApproxContains(this Rectangle container, Rectangle rect, int tolerance = 5)
        {
            Rectangle big = new Rectangle(container.X - tolerance, container.Y - tolerance, container.Width + tolerance, container.Height + tolerance);

            return big.Contains(rect);
        }
    }
}
