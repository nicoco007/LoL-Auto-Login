using System;
using System.Collections.Generic;
using System.Drawing;
using LoLAutoLogin;

namespace DiagnosticTool
{
    public class WindowInfo
    {
        public IntPtr Handle { get; }
        public string ProcessName { get; }
        public string Name { get; }
        public string ClassName { get; }
        public List<RECT> WindowRect { get; }
        public Image Image { get; set; }
        public DateTime StartTime { get; }
        public DateTime? KillTime { get; set; }

        public WindowInfo(IntPtr handle, string processName, string windowName, string className, Rectangle windowRectangle, DateTime startTime, Image img)
        {
            Handle = handle;
            ProcessName = processName;
            Name = windowName;
            ClassName = className;
            WindowRect = new List<RECT>();
            WindowRect.Add(windowRectangle);
            StartTime = startTime;
            Image = img;
        }
    }
}
