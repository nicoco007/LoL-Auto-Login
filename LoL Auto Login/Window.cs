using System;
using System.Drawing;
using System.Text;

namespace LoLAutoLogin
{
    internal class Window
    {
        internal IntPtr Handle { get; private set; }
        internal string ClassName { get; private set; }
        internal string Name { get; private set; }

        internal Window(IntPtr handle, string className, string name)
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

        internal Rectangle GetRect()
        {
            RECT rect;
            NativeMethods.GetWindowRect(Handle, out rect);
            return rect;
        }

        internal Bitmap Capture()
        {
            return Util.CaptureWindow(Handle);
        }

        internal void Focus()
        {
            NativeMethods.SetForegroundWindow(Handle);
        }

        internal bool Exists()
        {
            return NativeMethods.IsWindow(Handle);
        }

        internal bool IsFocused()
        {
            return NativeMethods.GetForegroundWindow() == Handle;
        }

        public override string ToString()
        {
            return $"{{Handle={Handle}, ClassName={ClassName}, Name={Name}}}";
        }
    }
}
