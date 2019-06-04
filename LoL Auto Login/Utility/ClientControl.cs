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

using LoLAutoLogin.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
                int timeout = Config.GetIntegerValue("client-load-timeout", 30) * 1000;
                bool clientIsAlreadyRunning = IsClientRunning();

                Logger.Info($"Waiting for login screen for {timeout} ms");

                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                if (!clientIsAlreadyRunning && !AwaitClientProcess(stopwatch, timeout))
                {
                    Logger.Error($"Client process not found after {timeout} ms");
                    return;
                }

                Logger.Info("Waiting for client window");

                ClientWindow clientWindow = AwaitClientWindow(stopwatch, timeout);

                if (clientWindow == null)
                {
                    Logger.Error($"Client window not found after {timeout} ms");
                    return;
                }

                clientWindow.RefreshStatus();

                if (clientIsAlreadyRunning && clientWindow.Status != ClientStatus.OnLoginScreen)
                {
                    Logger.Info("Client was already running and is not on login screen; assuming user is already logged in");
                    clientWindow.Focus();
                    return;
                }
                else if (!AwaitLoginScreen(clientWindow))
                {
                    Logger.Info($"Client window lost");
                    return;
                }

                int delay = Config.GetIntegerValue("login-detection.delay", 500);
                Logger.Info($"Waiting {delay} ms before entering password");
                Thread.Sleep(delay);

                Logger.Info("Entering password");
                Program.SetNotifyIconText("Entering password");

                string password = PasswordManager.Load();

                clientWindow.EnterPassword(password);
                clientWindow.Focus();

                Logger.Info("Successfully entered password (well, hopefully)");
            }, TaskCreationOptions.LongRunning);
        }

        private static bool IsClientRunning()
        {
            return Process.GetProcessesByName("LeagueClient").Length > 0;
        }

        private static bool AwaitClientProcess(Stopwatch stopwatch, int timeout)
        {
            if (!stopwatch.IsRunning)
                throw new InvalidOperationException("Stopwatch must be running");

            bool processExists = IsClientRunning();

            if (!processExists)
            {
                Logger.Info("Starting client");
                StartClient();
            }

            while (stopwatch.ElapsedMilliseconds < timeout && processExists == false)
            {
                Thread.Sleep(100);
                processExists = IsClientRunning();
            }

            return processExists;
        }

        /// <summary>
        /// Hangs until the client window is found or the preset timeout is reached.
        /// </summary>
        /// <param name="timeout">Timeout in milliseconds</param>
        /// <returns>Client window handle if found, zero if not.</returns>
        private static ClientWindow AwaitClientWindow(Stopwatch stopwatch, int timeout)
        {
            if (!stopwatch.IsRunning)
                throw new InvalidOperationException("Stopwatch must be running");

            ClientWindow clientWindow = GetClientWindow();

            while (IsClientRunning() && stopwatch.ElapsedMilliseconds < timeout && clientWindow == null)
            {
                Thread.Sleep(100);
                clientWindow = GetClientWindow();
            } 

            return clientWindow;
        }

        /// <summary>
        /// Hangs until the state of the client window is known.
        /// </summary>
        /// <returns>Whether the password box was found or not.</returns>
        private static bool AwaitLoginScreen(ClientWindow clientWindow)
        {
            if (clientWindow == null)
            {
                throw new ArgumentNullException(nameof(clientWindow));
            }

            try
            {
                clientWindow.RefreshStatus();

                while (clientWindow.Exists() && clientWindow.Status != ClientStatus.OnLoginScreen)
                {
                    Thread.Sleep(100);
                    clientWindow.RefreshStatus();
                }
            }
            catch (Exception ex)
            {
                Logger.PrintException("Failed to refresh the client window's state", ex);
            }

            return clientWindow.Status == ClientStatus.OnLoginScreen;
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

                return clientWindow;
            }

            return null;
        }
    }
}
