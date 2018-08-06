﻿// Copyright © 2015-2018 Nicolas Gnyra

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

using AutoIt;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;

namespace LoLAutoLogin
{
    internal static class ClientControl
    {
        private const string CLIENT_CLASS = "RCLIENT";
        private const string CLIENT_NAME = null;

        /// <summary>
        /// Starts the League Client executable
        /// </summary>
        internal static void Start()
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
                        EnterPassword(clientWindow, passwordRect, true);
                    }
                    else
                    {
                        Logger.Info("Client doesn't seem to be on login page; focusing client");

                        // client is logged in, show window
                        FocusClient();

                        Program.Shutdown();
                    }
                }
                else
                {
                    int clientTimeout = Config.GetIntegerValue("client-load-timeout", 30) * 1000;

                    Logger.Info("Client is not running, launching client");
                    Logger.Info($"Waiting for {clientTimeout} ms");

                    Program.SetNotifyIconText("Waiting for client");

                    Start();

                    // create & start stopwatch
                    var sw = new Stopwatch();
                    sw.Start();

                    // get client handle
                    clientWindow = AwaitClientHandle(clientTimeout);

                    // check if we got a valid handle
                    if (clientWindow != null)
                    {
                        Logger.Info($"Client found after {sw.ElapsedMilliseconds} ms");

                        // get password box
                        var found = WaitForPasswordBox(clientWindow);

                        // check if the password box was found
                        if (found != Rectangle.Empty)
                        {
                            Logger.Info($"Password box found after {sw.ElapsedMilliseconds} ms");

                            EnterPassword(clientWindow, found, false);
                        }
                        else
                        {
                            Logger.Info("Client window lost");
                            Program.Shutdown();
                        }
                    }
                    else
                    {
                        Logger.Info($"Client not found after {clientTimeout} ms");
                        Program.Shutdown();
                    }
                }
            });
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
            
            ClientWindow clientWindow;

            // search for window until client timeout is reached or window is found
            do
            {
                clientWindow = GetClientWindow();
                Thread.Sleep(500);
            }
            while (sw.ElapsedMilliseconds < timeout && (clientWindow == null || clientWindow.Status == ClientStatus.Unknown));
            
            return clientWindow;
        }

        /// <summary>
        /// Hangs until the password box for the client is found or the client exits.
        /// </summary>
        /// <returns>Whether the password box was found or not.</returns>
        private static Rectangle WaitForPasswordBox(ClientWindow clientWindow)
        {
            // create found & handle varables
            Rectangle found = Rectangle.Empty;

            // loop while not found and while client handle is something
            do
            {
                try
                {
                    clientWindow.RefreshStatus();
                    found = clientWindow.PasswordBox;
                }
                catch (Exception ex)
                {
                    Logger.PrintException(ex);
                    break;
                }

                Thread.Sleep(500);
            }
            while (clientWindow.Exists() && clientWindow.Status != ClientStatus.OnLoginScreen);

            // return whether client was found or not
            return found;
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
        private static void EnterPassword(Window clientWindow, Rectangle passwordRect, bool running)
        {
            string password = PasswordManager.Load();

            // create character array from password
            var passArray = password.ToCharArray();

            Logger.Info("Entering password");
            Program.SetNotifyIconText("Entering password");

            Rectangle rect = clientWindow.GetRect();

            if (running || Config.GetBooleanValue("login-detection.always-click", true))
            {
                clientWindow.Focus();
                AutoItX.MouseClick("primary", rect.Left + passwordRect.Left + passwordRect.Width / 2, rect.Top + passwordRect.Top + passwordRect.Height / 2, 1, 0);
                AutoItX.ControlSend(clientWindow.Handle, IntPtr.Zero, "^a", 0);
                AutoItX.ControlSend(clientWindow.Handle, IntPtr.Zero, "{BACKSPACE}", 0);
            }

            int i = 0;

            // enter password one character at a time
            while (i <= passArray.Length && clientWindow.Exists())
            {
                // get window rectangle, in case it is resized or moved
                rect = clientWindow.GetRect();

                // focus window & click on password box
                clientWindow.Focus();

                // check if client is foreground window
                if (clientWindow.IsFocused())
                {
                    if (i < passArray.Length)
                        AutoItX.ControlSend(clientWindow.Handle, IntPtr.Zero, string.Format("{{ASC {0:000}}}", (int)passArray[i]), 0);
                    else
                        AutoItX.ControlSend(clientWindow.Handle, IntPtr.Zero, "{ENTER}", 0);

                    i++;
                }
            }
            
            Logger.Info("Successfully entered password (well, hopefully)!");

            Program.Shutdown();
        }

        private static ClientWindow GetClientWindow()
        {
            List<Window> windows = Util.GetWindows(CLIENT_CLASS, CLIENT_NAME);

            foreach (var window in windows)
            {
                var match = ClientWindow.FromWindow(window);

                if (match.IsMatch)
                    return match;
            }

            return null;
        }
        
        /// <summary>
        /// Focuses all League Client windows
        /// </summary>
        private static void FocusClient() => Util.FocusWindows(CLIENT_CLASS, CLIENT_NAME);
    }
}