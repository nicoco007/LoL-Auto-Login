using Microsoft;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;
using AutoIt;
using Emgu.CV;
using Emgu.CV.Structure;

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
        private int ClientTimeout = 30000;

        // debug info
        private List<ClientWindowMatch> windowsFound = new List<ClientWindowMatch>();

        // these values can be changed through the YAML config file
        private double MatchTolerance;
        
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

        private void LoadSettings()
        {
            // get config directory & settings file
            var dir = Path.Combine(Directory.GetCurrentDirectory(), "Config");
            var settingsFile = Path.Combine(dir, "LoLAutoLoginSettings.yaml");
            
            // make sure the config directory exists
            Directory.CreateDirectory(dir);

            // log
            Log.Info($"Loading settings from \"{settingsFile}\"");

            // define default settings
            var defaultSettings = new YamlMappingNode(
                new YamlScalarNode("settings"),
                new YamlMappingNode(
                    new YamlScalarNode("login-detection"),
                    new YamlMappingNode(
                        new YamlScalarNode("template-matching-tolerance"),
                        new YamlScalarNode("0.75")
                    ),
                    new YamlScalarNode("client-load-timeout"),
                    new YamlScalarNode("30")
                )
            );

            // create settings variable
            YamlMappingNode settings;

            // check if settings exist
            if (File.Exists(settingsFile))
            {
                try
                {
                    // load settings from yaml
                    var loadedSettings = ReadYaml<YamlMappingNode>(settingsFile);

                    // merge settings if not empty, use default if it is
                    if (loadedSettings != null)
                    {
                        // merge settings
                        settings = MergeMappingNodes(defaultSettings, loadedSettings, false);

                        // log
                        Log.Info("Loaded settings.");
                    }
                    else
                    {
                        // log
                        Log.Info("Settings file is empty, using default settings.");

                        // use default settings
                        settings = defaultSettings;
                    }
                }
                catch (Exception ex)
                {
                    // print error
                    Log.PrintException(ex);
                    Log.Warn("Failed to parse YAML, reverting to default settings.");
                    
                    // use default settings
                    settings = defaultSettings;
                }
            }
            else
            {
                // log
                Log.Info("Settings file does not exist, using default settings.");

                // use default settings
                settings = defaultSettings;
            }
            
            // wrap in try/catch in case there's a parsing error
            try
            {
                // set vars to loaded values
                MatchTolerance = double.Parse(((YamlScalarNode)settings["settings"]["login-detection"]["template-matching-tolerance"]).Value, CultureInfo.InvariantCulture);
                ClientTimeout = int.Parse(((YamlScalarNode)settings["settings"]["client-load-timeout"]).Value) * 1000;
            }
            catch (Exception ex)
            {
                Log.PrintException(ex);
                Log.Warn("Failed to parse YAML values, reverting to default settings.");

                // use default settings
                settings = defaultSettings;
            }

            // write yaml
            WriteYaml(settingsFile, settings);
        }

        private T ReadYaml<T>(string file)
        {
            T read = default(T);

            using (var reader = new StreamReader(file))
            {
                var deserializer = new Deserializer();
                var parser = new Parser(reader);
                read = deserializer.Deserialize<T>(parser);
            }

            return read;
        }

        private void WriteYaml<T>(string file, T yaml)
        {
            using (var writer = new StreamWriter(file))
            {
                var serializer = new SerializerBuilder().EnsureRoundtrip().Build();
                writer.Write(serializer.Serialize(yaml));
            }
        }
        
        private YamlMappingNode MergeMappingNodes(YamlMappingNode a, YamlMappingNode b, bool mergeNonSharedValues = true)
        {
            // create merged values node
            YamlMappingNode merged = new YamlMappingNode();

            // iterate through a's items
            foreach (var item in a.Children)
            {
                // check if b contains this item
                if (b.Children.ContainsKey(item.Key))
                {
                    // if both values are mapping nodes, add merged mapped nodes; if not, add b's value
                    if (item.Value is YamlMappingNode && b[item.Key] is YamlMappingNode)
                        merged.Children.Add(item.Key, MergeMappingNodes((YamlMappingNode)item.Value, (YamlMappingNode)b[item.Key], mergeNonSharedValues));
                    else
                        merged.Children.Add(item.Key, b[item.Key]);
                }
                else
                {
                    // add item to merged
                    merged.Children.Add(item);
                }
            }

            // if we want to merged non-shared values, add all of b's children that aren't already in merged
            if (mergeNonSharedValues)
                foreach (var item in b.Children)
                    if (!merged.Children.ContainsKey(item.Key))
                        merged.Children.Add(item);

            // log loaded values to verbose
            foreach (var item in merged)
                if (item.Key is YamlScalarNode && item.Value is YamlScalarNode)
                    Log.Verbose("{0} = {1}", ((YamlScalarNode)item.Key).Value, ((YamlScalarNode)item.Value).Value);

            // return merged values
            return merged;
        }

        private async void MainForm_Load(object sender, EventArgs e)
        {
            // start logging
            Log.Info("Started LoL Auto Login v{0}", Assembly.GetEntryAssembly().GetName().Version);

            // load settings
            LoadSettings();

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
            Log.Debug($"Trying to find window handle {{ClassName={(lpClassName ?? "null")},WindowName={(lpWindowName ?? "null")},Size={new Size(width, height)}}}");

            // try to get window handle and rectangle using specified arguments
            var hwnd = NativeMethods.FindWindow(lpClassName, lpWindowName);
            RECT rect;
            NativeMethods.GetWindowRect(hwnd, out rect);

            // check if handle is nothing
            if (hwnd == IntPtr.Zero)
            {
                // log that we didn't find a window
                Log.Verbose("Failed to find window with specified arguments!");

                return IntPtr.Zero;
            }

            // log what we found
            Log.Verbose($"Found window {{Handle={hwnd},Rectangle={rect}}}");

            if (rect.Size.Width >= width && rect.Size.Height >= height)
            {
                Log.Verbose("Correct window handle found!");

                AddFoundWindow(hwnd, rect, lpClassName, lpWindowName);

                return hwnd;
            }

            while (NativeMethods.FindWindowEx(IntPtr.Zero, hwnd, lpClassName, lpWindowName) != IntPtr.Zero)
            {
                hwnd = NativeMethods.FindWindowEx(IntPtr.Zero, hwnd, lpClassName, lpWindowName);
                NativeMethods.GetWindowRect(hwnd, out rect);

                Log.Verbose($"Found window {{Handle={hwnd},Rectangle={rect}}}");

                if (rect.Size.Width < width || rect.Size.Height < height) continue;

                Log.Verbose("Correct window handle found!");

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

            string logsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Logs", "LoL Auto Login Logs");

            if (Directory.Exists(logsDirectory))
            {
                FileInfo[] logFiles = new DirectoryInfo(logsDirectory).GetFiles().OrderByDescending(f => f.LastWriteTime).ToArray();

                if (logFiles.Length > 50)
                {
                    Log.Info($"Deleting {logFiles.Length - 50} old log files...");

                    foreach (FileInfo logFile in logFiles.Skip(50))
                    {
                        try
                        {
                            logFile.Delete();
                        }
                        catch (Exception ex)
                        {
                            Log.Error($"Failed to delete log file \"{logFile.Name}\": {ex.GetType()} - {ex.Message}");
                        }
                    }

                    Log.Info("Done.");
                }
            }
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
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
            IProgress<ShowBalloonTipEventArgs> showBalloonTip = new System.Progress<ShowBalloonTipEventArgs>((e) =>
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

                    var passwordRect = GetPasswordRect(clientHandle);

                    // check if password box is visible (not logged in)
                    if (passwordRect != Rectangle.Empty)
                    {
                        // log
                        Log.Info("Client is open on login page, entering password.");

                        // client is on login page, enter password
                        EnterPassword(clientHandle, passwordRect, showBalloonTip);
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
                    Log.Info($"Waiting for {ClientTimeout} ms.");

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
                            if (clientHandle != IntPtr.Zero && found != Rectangle.Empty)
                            {
                                // log
                                Log.Info($"Password box found after {sw.ElapsedMilliseconds} ms!");

                                // enter password
                                EnterPassword(clientHandle, found, showBalloonTip);
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
        private Rectangle WaitForPasswordBox(IProgress<ShowBalloonTipEventArgs> progress)
        {

            // create found & handle varables
            Rectangle found = Rectangle.Empty;
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
                    found = GetPasswordRect(clientHandle);
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
                    return Rectangle.Empty;

                }

                // sleep
                Thread.Sleep(500);
            }
            while (clientHandle != IntPtr.Zero && found == Rectangle.Empty);

            // return whether client was found or not
            return found;

        }

        /// <summary>
        /// Checks to see if the password box is visible in the client's window.
        /// </summary>
        /// <param name="clientHandle">Handle of the client window</param>
        /// <returns>Whether the password box is visible or not.</returns>
        public Rectangle GetPasswordRect(IntPtr clientHandle)
        {
            // check that the handle is valid
            if (clientHandle == IntPtr.Zero)
                return Rectangle.Empty;

            // get client window image
            var source = new Image<Bgr, byte>(new Bitmap(ScreenCapture.CaptureWindow(clientHandle)));
            var template = new Image<Bgr, byte>(Properties.Resources.template);

            // compare the images
            var found = CompareImage(source, template, MatchTolerance);
            
            source.Dispose();
            template.Dispose();

            // force garbage collection
            GC.Collect();

            // return
            return found;
        }

        /// <summary>
        /// Compares two images using their hashes & supplied tolerance
        /// </summary>
        /// <param name="a">First image</param>
        /// <param name="b">Second image</param>
        /// <param name="matchTolerance">Percent tolerance of similar pixel count versus total pixel count</param>
        /// <returns>Whether the images are similar or not</returns>
        public Rectangle CompareImage(Image<Bgr, byte> source, Image<Bgr, byte> template, double matchTolerance = 0.80)
        {
            using (Image<Gray, float> result = source.MatchTemplate(template, Emgu.CV.CvEnum.TemplateMatchingType.CcoeffNormed))
            {
                double[] minValues, maxValues;
                Point[] minLocations, maxLocations;

                result.MinMax(out minValues, out maxValues, out minLocations, out maxLocations);
                
                Log.Info($"Template matching: needed {matchTolerance}, got {maxValues[0]}");

                if (maxValues[0] > matchTolerance)
                {
                    return new Rectangle(maxLocations[0], template.Size);
                }
            }

            return Rectangle.Empty;
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
        /// Enters the password into the client's password box.
        /// </summary>
        /// <param name="clientHandle">Handle of the client window</param>
        /// <param name="progress">Progress interface used to pass messages</param>
        public void EnterPassword(IntPtr clientHandle, Rectangle passwordRect, IProgress<ShowBalloonTipEventArgs> progress)
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

            // enter password one character at a time
            for (int i = 0; i <= passArray.Length && clientHandle != IntPtr.Zero; i++)
            {
                // get window rectangle, in case it is resized or moved
                RECT rect;
                NativeMethods.GetWindowRect(clientHandle, out rect);
                AddFoundWindow(clientHandle, rect);
                Log.Verbose("Client rectangle=" + rect.ToString());

                // move cursor above password box
                AutoItX.MouseUp("primary");
                NativeMethods.SetForegroundWindow(clientHandle);

                // focus window & click on password box
                AutoItX.MouseClick("primary", rect.Left + passwordRect.Left + passwordRect.Width / 2, rect.Top + passwordRect.Top + passwordRect.Height / 2, 1, 0);

                // check if client is foreground window
                if (NativeMethods.GetForegroundWindow() == clientHandle)
                {
                    // enter password character, press enter if complete
                    if (i < passArray.Length)
                    {
                        // enter character
                        AutoItX.ControlSend(clientHandle, IntPtr.Zero, string.Format("{{END}}{{ASC {0:000}}}", (int)passArray[i]), 0);
                    }
                    else
                    {
                        // press enter
                        AutoItX.ControlSend(clientHandle, IntPtr.Zero, "{ENTER}", 0);
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
