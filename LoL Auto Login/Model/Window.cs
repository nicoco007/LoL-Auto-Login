// Copyright © 2015-2019 Nicolas Gnyra

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

using LoLAutoLogin.Native;
using LoLAutoLogin.Utility;
using System;
using System.Drawing;
using System.Text;

namespace LoLAutoLogin.Model
{
    public class Window
    {
        public IntPtr Handle { get; }
        public Window Parent { get; }
        public string ClassName { get; }
        public string Name { get; }

        public Window(IntPtr handle, IntPtr parentHandle, string className = null, string name = null)
        {
            if (handle == IntPtr.Zero)
                throw new ArgumentOutOfRangeException(nameof(handle) + " cannot be zero");

            if (parentHandle != IntPtr.Zero)
            {
                Parent = new Window(parentHandle, NativeMethods.GetParent(parentHandle));
            }
            else
            {
                Parent = null;
            }

            Handle = handle;
            ClassName = !string.IsNullOrEmpty(className) ? className : GetWindowClassName(handle);
            Name = !string.IsNullOrEmpty(name) ? name : GetWindowName(handle);
        }

        public Window(IntPtr handle, Window parent, string className = null, string name = null)
        {
            if (handle == IntPtr.Zero)
                throw new ArgumentOutOfRangeException(nameof(handle) + " cannot be zero");

            Handle = handle;
            Parent = parent;
            ClassName = !string.IsNullOrEmpty(className) ? className : GetWindowClassName(handle);
            Name = !string.IsNullOrEmpty(name) ? name : GetWindowName(handle);
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

        public void SendMouseClick(int x, int y)
        {
            int lParam = (y << 16) + x;

            NativeMethods.SendMessage(Handle, (uint)WindowMessage.LBUTTONDOWN, 0, lParam);
            NativeMethods.SendMessage(Handle, (uint)WindowMessage.LBUTTONUP, 0, lParam);
        }

        public void SendKey(VirtualKeyCode keyCode)
        {
            NativeMethods.SendMessage(Handle, (uint)WindowMessage.KEYDOWN, (int)keyCode, 0);
            NativeMethods.SendMessage(Handle, (uint)WindowMessage.CHAR, 0x01, 0);
            NativeMethods.SendMessage(Handle, (uint)WindowMessage.KEYUP, (int)keyCode, 0);
        }

        public void SendText(string text)
        {
            foreach (char c in text)
            {
                NativeMethods.SendMessage(Handle, (uint)WindowMessage.CHAR, c, 0);
            }
        }

        private int GetDigitAtPosition(int input, int position)
        {
            return (int)Math.Floor((input - Math.Floor(input / Math.Pow(10, position + 1)) * Math.Pow(10, position + 1)) / Math.Pow(10, position));
        }

        public Window FindChildRecursively(string className, string windowName)
        {
            return FindChildRecursively(Handle, className, windowName);
        }

        private Window FindChildRecursively(IntPtr parent, string className, string windowName)
        {
            IntPtr child = NativeMethods.FindWindowEx(parent, IntPtr.Zero, null, null);

            Console.WriteLine(className + " " + windowName);

            Console.WriteLine("FindChildRecursively -> parent: " + parent.ToString("X"));

            while (child != IntPtr.Zero)
            {
                Console.WriteLine("FindChildRecursively -> child: " + child.ToString("X"));
                Window found;

                if ((found = FindChildRecursively(child, className, windowName)) != null)
                {
                    return found;
                }

                if ((string.IsNullOrEmpty(className) || GetWindowClassName(child) == className) && (string.IsNullOrEmpty(windowName) || GetWindowName(child) == windowName))
                {
                    return new Window(child, parent, className, windowName);
                }

                child = NativeMethods.FindWindowEx(parent, child, null, null);
            }

            return null;
        }

        private string GetWindowClassName(IntPtr handle)
        {
            if (handle == IntPtr.Zero)
                throw new ArgumentOutOfRangeException(nameof(handle) + " cannot be zero");

            StringBuilder sb = new StringBuilder(256);
            NativeMethods.GetClassName(handle, sb, sb.Capacity);
            return sb.ToString();
        }

        private string GetWindowName(IntPtr handle)
        {
            if (handle == IntPtr.Zero)
                throw new ArgumentOutOfRangeException(nameof(handle) + " cannot be zero");

            StringBuilder sb = new StringBuilder(256);
            NativeMethods.GetWindowText(handle, sb, sb.Capacity);
            return sb.ToString();
        }
    }
}
