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

namespace LoLAutoLogin
{
    public class ClientWindowMatch : IEquatable<ClientWindowMatch>
    {
        public IntPtr Handle { get; set; }
        public string Name { get; set; }
        public string Class { get; set; }
        public Rectangle WindowRectangle { get; }

        public ClientWindowMatch(IntPtr handle, string name, string windowClass, Rectangle rect)
        {
            Handle = handle;
            Name = name;
            Class = windowClass;
            WindowRectangle = rect;
        }

        public override string ToString()
        {
            return $"{{Handle={Handle}, Name=\"{Name}\", Class=\"{Class}\", Rectangle={WindowRectangle}}}";
        }

        public bool Equals(ClientWindowMatch other)
        {
            return Handle == other.Handle && Name == other.Name && Class == other.Class && WindowRectangle == other.WindowRectangle;
        }
    }
}
