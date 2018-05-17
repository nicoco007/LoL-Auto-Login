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
using System.Windows.Forms;

namespace LoLAutoLogin
{
    public partial class MainForm : Form
    {
        private ShowBalloonTipEventArgs latestBalloonTip;

        public MainForm()
        {
            InitializeComponent();

            // create notification icon context menu (so user can exit if program hangs)
            var menu = new ContextMenu();
            var item = new MenuItem("&Exit", (sender, e) => Application.Exit());
            menu.MenuItems.Add(item);
            notifyIcon.ContextMenu = menu;

            // trigger save when the 'enter' key is pressed
            AcceptButton = saveButton;

            // hide
            Opacity = 0.0f;
            ShowInTaskbar = false;
        }

        private async void MainForm_Load(object sender, EventArgs e)
        {
            // start logging
            Logger.Info("Started LoL Auto Login v" + Assembly.GetExecutingAssembly().GetName().Version);
            Logger.Info($"OS Version: {Util.GetFriendlyOSVersion()}");

            foreach (Screen screen in Screen.AllScreens)
                Logger.Info(string.Format("Screen {0}: {1}", screen.DeviceName, screen.Bounds));

            string fileName = Path.Combine(Folders.Configuration.FullName, "LeagueClientSettings.yaml");
            ClientInfo info;

            if (File.Exists(fileName))
            {
                try
                {
                    info = new ClientInfo(fileName);

                    Logger.Info($"Region: {info.Region}; Locale: {info.Locale}");

                    if (!info.RemembersUsername)
                        Logger.Warn("Client isn't configured to remember username");
                }
                catch (Exception ex)
                {
                    Logger.PrintException(ex);
                    Logger.Warn("Failed to load client settings");
                }
            }
            else
            {
                Logger.Info($"Client configuration not found at \"{fileName}\"");
            }

            // load settings
            Settings.Load();

            LogLevel logLevel;

            if (Enum.TryParse(Settings.LogLevel, true, out logLevel))
                Logger.Level = logLevel;
            else
                Logger.Info($"Invalid log level \"{Settings.LogLevel}\"");

            CheckLocation();

            // check if a Shift key is being pressed
            if (NativeMethods.GetAsyncKeyState(Keys.RShiftKey) != 0 || NativeMethods.GetAsyncKeyState(Keys.LShiftKey) != 0)
            {
                Logger.Info("Shift key is being pressed - starting client without running LoL Auto Login");

                // try launching league of legends
                try
                {
                    ClientControl.Start();
                    Application.Exit();
                }
                catch (Exception ex)
                {
                    // print error to log and show balloon tip to inform user of fatal error
                    Logger.Fatal("Could not start League of Legends!");
                    Logger.PrintException(ex, true);
                    
                    ShowFatalErrorBalloonTip();
                }

                return;
            }
            
            if (PasswordExists())
            {
                try
                {
                    await ClientControl.RunLogin();
                }
                catch (Exception ex)
                {
                    // print error to log and show balloon tip to inform user of fatal error
                    Logger.Fatal("Could not start League of Legends!");
                    Logger.PrintException(ex, true);

                    ShowFatalErrorBalloonTip();
                }
            }
            else
            {
                // show window if it doesn't
                Opacity = 1.0f;
                ShowInTaskbar = true;
                Logger.Info("Password file not found, prompting user to enter password");
            }
        }

        private void CheckLocation()
        {
            // check if program is in same directory as league of legends
            if (!File.Exists("LeagueClient.exe"))
            {
                Logger.Fatal("Launcher executable not found!");
                
                MessageBox.Show(this, "Please place LoL Auto Login in your League of Legends directory (beside the \"LeagueClient.exe\" file).", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);

                Application.Exit();
            }
        }

        private bool PasswordExists()
        {
            if (!File.Exists("password")) return false;

            using (var reader = new StreamReader("password"))
            {
                if (!Regex.IsMatch(reader.ReadToEnd(), @"^[a-zA-Z0-9\+\/]*={0,3}$"))
                    return true;

                Logger.Info("Password is old format; prompting user to enter password again");
                MessageBox.Show("Password encryption has been changed. You will be prompted to enter your password once again.", "LoL Auto Login - Encryption method changed", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            return false;
        }

        private async void saveButton_Click(object sender, EventArgs e)
        {
            // check if a password was inputted
            if (string.IsNullOrEmpty(passTextBox.Text))
            {
                MessageBox.Show(this, "You must enter a password!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            Logger.Info("Encrypting & saving password to file");
            
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
                MessageBox.Show(this, "Something went wrong when trying to save your password:" + Environment.NewLine + Environment.NewLine + ex.StackTrace, "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                
                Logger.Error("Could not save password to file");
                Logger.PrintException(ex, false);

                return;
            }

            Hide();

            try
            {
                await ClientControl.RunLogin();
            }
            catch (Exception ex)
            {
                Logger.Fatal("Failed to run login sequence");
                Logger.PrintException(ex);
                ShowFatalErrorBalloonTip();
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Logger.Info("Shutting down");

            // hide & dispose of taskbar icon
            notifyIcon.Visible = false;
            notifyIcon.Dispose();
            notifyIcon = null;

            Logger.CleanFiles();
        }

        private void notifyIcon_BalloonTipClicked(object sender, EventArgs e)
        {
            if (latestBalloonTip != null)
            {
                latestBalloonTip.OnClick(e);

                if (latestBalloonTip.ExitOnClose)
                    Application.Exit();
            }
        }

        private void notifyIcon_BalloonTipClosed(object sender, EventArgs e)
        {
            if (latestBalloonTip != null && latestBalloonTip.ExitOnClose)
                Application.Exit();
        }

        private void ShowBalloonTip(ShowBalloonTipEventArgs e)
        {
            latestBalloonTip = e;
            notifyIcon.ShowBalloonTip(2500, e.Title, e.Message, e.Icon);
        }

        private void ShowFatalErrorBalloonTip()
        {
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
