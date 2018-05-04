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

using AutoIt;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

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
                Window clientWindow;

                // check if client is already running & window is present
                if (Process.GetProcessesByName("LeagueClient").Length > 0)
                {
                    Logger.Info("Client is already open");
                    
                    Rectangle passwordRect;

                    clientWindow = GetClientWindowHandle();

                    // check if password box is visible (not logged in)
                    if (clientWindow != null && (passwordRect = GetPasswordRect(clientWindow)) != Rectangle.Empty)
                    {
                        Logger.Info("Client is open on login page, entering password");

                        // client is on login page, enter password
                        EnterPassword(clientWindow, passwordRect);
                    }
                    else
                    {
                        Logger.Info("Client doesn't seem to be on login page; focusing client");

                        // client is logged in, show window
                        FocusClient();

                        Application.Exit();
                    }
                }
                else
                {
                    Logger.Info("Client is not running, launching client");
                    Logger.Info($"Waiting for {Settings.ClientTimeout} ms");

                    Start();

                    // create & start stopwatch
                    var sw = new Stopwatch();
                    sw.Start();

                    // get client handle
                    clientWindow = AwaitClientHandle();

                    // check if we got a valid handle
                    if (clientWindow != null)
                    {
                        Logger.Info($"Client found after {sw.ElapsedMilliseconds} ms");

                        // get password box
                        var found = WaitForPasswordBox();

                        // check if the password box was found
                        if (found != Rectangle.Empty)
                        {
                            Logger.Info($"Password box found after {sw.ElapsedMilliseconds} ms");

                            EnterPassword(clientWindow, found);
                        }
                        else
                        {
                            Logger.Info("Client window lost");
                        }
                    }
                    else
                    {
                        Logger.Info($"Client not found after {Settings.ClientTimeout} ms");
                    }
                }
            });
        }

        /// <summary>
        /// Hangs until the client window is found or the preset timeout is reached.
        /// </summary>
        /// <returns>Client window handle if found, zero if not.</returns>
        private static Window AwaitClientHandle()
        {
            // create & start stopwatch
            var sw = new Stopwatch();
            sw.Start();

            // create client handle variable
            Window clientWindow = GetClientWindowHandle();

            // search for window until client timeout is reached or window is found
            while (sw.ElapsedMilliseconds < Settings.ClientTimeout && clientWindow == null)
            {
                Thread.Sleep(500);
                clientWindow = GetClientWindowHandle();
            };

            // return found handle
            return clientWindow;
        }

        /// <summary>
        /// Hangs until the password box for the client is found or the client exits.
        /// </summary>
        /// <param name="progress">Progress interface used to pass messages</param>
        /// <returns>Whether the password box was found or not.</returns>
        private static Rectangle WaitForPasswordBox()
        {
            // create found & handle varables
            Rectangle found = Rectangle.Empty;
            Window clientWindow;

            // loop while not found and while client handle is something
            do
            {
                clientWindow = GetClientWindowHandle();
                
                if (clientWindow == null)
                    continue;
                
                found = GetPasswordRect(clientWindow);
                
                Thread.Sleep(500);
            }
            while (clientWindow != null && found == Rectangle.Empty);

            // return whether client was found or not
            return found;
        }

        /// <summary>
        /// Checks to see if the password box is visible in the client's window.
        /// </summary>
        /// <param name="clientHandle">Handle of the client window</param>
        /// <returns>Whether the password box is visible or not.</returns>
        private static Rectangle GetPasswordRect(Window clientWindow)
        {
            // check that the handle is valid
            if (clientWindow == null)
                return Rectangle.Empty;

            // compare the images
            var found = Util.CompareImage(clientWindow.Capture(), Properties.Resources.template_canny, Settings.PasswordMatchTolerance, new Size(1024, 576), new RectangleF(0.8125f, 0.0f, 0.1875f, 1.0f));

            // force garbage collection
            GC.Collect();

            // return
            return found;
        }

        /// <summary>
        /// Enters the password into the client's password box.
        /// </summary>
        /// <param name="clientHandle">Handle of the client window</param>
        /// <param name="progress">Progress interface used to pass messages</param>
        private static void EnterPassword(Window clientWindow, Rectangle passwordRect)
        {
            // create password string
            string password;
            
            // create file stream
            using (var file = new FileStream("password", FileMode.Open, FileAccess.Read))
            {
                // read bytes
                var buffer = new byte[file.Length];
                file.Read(buffer, 0, (int)file.Length);

                // decrypt password
                password = Encryption.Decrypt(buffer);
            }

            // create character array from password
            var passArray = password.ToCharArray();

            Logger.Info("Entering password");

            Rectangle rect = clientWindow.GetRect();

            if (Settings.EnableClick)
                AutoItX.MouseClick("primary", rect.Left + passwordRect.Left + passwordRect.Width / 2, rect.Top + passwordRect.Top + passwordRect.Height / 2, 1, 0);

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

            Application.Exit();
        }

        /// <summary>
        /// Retrieves the handle of the League Client window.
        /// </summary>
        /// <returns>Handle of the client.</returns>
        private static Window GetClientWindowHandle() => Util.GetSingleWindowFromImage(CLIENT_CLASS, CLIENT_NAME, new Size(1024, 576), Properties.Resources.template_canny, new Size(1024, 576), Settings.PasswordMatchTolerance);

        /// <summary>
        /// Focuses all League Client windows
        /// </summary>
        private static void FocusClient() => Util.FocusWindows(CLIENT_CLASS, CLIENT_NAME);
    }
}
