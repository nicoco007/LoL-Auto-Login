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

using System;
using System.Runtime.InteropServices;
using System.Text;

namespace LoLAutoLogin.Native
{
    internal class NativeMethods
    {
        internal const int SRCCOPY = 0x00CC0020;
        
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern IntPtr GetParent(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        internal static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        internal static extern short GetAsyncKeyState(System.Windows.Forms.Keys vKey);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool IsWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        internal static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll")]
        internal static extern IntPtr GetWindowDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        internal static extern bool ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool SetProcessDPIAware();

        [DllImport("kernel32.dll")]
        internal static extern uint GetLastError();

        [DllImport("gdi32.dll")]
        internal static extern int GetPixel(IntPtr hdc, int nXPos, int nYPos);

        [DllImport("gdi32.dll")]
        internal static extern bool BitBlt(IntPtr hObject, int nXDest, int nYDest,
            int nWidth, int nHeight, IntPtr hObjectSource,
            int nXSrc, int nYSrc, int dwRop);

        [DllImport("gdi32.dll")]
        internal static extern IntPtr CreateCompatibleBitmap(IntPtr hDC, int nWidth,
            int nHeight);

        [DllImport("gdi32.dll")]
        internal static extern IntPtr CreateCompatibleDC(IntPtr hDC);

        [DllImport("gdi32.dll")]
        internal static extern bool DeleteDC(IntPtr hDC);

        [DllImport("gdi32.dll")]
        internal static extern bool DeleteObject(IntPtr hObject);

        [DllImport("gdi32.dll")]
        internal static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObject);
        
        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        internal static extern IntPtr ILCreateFromPathW(string pszPath);

        [DllImport("shell32.dll")]
        internal static extern int SHOpenFolderAndSelectItems(IntPtr pidlFolder, int cild, IntPtr apidl, int dwFlags);

        [DllImport("shell32.dll")]
        internal static extern void ILFree(IntPtr pidl);

        [DllImport("user32.dll")]
        internal static extern int SendMessage(IntPtr hWnd, uint Msg, int wParam, int lParam);

        [DllImport("user32.dll")]
        internal static extern int MapVirtualKey(uint uCode, uint uMapType);

        [DllImport("user32.dll")]
        internal static extern uint WaitForInputIdle(IntPtr hProcess, uint dwMilliseconds);
    }
}
