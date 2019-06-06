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

using LoLAutoLogin.Managers;
using LoLAutoLogin.Model;
using LoLAutoLogin.Native;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;

namespace LoLAutoLogin.Utility
{
    internal static class ClientControl
    {
        private const string ROOT_CLIENT_CLASS = "RCLIENT";
        private const string ROOT_CLIENT_NAME = null;
        private const string CHILD_CLIENT_CLASS = null;
        private const string CHILD_CLIENT_NAME = "Chrome Legacy Window";

        /// <summary>
        /// Starts the League Client executable
        /// </summary>
        internal static void StartClient()
        {
            Process.Start("LeagueClient.exe");
        }

        /// <summary>
        /// Runs all the logic necessary to enter the password automatically into the League Client
        /// </summary>
        internal static async Task RunLogin()
        {
            await Task.Factory.StartNew(() =>
            {
                ClientWindow clientWindow;

                // check if client is already running & window is present
                if (Process.GetProcessesByName("LeagueClient").Length > 0)
                {
                    Logger.Info("Client is already open");
                    
                    Rectangle passwordRect;

                    clientWindow = GetClientWindow();

                    // check if password box is visible (not logged in)
                    if (clientWindow != null && (passwordRect = GetPasswordRect(clientWindow)) != Rectangle.Empty)
                    {
                        Logger.Info("Client is open on login page, entering password");

                        // client is on login page, enter password
                        EnterPassword(clientWindow, true);
                    }
                    else
                    {
                        Logger.Info("Client doesn't seem to be on login page; focusing client");

                        // client is logged in, show window
                        FocusClientWindows();
                    }
                }
                else
                {
                    int clientTimeout = Config.GetIntegerValue("client-load-timeout", 30) * 1000;

                    Logger.Info("Client is not running, launching client");
                    Logger.Info($"Waiting for {clientTimeout} ms");

                    Program.SetNotifyIconText("Waiting for client");

                    StartClient();

                    // create & start stopwatch
                    var sw = new Stopwatch();
                    sw.Start();

                    // get client handle
                    clientWindow = AwaitClientHandle(clientTimeout);

                    // check if we got a valid handle
                    if (clientWindow != null)
                    {
                        Logger.Info($"Client found after {sw.ElapsedMilliseconds} ms at {clientWindow.GetRect()}");

                        // get password box
                        var passwordBox = WaitForPasswordBox(clientWindow);

                        // check if the password box was found
                        if (passwordBox != Rectangle.Empty)
                        {
                            Logger.Info($"Password box found after {sw.ElapsedMilliseconds} ms at {passwordBox}");

                            EnterPassword(clientWindow, false);
                        }
                        else
                        {
                            Logger.Info("Client window lost");
                        }
                    }
                    else
                    {
                        Logger.Info($"Client not found after {clientTimeout} ms");
                    }
                }
            }, TaskCreationOptions.LongRunning);
        }

        /// <summary>
        /// Hangs until the client window is found or the preset timeout is reached.
        /// </summary>
        /// <param name="timeout">Timeout in milliseconds</param>
        /// <returns>Client window handle if found, zero if not.</returns>
        private static ClientWindow AwaitClientHandle(int timeout)
        {
            var sw = new Stopwatch();
            sw.Start();
            
            ClientWindow clientWindow = GetClientWindow();

            // search for window until client timeout is reached or window is found
            while (sw.ElapsedMilliseconds < timeout && (clientWindow == null || clientWindow.Status == ClientStatus.Unknown))
            {
                Thread.Sleep(500);

                if (clientWindow != null)
                    clientWindow.RefreshStatus();
                else
                    clientWindow = GetClientWindow();
            }
            
            return clientWindow;
        }

        /// <summary>
        /// Hangs until the password box for the client is found or the client exits.
        /// </summary>
        /// <returns>Whether the password box was found or not.</returns>
        private static Rectangle WaitForPasswordBox(ClientWindow clientWindow)
        {
            // loop while not found and while client handle is something
            try
            {
                while (clientWindow.Exists() && clientWindow.Status != ClientStatus.OnLoginScreen)
                {
                    Thread.Sleep(500);
                    clientWindow.RefreshStatus();
                }
            }
            catch (Exception ex)
            {
                Logger.PrintException("Failed to refresh the client window's state", ex);
            }

            // return whether client was found or not
            return clientWindow.PasswordBox;
        }

        private static Rectangle GetPasswordRect(ClientWindow clientWindow)
        {
            // check that the handle is valid
            if (clientWindow == null)
                return Rectangle.Empty;

            var result = Rectangle.Empty;

            if (clientWindow.Status == ClientStatus.OnLoginScreen)
                result = clientWindow.PasswordBox;
            
            GC.Collect();
            
            return result;
        }

        /// <summary>
        /// Enters the password into the client's password box.
        /// </summary>
        /// <param name="clientHandle">Handle of the client window</param>
        /// <param name="progress">Progress interface used to pass messages</param>
        private static void EnterPassword(ClientWindow clientWindow, bool running)
        {
            int delay = Config.GetIntegerValue("login-detection.delay", 500);

            Logger.Info($"Waiting {delay} ms before entering password");

            Thread.Sleep(delay);

            Logger.Info("Entering password");
            Program.SetNotifyIconText("Entering password");

            Rectangle passwordBox = clientWindow.PasswordBox;

            // erase whatever is in the password box (double-click selects everything)
            clientWindow.InnerWindow.SendMouseClick(passwordBox.Left + passwordBox.Width / 2, passwordBox.Top + passwordBox.Height / 2);
            clientWindow.InnerWindow.SendMouseClick(passwordBox.Left + passwordBox.Width / 2, passwordBox.Top + passwordBox.Height / 2);
            clientWindow.InnerWindow.Parent.SendKey(VirtualKeyCode.BACK);

            clientWindow.InnerWindow.Parent.SendText(ProfileManager.GetDefaultProfile().DecryptPassword());

            clientWindow.InnerWindow.Parent.SendKey(VirtualKeyCode.RETURN);

            Logger.Info("Successfully entered password (well, hopefully)");
        }

        private static ClientWindow GetClientWindow()
        {
            List<Window> windows = Util.GetWindows(ROOT_CLIENT_CLASS, ROOT_CLIENT_NAME);

            foreach (var window in windows)
            {
                Window child = window.FindChildRecursively(CHILD_CLIENT_CLASS, CHILD_CLIENT_NAME);

                if (child == null)
                {
                    continue;
                }

                ClientWindow clientWindow = ClientWindow.FromWindow(window, child);

                if (clientWindow.IsMatch)
                {
                    return clientWindow;
                }
            }

            return null;
        }
        
        /// <summary>
        /// Focuses all League Client windows
        /// </summary>
        private static void FocusClientWindows() => Util.FocusWindows(ROOT_CLIENT_CLASS, ROOT_CLIENT_NAME);
    }
}
