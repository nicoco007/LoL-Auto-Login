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
