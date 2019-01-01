// Copyright © 2015-2018 Nicolas Gnyra

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

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LoLAutoLogin
{
    internal static class Program
    {
        public static Version Version;
        
        private static ManualResetEvent resetEvent = new ManualResetEvent(true);
        private static NotifyIcon notifyIcon;
        private static ShowBalloonTipEventArgs latestBalloonTip;

        /// <summary>
        /// Point d'entrée principal de l'application.
        /// </summary>
        private static int Main()
        {
            LoadSettings();

            // start logging
            Logger.Info("Started LoL Auto Login v" + Version);

            new Thread(() => UIThread()).Start();

            // run on our own thread since the async functions of WebClient kill threads when program exits
            if (Config.GetBooleanValue("check-for-updates", true))
                new Thread(CheckLatestVersion).Start();

            Run().Wait();

            resetEvent.WaitOne();

            Shutdown();

            return 0;
        }

        private static void CheckLatestVersion()
        {
            Logger.Info("Checking for updates");

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            
            try
            {
                using (WebClient updateClient = new WebClient())
                {
                    updateClient.Headers.Add("User-Agent", "LoL Auto Login v" + Version);
                    string result = updateClient.DownloadString(new Uri("https://api.github.com/repos/nicoco007/lol-auto-login/releases/latest"));
                    
                    JObject obj = JsonConvert.DeserializeObject<JObject>(result);
                    var stringValue = obj["tag_name"].Value<string>();

                    if (string.IsNullOrEmpty(stringValue))
                    {
                        Logger.Error("Failed to get \"tag_name\" value from API response");
                        return;
                    }

                    Match match = Regex.Match(stringValue, @"[0-9]+(?:\.[0-9]+){0,3}"); // extract version from tag name

                    if (!match.Success)
                    {
                        Logger.Error($"Failed to parse {stringValue} as a valid version");
                        return;
                    }

                    var latestVersion = new Version(match.Value);

                    if (latestVersion > Version)
                    {
                        Logger.Info("New version available: " + latestVersion);

                        DialogResult dialogResult = MessageBox.Show("A new version of LoL Auto Login is available! Would you like to download it?", "New Version Available", MessageBoxButtons.YesNo, MessageBoxIcon.Information);

                        if (dialogResult == DialogResult.Yes)
                        {
                            Process.Start("https://go.nicoco007.com/link/9f45794c-a0b7-49d2-9ae1-85e37c0b42e2");
                            return;
                        }

                        dialogResult = MessageBox.Show("Would you like to disable automatic update checking?", "Disable Automatic Updates", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                        if (dialogResult == DialogResult.Yes)
                        {
                            Config.SetValue("check-for-updates", false);
                            MessageBox.Show("Automatic update checking has been disabled. You can re-enable updates by editing the configuration file.", "Updates Disabled", MessageBoxButtons.OK, MessageBoxIcon.Information); // TODO: this should be done through the interface
                        }
                    }
                    else
                    {
                        Logger.Info($"Newer version not found (latest is {latestVersion})");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.PrintException("Failed to get latest version", ex);
            }
        }

        private static async Task Run()
        {
            if (Config.GetBooleanValue("login-detection.debug", false))
            {
                if (!Directory.Exists(Folders.Debug))
                    Directory.CreateDirectory(Folders.Debug);

                Logger.Debug("Cleaning debug directory");

                try
                {
                    foreach (string file in Directory.EnumerateFiles(Folders.Debug))
                        File.Delete(file);
                }
                catch (Exception ex)
                {
                    Logger.PrintException("Failed to clean debug directory", ex);
                }
            }
            else if (Directory.Exists(Folders.Debug))
            {
                try
                {
                    Directory.Delete(Folders.Debug, true);
                }
                catch (Exception ex)
                {
                    Logger.PrintException("Failed to delete debug directory", ex);
                }
            }

            ShowSystemInfo();

            if (!IsCorrectLocation())
                return;

            // check if a Shift key is being pressed
            if (NativeMethods.GetAsyncKeyState(Keys.RShiftKey) != 0 || NativeMethods.GetAsyncKeyState(Keys.LShiftKey) != 0)
            {
                Logger.Info("Shift key is being pressed - starting client without running LoL Auto Login");

                // try launching league of legends
                try
                {
                    ClientControl.StartClient();
                }
                catch (Exception ex)
                {
                    FatalError("Could not start League of Legends!", ex);
                }

                return;
            }

            if (!PasswordExists())
            {
                Logger.Info("Password file not found, prompting user to enter password");

                var form = new MainForm();
                form.ShowDialog();

                if (form.Success != true)
                    return;

                Config.SetValue("check-for-updates", form.CheckForUpdates);
            }

            try
            {
                await ClientControl.RunLogin();
            }
            catch (Exception ex)
            {
                FatalError("Could not start League of Legends!", ex);
            }
        }

        [STAThread]
        private static void UIThread()
        {
            Logger.Debug("Created UI thread");

            NativeMethods.SetProcessDPIAware();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            LoadNotifyIcon();

            Application.Run(); // start message pump
        }

        private static void LoadNotifyIcon()
        {
            notifyIcon = new NotifyIcon
            {
                Icon = Properties.Resources.Icon,
                Visible = true,
                Text = "LoL Auto Login"
            };

            var menu = new ContextMenu();

            var item = new MenuItem("&Exit", (sender, e) => ForceShutdown());
            menu.MenuItems.Add(item);

            notifyIcon.ContextMenu = menu;

            notifyIcon.BalloonTipClicked += NotifyIcon_BalloonTipClicked;
            notifyIcon.BalloonTipClosed += NotifyIcon_BalloonTipClosed;
        }

        internal static void SetNotifyIconText(string status)
        {
            notifyIcon.Text = "LoL Auto Login";

            if (!string.IsNullOrEmpty(status))
                notifyIcon.Text += " – " + status;
        }

        private static void ShowSystemInfo()
        {
            Logger.Info($"OS Version: " + Util.GetFriendlyOSVersion());

            foreach (Screen screen in Screen.AllScreens)
                Logger.Info(string.Format("Screen {0}: {1}", screen.DeviceName, screen.Bounds));

            string fileName = Path.Combine(Folders.Configuration, "LeagueClientSettings.yaml");
            ClientSettings info;

            if (File.Exists(fileName))
            {
                try
                {
                    info = ClientSettings.FromFile(fileName);

                    Logger.Info($"Region: {info.Region}; Locale: {info.Locale}");

                    if (!info.RemembersUsername)
                        Logger.Warn("Client isn't configured to remember username");
                }
                catch (Exception ex)
                {
                    Logger.PrintException("Failed to load client settings", ex);
                }
            }
            else
            {
                Logger.Info($"Client configuration not found at \"{fileName}\"");
            }
        }

        private static void LoadSettings()
        {
            Logger.Debug("Loading settings");

            Logger.Setup();

            Version = Assembly.GetExecutingAssembly().GetName().Version;

            Config.Load();

            Logger.WriteToFile = Config.GetBooleanValue("log-to-file", true);
            Logger.SetLogLevel(Config.GetStringValue("log-level", "info"));
        }

        private static void NotifyIcon_BalloonTipClicked(object sender, EventArgs e)
        {
            if (latestBalloonTip != null)
            {
                latestBalloonTip.OnClick(e);

                if (latestBalloonTip.ExitOnClose)
                    resetEvent.Set();
            }
        }

        private static void NotifyIcon_BalloonTipClosed(object sender, EventArgs e)
        {
            if (latestBalloonTip != null && latestBalloonTip.ExitOnClose)
                resetEvent.Set();
        }

        internal static void Shutdown()
        {
            Logger.Info("Shutting down");

            if (notifyIcon != null)
            {
                // hide & dispose of taskbar icon
                notifyIcon.Visible = false;
                notifyIcon.Dispose();
                notifyIcon = null;
            }

            Logger.CleanFiles();

            Application.Exit();
        }

        internal static void ForceShutdown()
        {
            Shutdown();
            Environment.Exit(0); // kill everything
        }

        private static bool IsCorrectLocation()
        {
            // check if program is in same directory as league of legends
            if (!File.Exists("LeagueClient.exe"))
            {
                Logger.Fatal("Launcher executable not found!");

                MessageBox.Show("Please place LoL Auto Login in your League of Legends directory (beside the \"LeagueClient.exe\" file).", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);

                return false;
            }

            return true;
        }

        private static bool PasswordExists()
        {
            if (!File.Exists("password"))
                return false;

            using (var reader = new StreamReader("password"))
            {
                if (!Regex.IsMatch(reader.ReadToEnd(), @"^[a-zA-Z0-9\+\/]*={0,3}$"))
                    return true;

                Logger.Info("Password is old format; prompting user to enter password again");
                MessageBox.Show("Password encryption has been changed. You will be prompted to enter your password once again.", "LoL Auto Login - Encryption method changed", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            return false;
        }

        internal static void ShowBalloonTip(ShowBalloonTipEventArgs e)
        {
            resetEvent.Reset();
            latestBalloonTip = e;
            notifyIcon.ShowBalloonTip(2500, e.Title, e.Message, e.Icon);
        }

        internal static void FatalError(string message, Exception ex)
        {
            Logger.PrintException(message, ex, true);

            ShowBalloonTip(new ShowBalloonTipEventArgs(
                "LoL Auto Login has encountered a fatal error",
                "Click here to access the log. If this issue persists, please submit an issue.",
                ToolTipIcon.Error,
                true,
                (sender, e) => Util.OpenFolderAndSelectFile(Logger.LogFile)
            ));
        }
    }
}
