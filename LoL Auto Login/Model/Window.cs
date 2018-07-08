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
using System.Text;

namespace LoLAutoLogin
{
    public class Window
    {
        public IntPtr Handle { get; private set; }
        public string ClassName { get; private set; }
        public string Name { get; private set; }

        public Window(IntPtr handle, string className, string name)
        {
            if (string.IsNullOrEmpty(className))
            {
                StringBuilder builder = new StringBuilder(256);
                NativeMethods.GetClassName(handle, builder, 256);
                className = builder.ToString();
            }

            if (string.IsNullOrEmpty(name))
            {
                StringBuilder builder = new StringBuilder(256);
                NativeMethods.GetWindowText(handle, builder, 256);
                name = builder.ToString();
            }

            Handle = handle;
            ClassName = className;
            Name = name;
        }

        public Rectangle GetRect()
        {
            RECT rect;
            NativeMethods.GetWindowRect(Handle, out rect);
            return rect;
        }

        public Bitmap Capture()
        {
            return Util.CaptureWindow(Handle);
        }

        public void Focus()
        {
            NativeMethods.SetForegroundWindow(Handle);
        }

        public bool Exists()
        {
            return NativeMethods.IsWindow(Handle);
        }

        public bool IsFocused()
        {
            return NativeMethods.GetForegroundWindow() == Handle;
        }

        public override string ToString()
        {
            return $"{{Handle={Handle}, ClassName={ClassName}, Name={Name}}}";
        }
    }
}
