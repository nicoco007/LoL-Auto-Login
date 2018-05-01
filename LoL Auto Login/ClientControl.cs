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
                IntPtr clientHandle;

                // check if client is already running & window is present
                if (Process.GetProcessesByName("LeagueClient").Length > 0)
                {
                    Logger.Info("Client is already open");
                    
                    Rectangle passwordRect;

                    clientHandle = GetClientWindowHandle();

                    Console.WriteLine(GetPasswordRect(clientHandle));

                    // check if password box is visible (not logged in)
                    if (clientHandle != IntPtr.Zero && (passwordRect = GetPasswordRect(clientHandle)) != Rectangle.Empty)
                    {
                        Logger.Info("Client is open on login page, entering password");

                        // client is on login page, enter password
                        EnterPassword(clientHandle, passwordRect);
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
                    clientHandle = AwaitClientHandle();

                    // check if we got a valid handle
                    if (clientHandle != IntPtr.Zero)
                    {
                        Logger.Info($"Client found after {sw.ElapsedMilliseconds} ms");

                        // get password box
                        var found = WaitForPasswordBox();

                        // check if the password box was found
                        if (clientHandle != IntPtr.Zero && found != Rectangle.Empty)
                        {
                            Logger.Info($"Password box found after {sw.ElapsedMilliseconds} ms");

                            EnterPassword(clientHandle, found);
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
        private static IntPtr AwaitClientHandle()
        {
            // create & start stopwatch
            var sw = new Stopwatch();
            sw.Start();

            // create client handle variable
            IntPtr clientHandle = GetClientWindowHandle();

            // search for window until client timeout is reached or window is found
            while (sw.ElapsedMilliseconds < Settings.ClientTimeout && clientHandle == IntPtr.Zero)
            {
                Thread.Sleep(500);
                clientHandle = GetClientWindowHandle();
            };

            // return found handle
            return clientHandle;
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
            IntPtr clientHandle;

            // loop while not found and while client handle is something
            do
            {
                clientHandle = GetClientWindowHandle();
                
                if (clientHandle == IntPtr.Zero)
                    continue;
                
                found = GetPasswordRect(clientHandle);
                
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
        private static Rectangle GetPasswordRect(IntPtr clientHandle)
        {
            // check that the handle is valid
            if (clientHandle == IntPtr.Zero)
                return Rectangle.Empty;

            // compare the images
            var found = Util.CompareImage(Util.CaptureWindow(clientHandle), Properties.Resources.template, Settings.PasswordMatchTolerance, new double[] { 1, 0.8125, 0.64 }, new RectangleF(0.8f, 0.0f, 0.2f, 1.0f));

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
        private static void EnterPassword(IntPtr clientHandle, Rectangle passwordRect)
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

            Logger.Info("Entering password...");

            RECT rect;
            NativeMethods.GetWindowRect(clientHandle, out rect);

            AutoItX.MouseClick("primary", rect.Left + passwordRect.Left + passwordRect.Width / 2, rect.Top + passwordRect.Top + passwordRect.Height / 2, 1, 0);

            int i = 0;

            // enter password one character at a time
            while (i <= passArray.Length && NativeMethods.IsWindow(clientHandle))
            {
                // get window rectangle, in case it is resized or moved
                NativeMethods.GetWindowRect(clientHandle, out rect);

                Logger.Trace("Client rectangle: " + rect.ToString());

                // focus window & click on password box
                NativeMethods.SetForegroundWindow(clientHandle);

                // check if client is foreground window
                if (NativeMethods.GetForegroundWindow() == clientHandle)
                {
                    if (i < passArray.Length)
                        AutoItX.ControlSend(clientHandle, IntPtr.Zero, string.Format("{{ASC {0:000}}}", (int)passArray[i]), 0);
                    else
                        AutoItX.ControlSend(clientHandle, IntPtr.Zero, "{ENTER}", 0);

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
        private static IntPtr GetClientWindowHandle() => Util.GetSingleWindowFromImage(CLIENT_CLASS, CLIENT_NAME, Properties.Resources.loginLogo, Settings.LogoMatchTolerance);

        /// <summary>
        /// Focuses all League Client windows
        /// </summary>
        private static void FocusClient() => Util.FocusWindows(CLIENT_CLASS, CLIENT_NAME);
    }
}
