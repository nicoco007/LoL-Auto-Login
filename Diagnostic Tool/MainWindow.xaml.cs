using LoLAutoLogin;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Linq;
using System.Timers;
using System.Drawing;
using System.Windows;
using System.IO;
using System.Text;
using Microsoft.Win32;

namespace DiagnosticTool
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly DateTime startTime = DateTime.Now;
        private Timer timer;
        private List<WindowInfo> windows = new List<WindowInfo>();

        private bool patcherWindowFound = false;
        private bool loginWindowFound = false;
        private bool alphaClientFound = false;

        private DateTime? patcherWindowStart, loginWindowStart, alphaClientStart, patcherWindowStop, loginWindowStop, alphaClientStop;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void WizardPage_Initialize(object sender, AvalonWizard.WizardPageInitEventArgs e)
        {
            timer = new Timer();
            timer.Interval = 500;
            timer.Elapsed += Timer_Elapsed;
            timer.Enabled = true;
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            // Process names: LoLLauncher.exe, LoLPatcher.exe, LoLPatcherUx.exe, LolClient.exe
            List<KeyValuePair<IntPtr, string>> handles = GetProcessHandles(new String[] { "LoLLauncher", "LoLPatcher", "LoLPatcherUx", "LolClient" });

            foreach (KeyValuePair<IntPtr, string> handle in handles)
            {
                if (!windows.Any(w => w.Handle == handle.Key))
                {
                    StringBuilder windowName = new StringBuilder(256);
                    StringBuilder className = new StringBuilder(256);

                    NativeMethods.GetWindowText(handle.Key, windowName, windowName.Capacity);
                    NativeMethods.GetClassName(handle.Key, className, className.Capacity);

                    // get window pos/size
                    RECT rect;
                    NativeMethods.GetWindowRect(handle.Key, out rect);

                    Image img = ScreenCapture.CaptureWindow(handle.Key);

                    windows.Add(new WindowInfo(handle.Key, handle.Value, windowName.ToString(), className.ToString(), rect, DateTime.Now, img));
                }
                else
                {
                    WindowInfo window = windows.Find(w => w.Handle == handle.Key);
                    RECT rect;
                    NativeMethods.GetWindowRect(handle.Key, out rect);

                    if (window.WindowRect.LastOrDefault() != rect)
                        window.WindowRect.Add(rect);

                    window.Image = ScreenCapture.CaptureWindow(handle.Key);
                }
            }

            foreach (WindowInfo window in windows)
            {
                if (window.KillTime == null && !handles.Any(h => h.Key == window.Handle))
                {
                    window.KillTime = DateTime.Now;
                }
            }
            
            if (LoLAutoLogin.LoLAutoLogin.GetSingleWindowFromSize("LOLPATCHER", "LoL Patcher", 800, 600) != IntPtr.Zero)
            {
                patcherWindowFound = true;
                if (patcherWindowStart == null)
                    patcherWindowStart = DateTime.Now;
            }
            else if (patcherWindowStart != null && patcherWindowStop == null)
                patcherWindowStop = DateTime.Now;

            if (LoLAutoLogin.LoLAutoLogin.GetSingleWindowFromSize("ApolloRuntimeContentWindow", null, 800, 600) != IntPtr.Zero)
            {
                loginWindowFound = true;
                if (loginWindowStart == null)
                    loginWindowStart = DateTime.Now;
            }
            else if (loginWindowStart != null && loginWindowStop == null)
                loginWindowStop = DateTime.Now;

            if (LoLAutoLogin.LoLAutoLogin.GetAlphaClientWindowHandle() != IntPtr.Zero)
            {
                alphaClientFound = true;
                if (alphaClientStart == null)
                    alphaClientStart = DateTime.Now;
            }
            else if (alphaClientStart != null && alphaClientStop == null)
                alphaClientStop = DateTime.Now;
        }

        private List<KeyValuePair<IntPtr, string>> GetProcessHandles(string[] processNames)
        {
            List<KeyValuePair<IntPtr, string>> handles = new List<KeyValuePair<IntPtr, string>>();

            foreach (string processName in processNames)
                handles.AddRange(Process.GetProcessesByName(processName).Where(p => p.MainWindowHandle != IntPtr.Zero).Select(p => new KeyValuePair<IntPtr, string>(p.MainWindowHandle, p.ProcessName)));

            return handles;
        }

        private void WizardPage_Commit(object sender, AvalonWizard.WizardPageConfirmEventArgs e)
        {
            timer.Enabled = false;

            Directory.CreateDirectory("img");

            try
            {
                using (StreamWriter writer = new StreamWriter($@"lolaldiag-{DateTime.Now:yyyy-MM-dd\THH-mm-ss}.dump", false, Encoding.UTF8))
                {
                    writer.WriteLine("LOLALDIAG v{0} - {1:yyyy-MM-dd HH:mm:ss}", FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly().Location).FileVersion, DateTime.Now);
                    writer.WriteLine();
                    writer.WriteLine("System Information");
                    writer.WriteLine("==================");
                    writer.WriteLine("OS: " + GetOSFriendlyName());
                    writer.WriteLine("Architecture: " + (Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit"));
                    writer.WriteLine("Screen resolution(s): " + string.Join(",", System.Windows.Forms.Screen.AllScreens.Select(s => s.Bounds)));
                    writer.WriteLine();
                    writer.WriteLine("Basic Tests");
                    writer.WriteLine("===========");
                    writer.WriteLine(GetTestResult("Patcher window: ", patcherWindowFound, patcherWindowStart, patcherWindowStop));
                    writer.WriteLine(GetTestResult("Login/client window: ", loginWindowFound, loginWindowStart, loginWindowStop));
                    writer.WriteLine(GetTestResult("Alpha client: ", alphaClientFound, alphaClientStart, alphaClientStop));
                    writer.WriteLine();
                    writer.WriteLine("Found {0} windows.", windows.Count);
                    writer.WriteLine();

                    for (int i = 0; i < windows.Count; i++)
                    {
                        writer.WriteLine("    Window #" + i);
                        writer.WriteLine("    > Process Name: " + windows[i].ProcessName);
                        writer.WriteLine("    > Window Name: " + (string.IsNullOrEmpty(windows[i].Name) ? "(EMPTY)" : windows[i].Name));
                        writer.WriteLine("    > Class Name: " + (string.IsNullOrEmpty(windows[i].ClassName) ? "(EMPTY)" : windows[i].ClassName));
                        writer.WriteLine("    > Handle: " + windows[i].Handle);
                        writer.WriteLine("    > Rectangle: " + string.Join(", ", windows[i].WindowRect.ToArray()));
                        writer.WriteLine("    > Killed: " + (windows[i].KillTime != null ? windows[i].KillTime + " (" + (windows[i].KillTime - windows[i].StartTime) + ")" : "Never"));
                        writer.WriteLine("    > Image: " + ImageToBase64(windows[i].Image));
                        writer.WriteLine();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private string ImageToBase64(Image img)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                img.Save(ms, ImageFormat.Jpeg);
                return Convert.ToBase64String(ms.ToArray());
            }
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(e.Uri.AbsoluteUri);
        }

        private void Wizard_Cancelled(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Wizard_Finished(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private string GetLocalMachineString(string path, string key)
        {
            try
            {
                RegistryKey rk = Registry.LocalMachine.OpenSubKey(path);
                if (rk == null) return "";
                return (string) rk.GetValue(key);
            }
            catch { return ""; }
        }

        private string GetOSFriendlyName()
        {
            string productName = GetLocalMachineString(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ProductName");
            string csdVersion = GetLocalMachineString(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "CSDVersion");
            string releaseId = GetLocalMachineString(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ReleaseId");

            if (!string.IsNullOrEmpty(productName))
                return productName + (string.IsNullOrEmpty(csdVersion) ? "" : " " + csdVersion) + (string.IsNullOrEmpty(releaseId) ? "" : " (Release ID: " + releaseId + ")");

            return "Unknown";
        }

        private string GetTestResult(string label, bool found, DateTime? start, DateTime? stop)
        {
            string str = label + found;

            if (start != null && stop != null)
            {
                str += $" (Time: {stop - start})";
            }
            else
            {
                if (start != null)
                    str += $" Start: {start:HH:mm:ss}";

                if (stop != null)
                    str += $" Stop: {stop:HH:mm:ss}";
            }

            return str;
        }
    }
}
