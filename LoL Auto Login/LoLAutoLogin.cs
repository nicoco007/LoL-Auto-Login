using System;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
using System.Threading;
using System.IO;
using WindowsInput;
using WindowsInput.Native;

// TODO: fix sloppy code

namespace LoLAutoLogin
{

    public partial class LoLAutoLogin : Form
    {

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
            if(File.Exists("password")) CheckLeagueRunning();
            else Log.Info("Password file not found, prompting user to enter password...");

        }

        private void CheckLeagueRunning()
        {
            
            // hide this window
            this.Hide();

            // start launch process
            Log.Info("Password file found!");

            // check if league of legends is already running
            if (Process.GetProcessesByName("LolClient").Length > 0 || Process.GetProcessesByName("LoLLauncher").Length > 0 || Process.GetProcessesByName("LoLPatcher").Length > 0)
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

                    while (GetSingleWindowFromSize("LOLPATCHER", "LoL Patcher", 800, 600) != IntPtr.Zero) Thread.Sleep(500);

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
            t.IsBackground = true;
            t.Start();

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
                    notifyIcon.ShowBalloonTip(2500, "LoL Auto Login was unable to start League of Legends. Please check your logs for more information.", "LoL Auto Login has encountered a fatal error", ToolTipIcon.Error);
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
            while (patchersw.Elapsed.TotalSeconds < 30 && (patcherHwnd = GetSingleWindowFromSize("LOLPATCHER", "LoL Patcher", 800, 600)) == IntPtr.Zero)
            {
                Thread.Sleep(500);
            }

            // check if patcher window was found
            if (patcherHwnd != IntPtr.Zero)
            {
                // get patcher rectangle (window pos and size)
                RECT patcherRect;
                NativeMethods.GetWindowRect(patcherHwnd, out patcherRect);

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
                        
                        NativeMethods.GetWindowRect(patcherHwnd, out patcherRect);
                        NativeMethods.SetForegroundWindow(patcherHwnd);

                        Log.Info("Found Launch button after " + patchersw.ElapsedMilliseconds + " ms. Initiating click.");

                        InputSimulator sim = new InputSimulator();
                        sim.Mouse.LeftButtonUp();
                        Cursor.Position = new Point(patcherRect.Left + (int)(patcherRect.Width * 0.5), patcherRect.Top + (int)(patcherRect.Height * 0.025));
                        sim.Mouse.LeftButtonClick();
                        
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
                    Log.Error("Launch button failed to enable after 30 seconds. Aborting!");

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
                Log.Error("Patcher not found after 30 seconds. Aborting!");

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
            while (sw.Elapsed.Seconds < 30 && GetSingleWindowFromSize("ApolloRuntimeContentWindow", null, 800, 600) == IntPtr.Zero)
            {
                Thread.Sleep(200);
            }

            // check if client was found
            if (GetSingleWindowFromSize("ApolloRuntimeContentWindow", null, 800, 600) != IntPtr.Zero)
            {
                // get client window handle
                IntPtr hwnd = GetSingleWindowFromSize("ApolloRuntimeContentWindow", null, 800, 600);
                
                // get client window rectangle
                RECT rect;
                NativeMethods.GetWindowRect(hwnd, out rect);

                // log information found
                Log.Info("Found patcher after " + sw.ElapsedMilliseconds + " ms [Handle=" + hwnd.ToString() + ", Rectangle=" + rect.ToString() + "," + rect.Size.ToString() + "]");
                Log.Info("Waiting 15 seconds for login form to appear...");

                // reset stopwatch for form loading
                sw.Reset();
                sw.Start();

                Bitmap clientImage = new Bitmap(ScreenCapture.CaptureWindow(hwnd));

                bool found = false;

                while (sw.Elapsed.Seconds < 30 && !found)
                {
                    Log.Verbose("[Handle=" + hwnd.ToString() + ", Rectangle=" + rect.ToString() + "," + rect.Size.ToString() + "]");

                    NativeMethods.GetWindowRect(hwnd, out rect);

                    clientImage = new Bitmap(ScreenCapture.CaptureWindow(hwnd));

                    if (Pixels.PasswordBox.Compare(clientImage)) found = true;

                    clientImage.Dispose();

                    GC.Collect();

                    Thread.Sleep(500);
                }

                // check if password box was found
                if (found)
                {
                    // log information
                    Log.Info("Found password box after " + sw.ElapsedMilliseconds + " ms. Reading & decrypting password from file...");

                    NativeMethods.SetForegroundWindow(hwnd);

                    // create password string
                    string password;
                    
                    // try to read password from file
                    try
                    {

                        using (FileStream file = new FileStream("password", FileMode.Open, FileAccess.Read))
                        {

                            byte[] buffer = new byte[file.Length];
                            file.Read(buffer, 0, (int)file.Length);

                            password = Encryption.Decrypt(buffer);

                        }

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
                    
                    InputSimulator sim = new InputSimulator();

                    // enter password one character at a time
                    while (i <= passArray.Length && sw.Elapsed.Seconds < 30)
                    {
                        // get window rectangle, in case it is resized or moved
                        NativeMethods.GetWindowRect(hwnd, out rect);
                        Log.Verbose("Client rectangle=" + rect.ToString());

                        // move cursor above password box
                        sim.Mouse.LeftButtonUp();
                        NativeMethods.SetForegroundWindow(hwnd);
                        Cursor.Position = new Point(rect.Left + (int)(rect.Width * 0.192), rect.Top + (int)(rect.Height * 0.480));
                        
                        // focus window & click on password box
                        sim.Mouse.LeftButtonClick();

                        if (NativeMethods.GetForegroundWindow() == hwnd)
                        {
                            // enter password character, press enter if complete
                            if (i != passArray.Length)
                                this.Invoke(new Action(() =>
                                {
                                    
                                    sim.Keyboard.KeyPress(VirtualKeyCode.END);
                                    sim.Keyboard.TextEntry(passArray[i].ToString());

                                }));
                            else
                                this.Invoke(new Action(() =>
                                {

                                    sim.Keyboard.KeyPress(VirtualKeyCode.RETURN);

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

                using (FileStream file = new FileStream("password", FileMode.OpenOrCreate, FileAccess.Write))
                {

                    byte[] data = Encryption.Encrypt(passTextBox.Text);

                    file.Write(data, 0, data.Length);

                }

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
            CheckLeagueRunning();
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
            Log.Verbose(string.Format("Trying to find window handle [ClassName={0},WindowName={1},Size={2}]", (lpWindowName != null ? lpWindowName : "null"), (lpClassName != null ? lpClassName : "null"), new Size(width, height).ToString()));
            
            // try to get window handle and rectangle using specified arguments
            IntPtr hwnd = NativeMethods.FindWindow(lpClassName, lpWindowName);
            RECT rect = new RECT();
            NativeMethods.GetWindowRect(hwnd, out rect);

            // check if handle is nothing
            if (hwnd == IntPtr.Zero)
            {
                // log that we didn't find a window
                Log.Verbose("Failed to find window with specified arguments!");

                return IntPtr.Zero;
            }

            // log what we found
            Log.Verbose(string.Format("Found window [Handle={0},Rectangle={1}]", hwnd.ToString(), rect.ToString()));

            if (rect.Size.Width >= width && rect.Size.Height >= height)
            {
                Log.Verbose("Correct window handle found!");
                
                return hwnd;
            }
            else
            {
                while(NativeMethods.FindWindowEx(IntPtr.Zero, hwnd, lpClassName, lpWindowName) != IntPtr.Zero)
                {
                    hwnd = NativeMethods.FindWindowEx(IntPtr.Zero, hwnd, lpClassName, lpWindowName);
                    NativeMethods.GetWindowRect(hwnd, out rect);

                    Log.Verbose(string.Format("Found window [Handle={0},Rectangle={1}]", hwnd.ToString(), rect.ToString()));

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

            foreach (Process p in Process.GetProcessesByName(pName)) p.Kill();

        }

        public new void Hide()
        {

            this.Opacity = 0.0f;
            this.ShowInTaskbar = false;
            base.Hide();

        }

    }

}
