using System;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading;
using System.IO;
using System.Security.Cryptography;

namespace LoLAutoLogin
{
    public partial class LoLAutoLogin : Form
    {
        // P/Invoke Methods
        #region pinvoke methods
        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern int GetWindowText(System.IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);

        [DllImport("kernel32.dll")]
        static extern uint GetLastError();

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("gdi32.dll")]
        static extern int GetPixel(IntPtr hdc, int nXPos, int nYPos);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, int dwExtraInfo);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        
        [Flags]
        public enum MouseEventFlags : uint
        {
            LEFTDOWN = 0x00000002,
            LEFTUP = 0x00000004,
            MIDDLEDOWN = 0x00000020,
            MIDDLEUP = 0x00000040,
            MOVE = 0x00000001,
            ABSOLUTE = 0x00008000,
            RIGHTDOWN = 0x00000008,
            RIGHTUP = 0x00000010,
            WHEEL = 0x00000800,
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left, Top, Right, Bottom;

            public RECT(int left, int top, int right, int bottom)
            {
                Left = left;
                Top = top;
                Right = right;
                Bottom = bottom;
            }

            public RECT(System.Drawing.Rectangle r) : this(r.Left, r.Top, r.Right, r.Bottom) { }

            public int X
            {
                get { return Left; }
                set { Right -= (Left - value); Left = value; }
            }

            public int Y
            {
                get { return Top; }
                set { Bottom -= (Top - value); Top = value; }
            }

            public int Height
            {
                get { return Bottom - Top; }
                set { Bottom = value + Top; }
            }

            public int Width
            {
                get { return Right - Left; }
                set { Right = value + Left; }
            }

            public System.Drawing.Point Location
            {
                get { return new System.Drawing.Point(Left, Top); }
                set { X = value.X; Y = value.Y; }
            }

            public System.Drawing.Size Size
            {
                get { return new System.Drawing.Size(Width, Height); }
                set { Width = value.Width; Height = value.Height; }
            }

            public static implicit operator System.Drawing.Rectangle(RECT r)
            {
                return new System.Drawing.Rectangle(r.Left, r.Top, r.Width, r.Height);
            }

            public static implicit operator RECT(System.Drawing.Rectangle r)
            {
                return new RECT(r);
            }

            public static bool operator ==(RECT r1, RECT r2)
            {
                return r1.Equals(r2);
            }

            public static bool operator !=(RECT r1, RECT r2)
            {
                return !r1.Equals(r2);
            }

            public bool Equals(RECT r)
            {
                return r.Left == Left && r.Top == Top && r.Right == Right && r.Bottom == Bottom;
            }

            public override bool Equals(object obj)
            {
                if (obj is RECT)
                    return Equals((RECT)obj);
                else if (obj is System.Drawing.Rectangle)
                    return Equals(new RECT((System.Drawing.Rectangle)obj));
                return false;
            }

            public override int GetHashCode()
            {
                return ((System.Drawing.Rectangle)this).GetHashCode();
            }

            public override string ToString()
            {
                return string.Format(System.Globalization.CultureInfo.CurrentCulture, "{{Left={0},Top={1},Right={2},Bottom={3}}}", Left, Top, Right, Bottom);
            }
        }
        #endregion

        public LoLAutoLogin()
        {
            InitializeComponent();

            // create notification icon context menu (so user can exit if program hangs)
            ContextMenu cm = new ContextMenu();
            MenuItem mi = new MenuItem("&Exit", (sender, e) => Application.Exit());
            cm.MenuItems.Add(mi);
            notifyIcon.ContextMenu = cm;

            // set accept button (will be activated when 'enter' key is pressed)
            this.AcceptButton = saveButton;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Log.Info("Started LoL Auto Login");

            // check if program is in same directory as league of legends
            if(!File.Exists("lol.launcher.exe"))
            {
                Log.Fatal("\"lol.launcher.exe\" not found!");
                // show error message
                MessageBox.Show(this, "Please place LoL Auto Login in your League of Legends directory (beside the \"lol.launcher.exe\" file).", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);

                // hide form so it doesn't flash on screen
                this.Opacity = 0.0F;

                // exit application
                Application.Exit();

                // return so no other commands are executed
                return;
            }
            
            // check if password file exists
            if(File.Exists("password"))
            {
                BeginOperation();
            }
            else
            {
                Log.Info("Password file not found, prompting user to enter password...");
            }
        }

        private void BeginOperation()
        {
            // hide this window
            this.Opacity = 0.0F;
            this.Hide();
            this.ShowInTaskbar = false;

            // start launch process
            Log.Info("Password file found!");

            // check if league of legends is already running
            if (Process.GetProcessesByName("LolClient").Count() > 0 || Process.GetProcessesByName("LoLLauncher").Count() > 0 || Process.GetProcessesByName("LoLPatcher").Count() > 0)
            {
                Log.Warn("League of Legends is already running!");

                // prompt user to kill current league of legends process
                if (MessageBox.Show(this, "Another instance of League of Legends is currently running. Would you like to close it?", "League of Legends is already running!", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1) == DialogResult.Yes)
                {
                    Log.Info("Attempting to kill all League of Legends instances...");

                    // kill all league of legends processes
                    KillProcessesByName("LolClient");
                    KillProcessesByName("LoLLauncher");
                    KillProcessesByName("LoLPatcher");

                    while (GetSingleWindowFromSize("LOLPATCHER", "LoL Patcher", 1280, 800) != IntPtr.Zero)
                    {
                        Thread.Sleep(500);
                    }
                }
                else
                {
                    // exit if user says no
                    Application.Exit();
                    return;
                }
            }

            Log.Debug("Attempting to start thread...");

            Thread t = new Thread(PatcherLaunch);
            t.Start();

            this.FormClosing += (s, args) =>
            {
                t.Abort();
            };
        }

        private void PatcherLaunch()
        {
            // try launching league of legends
            try
            {
                Process.Start("lol.launcher.exe");
            }
            catch (Exception ex)
            {
                // print error to log and show balloon tip to inform user of fatal error
                Log.Fatal("Could not start League of Legends!");
                Log.PrintStackTrace(ex.StackTrace);
                this.Invoke(new Action(() => {
                    notifyIcon.ShowBalloonTip(2500, "LoL Auto Login encountered a fatal error and will now exit. Please check your logs for more information.", "LoL Auto Login has encountered a fatal error", ToolTipIcon.Error);
                }));
                
                // exit application
                Application.Exit();
                return;
            }

            // log
            Log.Info("Waiting 30 seconds for League of Legends Patcher...");

            // create stopwatch for loading timeout
            Stopwatch patchersw = new Stopwatch();
            patchersw.Start();

            IntPtr patcherHwnd = IntPtr.Zero;

            // search for the patcher window for 30 seconds
            while (patchersw.Elapsed.TotalSeconds < 30 && (patcherHwnd = GetSingleWindowFromSize("LOLPATCHER", "LoL Patcher", 1280, 800)) == IntPtr.Zero)
            {
                Thread.Sleep(500);
            }

            // check if patcher window was found
            if (patcherHwnd != IntPtr.Zero)
            {
                // get patcher rectangle (window pos and size)
                RECT patcherRect;
                GetWindowRect(patcherHwnd, out patcherRect);

                Log.Info("Found patcher after " + patchersw.ElapsedMilliseconds + " ms [Handle=" + patcherHwnd.ToString() + ", Rectangle=" + patcherRect.ToString() + "]");

                // reset stopwatch so it restarts for launch button search
                patchersw.Reset();
                patchersw.Start();

                Log.Info("Waiting 30 seconds for Launch button to enable...");
                
                bool clicked = false;

                // check if the "Launch" button is there and can be clicked
                while (patchersw.Elapsed.TotalSeconds < 30 && !clicked)
                {              
                    // get patcher image
                    Bitmap patcherImage = new Bitmap(ScreenCapture.CaptureWindow(patcherHwnd));

                    // check if the launch button is enabled
                    if(Pixels.LaunchButton.Compare(patcherImage))
                    {

                        GetWindowRect(patcherHwnd, out patcherRect);
                        SetForegroundWindow(patcherHwnd);

                        Log.Info("Found Launch button after " + patchersw.ElapsedMilliseconds + " ms. Initiating click.");

                        mouse_event((uint)MouseEventFlags.LEFTUP, (uint)Cursor.Position.X, (uint)Cursor.Position.Y, 0, 0);
                        Cursor.Position = new Point(patcherRect.Left + 640, patcherRect.Top + 20);
                        mouse_event((uint)MouseEventFlags.LEFTDOWN, (uint)Cursor.Position.X, (uint)Cursor.Position.Y, 0, 0);
                        mouse_event((uint)MouseEventFlags.LEFTUP, (uint)Cursor.Position.X, (uint)Cursor.Position.Y, 0, 0);
                        
                        clicked = true;

                        patchersw.Stop();

                        EnterPassword();
                    }

                    // dispose of image
                    patcherImage.Dispose();

                    // force garbage collection
                    GC.Collect();

                    Thread.Sleep(500);

                }

                if(patchersw.Elapsed.Seconds >= 15)
                {
                    // print error to log
                    Log.Error("Launch button failed to enable after 15 seconds. Aborting!");

                    // stop stopwatch
                    patchersw.Stop();

                    // exit application
                    Application.Exit();
                    return;
                }
            }
            else
            {
                // print error to log
                Log.Error("Patcher not found after 15 seconds. Aborting!");

                // stop stopwatch
                patchersw.Stop();

                // exit application
                Application.Exit();
                return;
            }
        }

        private void EnterPassword()
        {
            // create new stopwatch for client searching timeout
            Stopwatch sw = new Stopwatch();
            sw.Start();

            // log
            Log.Info("Waiting 15 seconds for League of Legends client...");

            // try to find league of legends client for 30 seconds
            while (sw.Elapsed.Seconds < 15 && GetSingleWindowFromSize("ApolloRuntimeContentWindow", null, 1024, 640) == IntPtr.Zero)
            {
                Thread.Sleep(200);
            }

            // check if client was found
            if (GetSingleWindowFromSize("ApolloRuntimeContentWindow", null, 1024, 640) != IntPtr.Zero)
            {
                // get client window handle
                IntPtr hwnd = GetSingleWindowFromSize("ApolloRuntimeContentWindow", null, 1024, 640);
                
                // get client window rectangle
                RECT rect;
                GetWindowRect(hwnd, out rect);

                // log information found
                Log.Info("Found patcher after " + sw.ElapsedMilliseconds + " ms [Handle=" + hwnd.ToString() + ", Rectangle=" + rect.ToString() + "," + rect.Size.ToString() + "]");
                Log.Info("Waiting 15 seconds for login form to appear...");

                // reset stopwatch for form loading
                sw.Reset();
                sw.Start();

                Bitmap clientImage = new Bitmap(ScreenCapture.CaptureWindow(hwnd));

                bool found = false;

                while (sw.Elapsed.Seconds < 15 && !found)
                {
                    Log.Verbose("[Handle=" + hwnd.ToString() + ", Rectangle=" + rect.ToString() + "," + rect.Size.ToString() + "]");

                    GetWindowRect(hwnd, out rect);

                    clientImage = new Bitmap(ScreenCapture.CaptureWindow(hwnd));

                    if (Pixels.PasswordBox.Compare(clientImage))
                    {

                        found = true;

                    }

                    clientImage.Dispose();

                    GC.Collect();

                    Thread.Sleep(500);
                }

                // check if password box was found
                if (found)
                {
                    // log information
                    Log.Info("Found password box after " + sw.ElapsedMilliseconds + " ms. Reading & decrypting password from file...");

                    SetForegroundWindow(hwnd);

                    // create password string
                    string password;
                    
                    // try to read password from file
                    try
                    {
                        // read and decrypt password
                        using (StreamReader sr = new StreamReader("password"))
                            password = Decrypt(sr.ReadToEnd());
                    }
                    catch(Exception ex)
                    {
                        // print exception & stacktrace to log
                        Log.Fatal("Password file could not be read!");
                        Log.PrintStackTrace(ex.StackTrace);

                        // show balloon tip to inform user of error
                        this.Invoke(new Action(() =>
                        {
                            notifyIcon.ShowBalloonTip(2500, "LoL Auto Login encountered a fatal error and will now exit. Please check your logs for more information.", "LoL Auto Login has encountered a fatal error", ToolTipIcon.Error);
                        }));

                        // exit application
                        Application.Exit();
                        return;
                    }
                    
                    // create character array from password
                    char[] passArray = password.ToCharArray();
                    
                    // log
                    Log.Info("Entering password...");

                    int i = 0;

                    // enter password one character at a time
                    while(i <= passArray.Length && sw.Elapsed.Seconds < 30)
                    {
                        // get window rectangle, in case it is resized or moved
                        GetWindowRect(hwnd, out rect);
                        Log.Verbose("Client rectangle=" + rect.ToString());

                        // move cursor above password box
                        mouse_event((uint)MouseEventFlags.LEFTUP, (uint)Cursor.Position.X, (uint)Cursor.Position.Y, 0, 0);
                        SetForegroundWindow(hwnd);
                        Cursor.Position = new Point(rect.Left + (int)(rect.Width * 0.192), rect.Top + (int)(rect.Height * 0.480));

                        // focus window & click on password box
                        mouse_event((uint)MouseEventFlags.LEFTDOWN, (uint)Cursor.Position.X, (uint)Cursor.Position.Y, 0, 0);
                        mouse_event((uint)MouseEventFlags.LEFTUP, (uint)Cursor.Position.X, (uint)Cursor.Position.Y, 0, 0);

                        if(GetForegroundWindow() == hwnd)
                        {
                            // enter password character, press enter if complete
                            if (i != passArray.Length)
                                this.Invoke(new Action(() =>
                                {
                                    SendKeys.Send("{END}" + passArray[i].ToString());
                                }));
                            else
                                this.Invoke(new Action(() =>
                                {
                                    SendKeys.Send("~");
                                }));

                            i++;
                        }
                    }

                    
                }
                else
                {
                    // print error to log
                    Log.Error("Password box not found after 15 seconds. Aborting!");

                    // stop stopwatch
                    sw.Stop();

                    // exit application
                    Application.Exit();
                    return;
                }
            }
            else
            {
                // print error to log
                Log.Error("Client window not found after 15 seconds. Aborting!");

                // stop stopwatch
                sw.Stop();

                // exit application
                Application.Exit();
                return;
            }

            // log success message
            Log.Info("Success!");

            // close program
            Application.Exit();
        }

        private void saveButton_Click(object sender, EventArgs e)
        {
            // check if a password was inputted
            if (String.IsNullOrEmpty(passTextBox.Text))
            {
                MessageBox.Show(this, "You must enter a valid password!", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // log
            Log.Info("Encrypting & saving password to file...");

            // try to write password to file
            try
            {
                // encrypt and write password to file
                using (StreamWriter sw = new StreamWriter("password"))
                    sw.Write(Encrypt(passTextBox.Text));
            }
            catch (Exception ex)
            {
                // show error message
                MessageBox.Show(this, "Something went wrong when trying to save your password:" + Environment.NewLine + Environment.NewLine + ex.StackTrace, "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);

                // print error message to log
                Log.Fatal("Could not save password to file!");
                Log.PrintStackTrace(ex.StackTrace);
            }

            // hide this window
            this.Opacity = 0.0F;
            this.Hide();

            // start launch process
            BeginOperation();
        }

        /// <summary>
        /// Gets a window with specified size using class name and window name.
        /// </summary>
        /// <param name="lpClassName">Class name</param>
        /// <param name="lpWindowName">Window name</param>
        /// <param name="width">Window minimum width</param>
        /// <param name="height">Window minimum height</param>
        /// <returns>The specified window's handle</returns>
        private IntPtr GetSingleWindowFromSize(string lpClassName, string lpWindowName, int width, int height)
        {
            // log what we are looking for
            Log.Verbose(String.Format("Trying to find window handle [ClassName={0},WindowName={1},Size={2}]", (lpWindowName != null ? lpWindowName : "null"), (lpClassName != null ? lpClassName : "null"), new Size(width, height).ToString()));
            
            // try to get window handle and rectangle using specified arguments
            IntPtr hwnd = FindWindow(lpClassName, lpWindowName);
            RECT rect = new RECT();
            GetWindowRect(hwnd, out rect);

            // check if handle is nothing
            if (hwnd == IntPtr.Zero)
            {
                // log that we didn't find a window
                Log.Verbose("Failed to find window with specified arguments!");

                return IntPtr.Zero;
            }

            // log what we found
            Log.Verbose(String.Format("Found window [Handle={0},Rectangle={1}]", hwnd.ToString(), rect.ToString()));

            if (rect.Size.Width >= width && rect.Size.Height >= height)
            {
                Log.Verbose("Correct window handle found!");
                
                return hwnd;
            }
            else
            {
                while(FindWindowEx(IntPtr.Zero, hwnd, lpClassName, lpWindowName) != IntPtr.Zero)
                {
                    hwnd = FindWindowEx(IntPtr.Zero, hwnd, lpClassName, lpWindowName);
                    GetWindowRect(hwnd, out rect);

                    Log.Verbose(String.Format("Found window [Handle={0},Rectangle={1}]", hwnd.ToString(), rect.ToString()));

                    if (rect.Size.Width >= width && rect.Size.Height >= height)
                    {
                        Log.Verbose("Correct window handle found!");

                        return hwnd;
                    }
                }
            }

            return IntPtr.Zero;
        }

        /// <summary>
        /// Kills every process with the specified name.
        /// </summary>
        /// <param name="pName">Name of process(es) to kill</param>
        public void KillProcessesByName(string pName)
        {
            Log.Verbose("Killing all " + pName + " processes.");
            foreach(Process p in Process.GetProcessesByName(pName))
            {
                p.Kill();
            }
        }

        // taken from http://www.aspsnippets.com/Articles/Encrypt-and-Decrypt-Username-or-Password-stored-in-database-in-ASPNet-using-C-and-VBNet.aspx by Mudassar Ahmed Khan, Oct 18 2013  
        /// <summary>
        /// Encrypt text using AES.
        /// </summary>
        /// <param name="clearText">Text to encrypt.</param>
        /// <returns>Encrypted text.</returns>
        private string Encrypt(string clearText)
        {
            string EncryptionKey = "MAKV2SPBNI99212";
            byte[] clearBytes = Encoding.Unicode.GetBytes(clearText);
            using (Aes encryptor = Aes.Create())
            {
                Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(EncryptionKey, new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });
                encryptor.Key = pdb.GetBytes(32);
                encryptor.IV = pdb.GetBytes(16);
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(clearBytes, 0, clearBytes.Length);
                        cs.Close();
                    }
                    clearText = Convert.ToBase64String(ms.ToArray());
                }
            }
            return clearText;
        }

        /// <summary>
        /// Decrypt AES encoded text.
        /// </summary>
        /// <param name="cipherText">Encrypted text.</param>
        /// <returns>Decrypted text.</returns>
        private string Decrypt(string cipherText)
        {
            string EncryptionKey = "MAKV2SPBNI99212";
            byte[] cipherBytes = Convert.FromBase64String(cipherText);
            using (Aes encryptor = Aes.Create())
            {
                Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(EncryptionKey, new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });
                encryptor.Key = pdb.GetBytes(32);
                encryptor.IV = pdb.GetBytes(16);
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(cipherBytes, 0, cipherBytes.Length);
                        cs.Close();
                    }
                    cipherText = Encoding.Unicode.GetString(ms.ToArray());
                }
            }
            return cipherText;
        }
    }
}
