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

using System;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LoLAutoLogin
{
    internal static class Program
    {
        private static ManualResetEvent resetEvent = new ManualResetEvent(false);
        private static NotifyIcon notifyIcon;
        private static ShowBalloonTipEventArgs latestBalloonTip;

        /// <summary>
        /// Point d'entrée principal de l'application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            LoadSettings();

            // start logging
            Logger.Info("Started LoL Auto Login v" + Assembly.GetExecutingAssembly().GetName().Version);

            NativeMethods.SetProcessDPIAware();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Run().Wait();
        }

        private static async Task Run()
        {
            if (Config.GetBooleanValue("login-detection.debug", false))
            {
                if (!Folders.Debug.Exists)
                    Folders.Debug.Create();

                Logger.Info("Cleaning debug directory");

                foreach (FileInfo file in Folders.Debug.EnumerateFiles())
                    file.Delete();
            }
            else if (Folders.Debug.Exists)
            {
                Folders.Debug.Delete(true);
            }

            LoadNotifyIcon();
            ShowSystemInfo();
            CheckLocation();

            // check if a Shift key is being pressed
            if (NativeMethods.GetAsyncKeyState(Keys.RShiftKey) != 0 || NativeMethods.GetAsyncKeyState(Keys.LShiftKey) != 0)
            {
                Logger.Info("Shift key is being pressed - starting client without running LoL Auto Login");

                // try launching league of legends
                try
                {
                    ClientControl.Start();
                    Shutdown();
                }
                catch (Exception ex)
                {
                    // print error to log and show balloon tip to inform user of fatal error
                    FatalError("Could not start League of Legends!", ex);
                }

                return;
            }

            if (!PasswordExists())
            {
                Logger.Info("Password file not found, prompting user to enter password");

                var form = new MainForm();
                Application.Run(form);

                if (form.Success != true)
                    Shutdown();
            }

            try
            {
                await ClientControl.RunLogin();
            }
            catch (Exception ex)
            {
                // print error to log and show balloon tip to inform user of fatal error
                FatalError("Could not start League of Legends!", ex);
            }

            resetEvent.WaitOne();
        }

        private static void LoadNotifyIcon()
        {
            notifyIcon = new NotifyIcon();
            notifyIcon.Icon = Properties.Resources.Icon;
            notifyIcon.Visible = true;
            notifyIcon.Text = "LoL Auto Login";

            var menu = new ContextMenu();

            var item = new MenuItem("&Exit", (sender, e) => Shutdown());
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

            string fileName = Path.Combine(Folders.Configuration.FullName, "LeagueClientSettings.yaml");
            ClientInfo info;

            if (File.Exists(fileName))
            {
                try
                {
                    info = ClientInfo.FromFile(fileName);

                    Logger.Info($"Region: {info.Region}; Locale: {info.Locale}");

                    if (!info.RemembersUsername)
                        Logger.Warn("Client isn't configured to remember username");
                }
                catch (Exception ex)
                {
                    Logger.Warn("Failed to load client settings");
                    Logger.PrintException(ex);
                }
            }
            else
            {
                Logger.Info($"Client configuration not found at \"{fileName}\"");
            }
        }

        private static void LoadSettings()
        {
            Config.Load();

            LogLevel logLevel;
            string strLevel = Config.GetStringValue("log-level", "info");

            if (Enum.TryParse(strLevel, true, out logLevel))
                Logger.Level = logLevel;
            else
                Logger.Info($"Invalid log level \"{strLevel}\", defaulting to INFO");
        }

        private static void NotifyIcon_BalloonTipClicked(object sender, EventArgs e)
        {
            if (latestBalloonTip != null)
            {
                latestBalloonTip.OnClick(e);

                if (latestBalloonTip.ExitOnClose)
                    Shutdown();
            }
        }

        private static void NotifyIcon_BalloonTipClosed(object sender, EventArgs e)
        {
            if (latestBalloonTip != null && latestBalloonTip.ExitOnClose)
                Shutdown();
        }

        internal static void Shutdown()
        {
            Logger.Info("Shutting down");

            // hide & dispose of taskbar icon
            notifyIcon.Visible = false;
            notifyIcon.Dispose();
            notifyIcon = null;

            Logger.CleanFiles();

            resetEvent.Set();
        }

        private static void CheckLocation()
        {
            // check if program is in same directory as league of legends
            if (!File.Exists("LeagueClient.exe"))
            {
                Logger.Fatal("Launcher executable not found!");

                MessageBox.Show("Please place LoL Auto Login in your League of Legends directory (beside the \"LeagueClient.exe\" file).", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);

                Shutdown();
            }
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
            latestBalloonTip = e;
            notifyIcon.ShowBalloonTip(2500, e.Title, e.Message, e.Icon);
        }

        internal static void FatalError(string message, Exception ex)
        {
            Logger.Fatal(message);
            Logger.PrintException(ex, true);

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
