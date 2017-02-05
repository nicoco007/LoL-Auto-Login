using Microsoft;
using System;
using System.Collections.Generic;
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

/// Copyright © 2015-2017 nicoco007
///
/// Licensed under the Apache License, Version 2.0 (the "License");
/// you may not use this file except in compliance with the License.
/// You may obtain a copy of the License at
///
///     http://www.apache.org/licenses/LICENSE-2.0
///
/// Unless required by applicable law or agreed to in writing, software
/// distributed under the License is distributed on an "AS IS" BASIS,
/// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
/// See the License for the specific language governing permissions and
/// limitations under the License.
namespace LoLAutoLogin
{

    public partial class MainForm : Form
    {

        // how long to wait until we decide the client isnt starting
        private const int ClientTimeout = 30000;

        // debug info
        private List<ClientWindowMatch> windowsFound = new List<ClientWindowMatch>();
        private int comparisonResolution = 32;
        private int minPixelsMatched = int.MaxValue;
        private int maxPixelsMatched = int.MinValue;

        public MainForm()
        {
            // init
            InitializeComponent();

            // create notification icon context menu (so user can exit if program hangs)
            var menu = new ContextMenu();
            var item = new MenuItem("&Exit", (sender, e) => Application.Exit());
            menu.MenuItems.Add(item);
            notifyIcon.ContextMenu = menu;

            // set accept button (will be activated when the 'enter' key is pressed)
            AcceptButton = saveButton;

            // hide
            Opacity = 0.0f;
            ShowInTaskbar = false;
        }

        private async void MainForm_Load(object sender, EventArgs e)
        {
            // start logging
            Log.Info("Started LoL Auto Login v{0}", Assembly.GetEntryAssembly().GetName().Version);

            // check if a Shift key is being pressed
            if (NativeMethods.GetAsyncKeyState(Keys.RShiftKey) != 0 || NativeMethods.GetAsyncKeyState(Keys.LShiftKey) != 0)
            {
                // log
                Log.Info("Shift key is being pressed!");

                // check if file exists
                if (CheckLocation())
                {
                    // log
                    Log.Info("Starting client without running LoL Auto Login.");

                    // run client
                    StartClient();

                    // exit
                    Application.Exit();
                    return;
                }
            }

            // if the client exe is not found, exit
            if (!CheckLocation()) return;

            // check if the password exists
            if (PasswordExists())
            {
                // run client if it does
                await RunClient();
            }
            else
            {
                // show window if it doesn't
                Opacity = 1.0f;
                ShowInTaskbar = true;
                Log.Info("Password file not found, prompting user to enter password...");
            }
        }

        private bool CheckLocation()
        {
            // check if program is in same directory as league of legends
            if (File.Exists("LeagueClient.exe")) return true;

            Log.Fatal("Launcher executable not found!");

            // show error message
            MessageBox.Show(this, "Please place LoL Auto Login in your League of Legends directory (beside the \"LeagueClient.exe file\").", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);

            // exit application
            Application.Exit();

            // return so no other commands are executed
            return false;
        }

        private bool PasswordExists()
        {
            if (!File.Exists("password")) return false;

            using (var reader = new StreamReader("password"))
            {
                if (!Regex.IsMatch(reader.ReadToEnd(), @"^[a-zA-Z0-9\+\/]*={0,3}$")) return true;

                Log.Info("Password is old format, prompting user to enter password again...");
                MessageBox.Show(@"Password encryption has been changed. You will be prompted to enter your password once again.", @"LoL Auto Login - Encryption method changed", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            return false;
        }

        private bool StartClient()
        {
            // try launching league of legends
            try
            {
                Process.Start("LeagueClient.exe");
                return true;
            }
            catch (Exception ex)
            {
                // print error to log and show balloon tip to inform user of fatal error
                Log.Fatal("Could not start League of Legends!");
                Log.PrintStackTrace(ex.StackTrace);

                Invoke(new Action(() => {
                    notifyIcon.ShowBalloonTip(2500, "LoL Auto Login was unable to start League of Legends. Please check your logs for more information.", "LoL Auto Login has encountered a fatal error", ToolTipIcon.Error);
                }));

                // exit application
                Application.Exit();
                return false;
            }
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
                using (var file = new FileStream("password", FileMode.OpenOrCreate, FileAccess.Write))
                {
                    var data = Encryption.Encrypt(passTextBox.Text);
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

            Hide();

            // run the client!
            await RunClient();
        }

        /// <summary>
        /// Gets a window with specified size using class name and window name.
        /// </summary>
        /// <param name="lpClassName">Class name</param>
        /// <param name="lpWindowName">Window name</param>
        /// <param name="width">Window minimum width</param>
        /// <param name="height">Window minimum height</param>
        /// <returns>The specified window's handle</returns>
        public IntPtr GetSingleWindowFromSize(string lpClassName, string lpWindowName, int width, int height)
        {
            // log what we are looking for
            Log.Debug($"Trying to find window handle [ClassName={(lpClassName ?? "null")},WindowName={(lpWindowName ?? "null")},Size={new Size(width, height)}]");

            // try to get window handle and rectangle using specified arguments
            var hwnd = NativeMethods.FindWindow(lpClassName, lpWindowName);
            RECT rect;
            NativeMethods.GetWindowRect(hwnd, out rect);

            // check if handle is nothing
            if (hwnd == IntPtr.Zero)
            {
                // log that we didn't find a window
                Log.Debug("Failed to find window with specified arguments!");

                return IntPtr.Zero;
            }

            // log what we found
            Log.Verbose($"Found window [Handle={hwnd},Rectangle={rect}]");

            if (rect.Size.Width >= width && rect.Size.Height >= height)
            {
                Log.Debug("Correct window handle found!");

                AddFoundWindow(hwnd, rect, lpClassName, lpWindowName);

                return hwnd;
            }

            while (NativeMethods.FindWindowEx(IntPtr.Zero, hwnd, lpClassName, lpWindowName) != IntPtr.Zero)
            {
                hwnd = NativeMethods.FindWindowEx(IntPtr.Zero, hwnd, lpClassName, lpWindowName);
                NativeMethods.GetWindowRect(hwnd, out rect);

                Log.Verbose($"Found window [Handle={hwnd},Rectangle={rect}]");

                if (rect.Size.Width < width || rect.Size.Height < height) continue;

                Log.Debug("Correct window handle found!");

                AddFoundWindow(hwnd, rect, lpClassName, lpWindowName);

                return hwnd;
            }

            return IntPtr.Zero;
        }

        /// <summary>
        /// Add a window handle to the found windows list, and log it if it is new.
        /// </summary>
        /// <param name="handle">Window handle</param>
        /// <param name="rect">Window rectangle</param>
        /// <param name="className">Window class name (optional)</param>
        /// <param name="name">Window name/text (optional)</param>
        public void AddFoundWindow(IntPtr handle, Rectangle rect, string className = null, string name = null)
        {
            // check if class name is defined
            if (string.IsNullOrEmpty(className))
            {
                // create stringbuilder
                StringBuilder sb = new StringBuilder(255);

                // get class name
                NativeMethods.GetClassName(handle, sb, sb.MaxCapacity);

                // set class name to stringbuilder text
                className = sb.ToString();
            }

            // check if window name/text is set
            if (string.IsNullOrEmpty(name))
            {
                // create stringbuilder
                StringBuilder sb = new StringBuilder(255);

                // get window name/text
                NativeMethods.GetWindowText(handle, sb, sb.MaxCapacity);

                // set window name/text
                name = sb.ToString();
            }

            // create instance of ClientWindowMatch with specified info
            ClientWindowMatch window = new ClientWindowMatch(handle, name, className, rect);
            
            // check if window is not already in list
            if (!windowsFound.Contains(window))
            {
                // log
                Log.Info("Found new/resized window: " + window);

                // add window to list
                windowsFound.Add(window);
            }
        }

        /// <summary>
        /// Kills every process with the specified name.
        /// </summary>
        /// <param name="pName">Name of process(es) to kill</param>
        public void KillProcessesByName(string pName)
        {
            Log.Verbose($"Killing all {pName} processes.");
            foreach (var p in Process.GetProcessesByName(pName)) p.Kill();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // hide & dispose of taskbar icon
            notifyIcon.Visible = false;
            notifyIcon.Dispose();
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            Log.Info("Pixel matching statistics:");
            Log.Info($"    Minimum: {minPixelsMatched / Math.Pow(comparisonResolution, 2):0.00000} ({minPixelsMatched}/{Math.Pow(comparisonResolution, 2)})");
            Log.Info($"    Maximum: {maxPixelsMatched / Math.Pow(comparisonResolution, 2):0.00000} ({maxPixelsMatched}/{Math.Pow(comparisonResolution, 2)})");

            Log.Info("Shutting down");
        }

        /// <summary>
        /// Runs all the logic necessary to enter the password automatically into the League Client
        /// </summary>
        /// <returns></returns>
        private async Task RunClient()
        {

            // hide window
            Hide();

            // create progress interface
            IProgress<ShowBalloonTipEventArgs> showBalloonTip = new Progress<ShowBalloonTipEventArgs>((e) =>
            {
                // show tooltip
                notifyIcon.ShowBalloonTip(2500, e.Title, e.Message, e.Icon);
            });

            await Task.Factory.StartNew(() =>
            {
                // create handle variable
                IntPtr clientHandle;

                // check if client is already running & window is present
                if (Process.GetProcessesByName("LeagueClient").Length > 0 && (clientHandle = GetClientWindowHandle()) != IntPtr.Zero)
                {
                    // log
                    Log.Info("Client is already open!");

                    // check if password box is visible (not logged in)
                    if (PasswordBoxIsVisible(clientHandle))
                    {
                        // log
                        Log.Info("Client is open on login page, entering password.");

                        // client is on login page, enter password
                        EnterPassword(clientHandle, showBalloonTip);
                    }
                    else
                    {
                        // log
                        Log.Info("Client doesn't seem to be on login page. Focusing client.");

                        // client is logged in, show window
                        NativeMethods.SetForegroundWindow(clientHandle);
                    }
                }
                else
                {
                    // log
                    Log.Info("Client is not running, launching client.");

                    // check if client exe exists & start client
                    if (CheckLocation() && StartClient())
                    {
                        // create & start stopwatch
                        var sw = new Stopwatch();
                        sw.Start();

                        // get client handle
                        clientHandle = AwaitClientHandle();

                        // check if we got a valid handle
                        if (clientHandle != IntPtr.Zero)
                        {
                            // log
                            Log.Info($"Client found after {sw.ElapsedMilliseconds} ms!");

                            // get password box
                            var found = WaitForPasswordBox(showBalloonTip);

                            // check if the password box was found
                            if (clientHandle != IntPtr.Zero && found)
                            {
                                // log
                                Log.Info($"Password box found after {sw.ElapsedMilliseconds} ms!");

                                // enter password
                                EnterPassword(clientHandle, showBalloonTip);
                            }
                            else
                            {
                                // log
                                Log.Info("Client window lost!");
                            }
                        }
                        else
                        {
                            // log
                            Log.Info($"Client not found after {ClientTimeout} ms. Aborting operation.");
                        }
                    }
                    else
                    {
                        // log
                        Log.Info("Failed to launch client.");
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
            var sw = new Stopwatch();
            sw.Start();

            // create client handle variable
            IntPtr clientHandle;

            // search for window until client timeout is reached or window is found
            do
                clientHandle = GetClientWindowHandle();
            while (sw.ElapsedMilliseconds < ClientTimeout && clientHandle == IntPtr.Zero);

            // return found handle
            return clientHandle;
        }

        /// <summary>
        /// Hangs until the password box for the client is found or the client exits.
        /// </summary>
        /// <param name="progress">Progress interface used to pass messages</param>
        /// <returns>Whether the password box was found or not.</returns>
        private bool WaitForPasswordBox(IProgress<ShowBalloonTipEventArgs> progress)
        {

            // create found & handle varables
            var found = false;
            IntPtr clientHandle;

            // loop while not found and while client handle is something
            do
            {
                // get client handle
                clientHandle = GetClientWindowHandle();

                // additional check just in case
                if (clientHandle == IntPtr.Zero) continue;

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
                    progress.Report(new ShowBalloonTipEventArgs(
                        "LoL Auto Login has encountered a fatal error",
                        "Please check your logs for more information.",
                        ToolTipIcon.Error
                    ));

                    // exit application
                    Application.Exit();
                    return false;

                }

                // sleep
                Thread.Sleep(500);
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
            if (clientHandle == IntPtr.Zero) return false;

            // get client window image
            var clientBitmap = new Bitmap(ScreenCapture.CaptureWindow(clientHandle));
            
            // get reference image
            Bitmap reference = Properties.Resources.reference_login;

            // get login sidebar thing
            Bitmap cropped = CropImage(clientBitmap, new Rectangle((int)(clientBitmap.Width * 0.825), 0, (int)(clientBitmap.Width * 0.175), clientBitmap.Height));

            // compare the images
            var found = CompareImage(reference, cropped);

            // dispose of bitmaps
            reference.Dispose();
            cropped.Dispose();

            // force garbage collection
            GC.Collect();

            // return
            return found;
        }

        /// <summary>
        /// Generates a "hash" composed of brightness values of a resized version of the image
        /// </summary>
        /// <param name="source">Source bitmap</param>
        /// <returns></returns>
        public List<short> GetHash(Bitmap source)
        {
            // create list
            List<short> list = new List<short>();

            // resize bitmap
            Bitmap resized = new Bitmap(source, new Size(comparisonResolution, comparisonResolution));

            // iterate through every pixel
            for (int i = 0; i < resized.Width; i++)
            {
                for (int j = 0; j < resized.Height; j++)
                {
                    // get brightness
                    float b = resized.GetPixel(i, j).GetBrightness();

                    // convert brightness 0-1 to 0-255 value
                    list.Add((short) (b * 255));
                }
            }

            // return brightness list
            return list;
        }

        /// <summary>
        /// Compares two images using their hashes & supplied tolerance
        /// </summary>
        /// <param name="a">First image</param>
        /// <param name="b">Second image</param>
        /// <param name="brightnessTolerance">Tolerance, from 0 to 255, when comparing the brightness of pixels</param>
        /// <param name="matchTolerance">Percent tolerance of similar pixel count versus total pixel count</param>
        /// <returns>Whether the images are similar or not</returns>
        public bool CompareImage(Bitmap a, Bitmap b, double brightnessTolerance = 10, double matchTolerance = 0.85)
        {
            // get hashes
            List<short> hashA = GetHash(a);
            List<short> hashB = GetHash(b);

            // get amount of similar pixels
            int similar = hashA.Zip(hashB, (i, j) => Math.Abs(i - j) < brightnessTolerance).Count(sim => sim);

            // log
            Log.Debug("Comparison: " + similar + "/" + hashA.Count);

            if (similar > maxPixelsMatched)
                maxPixelsMatched = similar;

            if (similar < minPixelsMatched)
                minPixelsMatched = similar;

            // return true if the amount of similar pixels is over tolerance, false if not
            return similar > hashA.Count * matchTolerance;
        }

        /// <summary>
        /// Checks if the value is between the specified minimum and maximum
        /// </summary>
        /// <param name="val">Value to check</param>
        /// <param name="min">Minimum</param>
        /// <param name="max">Maximum</param>
        /// <returns>Whether the value is between the specified minimum and maximum or not</returns>
        public bool InRange(double val, double min, double max)
        {
            // check if min/max values make sense
            if (min > max)
                throw new ArgumentException("Minimum must be smaller than maximum.");

            // return if val is between min and max
            return val >= min && val <= max;
        }
        
        /// <summary>
        /// Crop an image.
        /// </summary>
        /// <param name="bitmap">Bitmap to crop</param>
        /// <param name="rect">Crop rectangle</param>
        /// <returns>Cropped image</returns>
        private Bitmap CropImage(Bitmap bitmap, Rectangle rect)
        {
            // create bitmap
            Bitmap cropped = new Bitmap(rect.Width, rect.Height);

            // draw image on cropped bitmap
            using (Graphics g = Graphics.FromImage(cropped))
                g.DrawImage(bitmap, -rect.X, -rect.Y);

            // return cropped bitmap
            return cropped;
        }

        /// <summary>
        /// Enters the password into the client's password box.
        /// </summary>
        /// <param name="clientHandle">Handle of the client window</param>
        /// <param name="progress">Progress interface used to pass messages</param>
        public void EnterPassword(IntPtr clientHandle, IProgress<ShowBalloonTipEventArgs> progress)
        {
            // set window to foreground
            NativeMethods.SetForegroundWindow(clientHandle);

            // create password string
            string password;

            // try to read password from file
            try
            {

                // create file stream
                using (var file = new FileStream("password", FileMode.Open, FileAccess.Read))
                {
                    // read bytes
                    var buffer = new byte[file.Length];
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
                progress.Report(new ShowBalloonTipEventArgs(
                                "LoL Auto Login has encountered a fatal error",
                                "Please check your logs for more information.",
                                ToolTipIcon.Error
                            ));

                // exit application
                Application.Exit();
                return;
            }

            // create character array from password
            var passArray = password.ToCharArray();

            // log
            Log.Info("Entering password...");
            
            // create input simulator instance
            var sim = new InputSimulator();

            // enter password one character at a time, with one additional iteration for pressing the enter key
            for (int i = 0; (i <= passArray.Length) && (clientHandle != IntPtr.Zero); i++)
            {
                // get window rectangle, in case it is resized or moved
                RECT rect;
                NativeMethods.GetWindowRect(clientHandle, out rect);
                AddFoundWindow(clientHandle, rect);
                Log.Verbose("Client rectangle=" + rect.ToString());

                // move cursor above password box
                sim.Mouse.LeftButtonUp();
                NativeMethods.SetForegroundWindow(clientHandle);
                Cursor.Position = new Point(rect.Left + (int)(rect.Width * 0.914f), rect.Top + (int)(rect.Height * 0.347f));

                Log.Debug(Cursor.Position.ToString());

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
                }

                // get the client handle again
                clientHandle = GetClientWindowHandle();
            }
            
            // log
            Log.Info("Successfully entered password (well, hopefully)!");
        }

        /// <summary>
        /// Retrieves the handle of the League Client window.
        /// </summary>
        /// <returns>Handle of the client.</returns>
        public IntPtr GetClientWindowHandle() => GetSingleWindowFromSize("RCLIENT", null, 1000, 500);
    }

}
