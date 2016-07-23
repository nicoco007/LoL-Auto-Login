using Microsoft;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WindowsInput;
using WindowsInput.Native;

// TODO: fix sloppy code (make more modular)
// TODO: fix the derpy bug that sometimes causes the launcher/client not to focus correctly (this might be fixed by the new client, will see)

namespace LoLAutoLogin
{

    public partial class LoLAutoLogin : Form
    {

        const int patcherTimeout = 30000;
        const int launchTimeout = 30000;
        const int clientTimeout = 30000;
        const int passwordTimeout = 30000;
        private bool isAlpha = false;

        public LoLAutoLogin()
        {

            InitializeComponent();

            isAlpha = Environment.GetCommandLineArgs().Contains("--alpha");

            // create notification icon context menu (so user can exit if program hangs)
            ContextMenu menu = new ContextMenu();
            MenuItem item = new MenuItem("&Exit", (sender, e) => Application.Exit());
            menu.MenuItems.Add(item);
            notifyIcon.ContextMenu = menu;

            // set accept button (will be activated when 'enter' key is pressed)
            this.AcceptButton = saveButton;

        }
        
        private async void Form1_Load(object sender, EventArgs e)
        {

            Log.Info("Started LoL Auto Login v{0}", Assembly.GetEntryAssembly().GetName().Version);

            if(CheckLocation())
            {

                if (PasswordExists())
                {
                    if (isAlpha)
                        await RunAlphaClient();
                    else
                        RunPatcher();
                }
                else
                {
                    Log.Info("Password file not found, prompting user to enter password...");
                }

            }

        }

        private bool CheckLocation()
        {

            // check if program is in same directory as league of legends
            if (!File.Exists("lol.launcher.exe"))
            {

                Log.Fatal("\"lol.launcher.exe\" not found!");

                // show error message
                MessageBox.Show(this, "Please place LoL Auto Login in your League of Legends directory (beside the \"lol.launcher.exe\" file).", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);

                // hide form so it doesn't flash on screen
                this.Opacity = 0.0F;

                // exit application
                Application.Exit();

                // return so no other commands are executed
                return false;

            }

            return true;

        }

        private bool PasswordExists()
        {

            if (File.Exists("password"))
            {

                using (StreamReader reader = new StreamReader("password"))
                {

                    if (Regex.IsMatch(reader.ReadToEnd(), @"^[a-zA-Z0-9\+\/]*={0,3}$"))
                    {

                        Log.Info("Password is old format, prompting user to enter password again...");
                        MessageBox.Show("Password encryption has been changed. You will be prompted to enter your password once again.", "LoL Auto Login - Encryption method changed", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    }
                    else
                    {

                        return true;

                    }

                }

            }

            return false;

        }

        private bool CheckLeagueRunning()
        {

            if(Process.GetProcessesByName("LolClient").Length > 0 || Process.GetProcessesByName("LoLLauncher").Length > 0 || Process.GetProcessesByName("LoLPatcher").Length > 0)
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

                    while (GetSingleWindowFromSize("LOLPATCHER", "LoL Patcher", 800, 600) != IntPtr.Zero)
                        Thread.Sleep(500);

                    return false;

                }
                else
                {

                    // exit if user says no
                    Application.Exit();
                    return true;

                }

            }

            return false;

        }

        private void RunPatcher()
        {
            
            // hide this window
            this.Hide();

            // start launch process
            Log.Info("Password file found!");

            // check if league of legends is already running
            if (!CheckLeagueRunning())
            {

                Log.Debug("Attempting to start thread...");

                Thread t = new Thread(PatcherLaunch);

                this.FormClosing += (s, args) =>
                {
                    if (t != null && t.IsAlive)
                    {
                        t.Abort();
                    }
                };

                t.IsBackground = true;
                t.Start();

            }

        }

        private bool StartClient()
        {

            // try launching league of legends
            try
            {

                Process.Start("lol.launcher.exe");

                return true;

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
                return false;

            }

        }

        private void PatcherLaunch()
        {

            if(StartClient())
            {

                // log
                Log.Info("Waiting {0} ms for League of Legends Patcher...", patcherTimeout);

                // create stopwatch for loading timeout
                Stopwatch patchersw = new Stopwatch();
                patchersw.Start();

                IntPtr patcherHwnd = IntPtr.Zero;

                // search for the patcher window for 30 seconds
                while (patchersw.ElapsedMilliseconds < patcherTimeout && (patcherHwnd = GetSingleWindowFromSize("LOLPATCHER", "LoL Patcher", 800, 600)) == IntPtr.Zero)
                    Thread.Sleep(500);

                // check if patcher window was found
                if (patcherHwnd != IntPtr.Zero)
                {

                    // get patcher rectangle (window pos and size)
                    RECT patcherRect;
                    NativeMethods.GetWindowRect(patcherHwnd, out patcherRect);

                    Log.Info("Found patcher after {0} ms {{Handle={1}, Rectangle={2}}}", patchersw.Elapsed.TotalMilliseconds, patcherHwnd, patcherRect);

                    // reset stopwatch so it restarts for launch button search
                    patchersw.Reset();
                    patchersw.Start();

                    Log.Info("Waiting for Launch button to enable...");

                    bool clicked = false;
                    bool sleepMode = false;

                    // check if the "Launch" button is there and can be clicked
                    while (!clicked && patcherHwnd != IntPtr.Zero)
                    {
                        
                        // get patcher image
                        Bitmap patcherImage = new Bitmap(ScreenCapture.CaptureWindow(patcherHwnd));

                        // check if the launch button is enabled
                        if (Pixels.LaunchButton.Match(patcherImage))
                        {

                            // get patcher rectangle and make patcher go to top
                            NativeMethods.GetWindowRect(patcherHwnd, out patcherRect);
                            NativeMethods.SetForegroundWindow(patcherHwnd);

                            Log.Info("Found Launch button after {0} ms. Initiating click.", patchersw.Elapsed.TotalMilliseconds);

                            // use new input simulator instance to click on "Launch" button.
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

                        if(!sleepMode && patchersw.ElapsedMilliseconds >launchTimeout)
                        {

                            Log.Info("Launch button not enabling; going into sleep mode.");

                            sleepMode = true;

                        }

                        if(sleepMode)
                            Thread.Sleep(2000);
                        else
                            Thread.Sleep(500);

                        patcherHwnd = GetSingleWindowFromSize("LOLPATCHER", "LoL Patcher", 800, 600);

                    }

                }
                else
                {

                    // print error to log
                    Log.Error("Patcher not found after {0} ms. Aborting!", patcherTimeout);

                    // stop stopwatch
                    patchersw.Stop();

                }

            }
            
            // exit application
            Application.Exit();
            return;

        }

        private void EnterPassword()
        {
            
            // create new stopwatch for client searching timeout
            Stopwatch sw = new Stopwatch();
            sw.Start();

            // log
            Log.Info("Waiting {0} ms for League of Legends client...", clientTimeout);

            // try to find league of legends client for 30 seconds
            while (sw.ElapsedMilliseconds < clientTimeout && GetSingleWindowFromSize("ApolloRuntimeContentWindow", null, 800, 600) == IntPtr.Zero) Thread.Sleep(200);

            // check if client was found
            if (GetSingleWindowFromSize("ApolloRuntimeContentWindow", null, 800, 600) != IntPtr.Zero)
            {
                // get client window handle
                IntPtr hwnd = GetSingleWindowFromSize("ApolloRuntimeContentWindow", null, 800, 600);
                
                // get client window rectangle
                RECT rect;
                NativeMethods.GetWindowRect(hwnd, out rect);

                // log information found
                Log.Info("Found patcher after {0} ms {{Handle={1}, Rectangle={{Coordinates={2}, Size={3}}}}}", sw.Elapsed.TotalMilliseconds, hwnd, rect, rect.Size);
                Log.Info("Waiting 15 seconds for login form to appear...");

                // reset stopwatch for form loading
                sw.Reset();
                sw.Start();

                Bitmap clientImage = new Bitmap(ScreenCapture.CaptureWindow(hwnd));

                bool found = false;

                while (sw.ElapsedMilliseconds < passwordTimeout && !found && hwnd != IntPtr.Zero)
                {

                    Log.Verbose("{{Handle={0}, Rectangle={{Coordinates={1}, Size={2}}}}}", hwnd, rect, rect.Size);

                    NativeMethods.GetWindowRect(hwnd, out rect);

                    clientImage = new Bitmap(ScreenCapture.CaptureWindow(hwnd));

                    found = Pixels.PasswordBox.Match(clientImage);

                    clientImage.Dispose();

                    GC.Collect();

                    Thread.Sleep(500);

                    hwnd = GetSingleWindowFromSize("ApolloRuntimeContentWindow", null, 800, 600);

                }

                // check if password box was found
                if (found)
                {
                    // log information
                    Log.Info("Found password box after {0} ms. Reading & decrypting password from file...", sw.Elapsed.TotalMilliseconds);

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
                    while (i <= passArray.Length && sw.Elapsed.Seconds < 30 && hwnd != IntPtr.Zero)
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
                            {
                                sim.Keyboard.KeyPress(VirtualKeyCode.END);
                                sim.Keyboard.TextEntry(passArray[i].ToString());
                            }
                            else
                                sim.Keyboard.KeyPress(VirtualKeyCode.RETURN);

                            i++;
                        }

                        hwnd = GetSingleWindowFromSize("ApolloRuntimeContentWindow", null, 800, 600);

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

        private async void saveButton_Click(object sender, EventArgs e)
        {
            // check if a password was inputted
            if (string.IsNullOrEmpty(passTextBox.Text))
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
            if (isAlpha)
                await RunAlphaClient();
            else
                RunPatcher();
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
            Log.Debug(string.Format("Trying to find window handle [ClassName={0},WindowName={1},Size={2}]", (lpWindowName != null ? lpWindowName : "null"), (lpClassName != null ? lpClassName : "null"), new Size(width, height).ToString()));
            
            // try to get window handle and rectangle using specified arguments
            IntPtr hwnd = NativeMethods.FindWindow(lpClassName, lpWindowName);
            RECT rect = new RECT();
            NativeMethods.GetWindowRect(hwnd, out rect);

            // check if handle is nothing
            if (hwnd == IntPtr.Zero)
            {
                // log that we didn't find a window
                Log.Debug("Failed to find window with specified arguments!");

                return IntPtr.Zero;
            }

            // log what we found
            Log.Verbose(string.Format("Found window [Handle={0},Rectangle={1}]", hwnd.ToString(), rect.ToString()));

            if (rect.Size.Width >= width && rect.Size.Height >= height)
            {
                Log.Debug("Correct window handle found!");
                
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
                        Log.Debug("Correct window handle found!");

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

        private void LoLAutoLogin_FormClosing(object sender, FormClosingEventArgs e)
        {

            notifyIcon.Visible = false;
            notifyIcon.Dispose();

        }

        /// <summary>
        /// Runs all the logic necessary to enter the password automatically into the new League Client Alpha Update
        /// </summary>
        /// <returns></returns>
        private async Task RunAlphaClient()
        {

            // hide window
            this.Hide();

            // create progress interface
            IProgress<object[]> progress = new Progress<object[]>((s) =>
            {

                // show tooltip, use object array because i'm lazy
                notifyIcon.ShowBalloonTip(2500, (string)s[0], (string)s[1], (ToolTipIcon)s[2]);

            });

            await Task.Factory.StartNew(() =>
            {

                // create handle variable
                IntPtr clientHandle;

                // check if client is already running & window is present
                if (Process.GetProcessesByName("LeagueClient").Length > 0 && (clientHandle = GetSingleWindowFromSize("RCLIENT", null, 1200, 700)) != IntPtr.Zero)
                {
                    // log
                    Log.Info("Client is already open!");

                    // check if password box is visible (not logged in)
                    if (PasswordBoxIsVisible(clientHandle))
                    {
                        // log
                        Log.Info("Client is open on login page, entering password.");

                        // client is on login page, enter password
                        EnterAlphaPassword(clientHandle, progress);
                    }
                    else
                    {
                        // log
                        Log.Info("Client is open and logged in, focusing window.");

                        // client is logged in, show window
                        NativeMethods.SetForegroundWindow(clientHandle);
                    }

                }
                else
                {

                    // log
                    Log.Info("Client is not running, launching client.");

                    // check if client exe exists
                    if (File.Exists("LeagueClient.exe"))
                    {

                        // launch client
                        Process.Start("LeagueClient.exe");

                        // create & start stopwatch
                        Stopwatch sw = new Stopwatch();
                        sw.Start();

                        // get client handle
                        clientHandle = AwaitClientHandle();
                        
                        // check if we got a valid handle
                        if (clientHandle != IntPtr.Zero)
                        {

                            // log
                            Log.Info("Client found after {0} ms!", sw.ElapsedMilliseconds);

                            // get password box
                            bool found = WaitForPasswordBox(progress);

                            // check if the password box was found
                            if (clientHandle != IntPtr.Zero && found)
                            {
                                
                                // log
                                Log.Info("Password box found after {0} ms!", sw.ElapsedMilliseconds);

                                // enter password
                                EnterAlphaPassword(clientHandle, progress);

                            }
                            else
                            {

                                // log
                                Log.Info("Client exited!");

                            }
                        }
                        else
                        {

                            // log
                            Log.Info("Client not found after {0} ms. Aborting operation.", clientTimeout);

                        }

                    }

                }

            });

            // done, exit application
            Application.Exit();

        }

        /// <summary>
        /// Hangs until the client window is found or the preset timeout is reached.
        /// </summary>
        /// <returns>Client window handle if found, zero if not.</returns>
        private IntPtr AwaitClientHandle()
        {

            // create & start stopwatch
            Stopwatch sw = new Stopwatch();
            sw.Start();

            // create client handle variable
            IntPtr clientHandle = IntPtr.Zero;

            // search for window until clientTimeout is reached or window is found
            do
                clientHandle = GetSingleWindowFromSize("RCLIENT", null, 1200, 700); // use null for window name since it might be translated
            while (sw.ElapsedMilliseconds < clientTimeout && clientHandle == IntPtr.Zero);

            // return found handle
            return clientHandle;

        }

        /// <summary>
        /// Hangs until the password box for the client is found or the client exits.
        /// </summary>
        /// <param name="progress">Progress interface used to pass messages</param>
        /// <returns>Whether the password box was found or not.</returns>
        private bool WaitForPasswordBox(IProgress<object[]> progress)
        {

            // create found & handle varables
            bool found = false;
            IntPtr clientHandle;

            // loop while not found and while client handle is something
            do
            {
               
                // get client handle
                clientHandle = GetSingleWindowFromSize("RCLIENT", null, 1200, 700);

                // additional check just in case
                if (clientHandle != IntPtr.Zero)
                {
                    
                    // this could fail so wrap in try/catch
                    try
                    {

                        // check if password box is visible
                        found = PasswordBoxIsVisible(clientHandle);

                    }
                    catch (Exception ex)
                    {

                        // print exception & stacktrace to log
                        Log.Fatal("Could not get client window image: " + ex.Message);
                        Log.PrintStackTrace(ex.StackTrace);

                        // show balloon tip to inform user of error
                        progress.Report(new object[] {
                            "LoL Auto Login has encountered a fatal error",
                            "Please check your logs for more information.",
                            ToolTipIcon.Error
                        });

                        // exit application
                        Application.Exit();
                        return false;

                    }

                    // sleep
                    Thread.Sleep(500);

                }

            }
            while (clientHandle != IntPtr.Zero && !found);

            // return whether client was found or not
            return found;

        }

        /// <summary>
        /// Checks to see if the password box is visible in the client's window.
        /// </summary>
        /// <param name="clientHandle">Handle of the client window</param>
        /// <returns>Whether the password box is visible or not.</returns>
        public bool PasswordBoxIsVisible(IntPtr clientHandle)
        {

            // check that the handle is valid
            if (clientHandle != IntPtr.Zero)
            {

                // get client window image
                Bitmap clientBitmap = new Bitmap(ScreenCapture.CaptureWindow(clientHandle));

                // get pixels
                Pixel px1 = new Pixel(new PixelCoord(0.914f, true), new PixelCoord(0.347f, true), Color.FromArgb(255, 0, 0, 0), Color.FromArgb(255, 5, 10, 10));
                Pixel px2 = new Pixel(new PixelCoord(0.914f, true), new PixelCoord(0.208f, true), Color.FromArgb(255, 0, 5, 15), Color.FromArgb(255, 5, 15, 25));

                // check if the password box is displayed
                bool found = px1.Match(clientBitmap) && px2.Match(clientBitmap);

                // dispose of image & collect garbage
                clientBitmap.Dispose();
                GC.Collect();

                return found;

            }

            // return false by default
            return false;

        }

        /// <summary>
        /// Enters the password into the client's password box.
        /// </summary>
        /// <param name="clientHandle">Handle of the client window</param>
        /// <param name="progress">Progress interface used to pass messages</param>
        public void EnterAlphaPassword(IntPtr clientHandle, IProgress<object[]> progress)
        {
            // set window to foreground
            NativeMethods.SetForegroundWindow(clientHandle);

            // create password string
            string password;

            // try to read password from file
            try
            {

                // create file stream
                using (FileStream file = new FileStream("password", FileMode.Open, FileAccess.Read))
                {

                    // read bytes
                    byte[] buffer = new byte[file.Length];
                    file.Read(buffer, 0, (int)file.Length);

                    // decrypt password
                    password = Encryption.Decrypt(buffer);

                }

            }
            catch (Exception ex)
            {
                // print exception & stacktrace to log
                Log.Fatal("Password file could not be read: " + ex.Message);
                Log.PrintStackTrace(ex.StackTrace);

                // show balloon tip to inform user of error
                progress.Report(new object[] {
                                "LoL Auto Login has encountered a fatal error",
                                "Please check your logs for more information.",
                                ToolTipIcon.Error
                            });

                // exit application
                Application.Exit();
                return;
            }

            // create character array from password
            char[] passArray = password.ToCharArray();

            // log
            Log.Info("Entering password...");

            int i = 0;
            RECT rect;
            InputSimulator sim = new InputSimulator();

            // enter password one character at a time
            while (i <= passArray.Length && clientHandle != IntPtr.Zero)
            {

                // get window rectangle, in case it is resized or moved
                NativeMethods.GetWindowRect(clientHandle, out rect);
                Log.Verbose("Client rectangle=" + rect.ToString());

                // move cursor above password box
                sim.Mouse.LeftButtonUp();
                NativeMethods.SetForegroundWindow(clientHandle);
                Cursor.Position = new Point(rect.Left + (int)(rect.Width * 0.914f), rect.Top + (int)(rect.Height * 0.347f));

                // focus window & click on password box
                sim.Mouse.LeftButtonClick();

                // check if client is foreground window
                if (NativeMethods.GetForegroundWindow() == clientHandle)
                {
                    
                    // enter password character, press enter if complete
                    if (i != passArray.Length)
                    {
                        
                        // go to end of text box
                        sim.Keyboard.KeyPress(VirtualKeyCode.END);

                        // enter character
                        sim.Keyboard.TextEntry(passArray[i].ToString());

                    }
                    else
                    {

                        // press enter
                        sim.Keyboard.KeyPress(VirtualKeyCode.RETURN);

                    }

                    // increment counter
                    i++;

                }

                // get the client handle again
                clientHandle = GetSingleWindowFromSize("RCLIENT", null, 1200, 700);

            }

        }

    }

}
