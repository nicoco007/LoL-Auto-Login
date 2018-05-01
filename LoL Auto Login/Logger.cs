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

using System;
using System.Linq;
using System.IO;
using System.Windows.Forms;

namespace LoLAutoLogin
{
    internal static class Logger
    {
        internal static LogLevel Level = LogLevel.Info;

        internal static string LogsDirectory { get { return Path.Combine(Directory.GetCurrentDirectory(), "Logs", "LoL Auto Login Logs"); } }
        internal static string LogFile { get { return Path.Combine(LogsDirectory, LogFileName); } }

        internal static readonly string LogFileName = $@"{DateTime.Now:yyyy-MM-dd\THH-mm-ss}_LoLAutoLogin.log";

        private static readonly DateTime StartTime = DateTime.Now;
        private static bool writeToFile = true;

        internal static void Trace(string message, params object[] arg)
        {
            if (Level <= LogLevel.Trace)
                Write("TRACE", message, arg);
        }

        internal static void Debug(string message, params object[] arg)
        {
            if (Level <= LogLevel.Debug)
                Write("DEBUG", message, arg);
        }

        internal static void Info(string message, params object[] arg)
        {
            if (Level <= LogLevel.Info)
                Write("INFO", message, arg);
        }

        internal static void Warn(string message, params object[] arg)
        {
            if (Level <= LogLevel.Warning)
                Write("WARN", message, arg);
        }

        internal static void Error(string message, params object[] arg)
        {
            if (Level <= LogLevel.Error)
                Write("ERROR", message, arg);
        }

        internal static void Fatal(string message, params object[] arg)
        {
            if (Level <= LogLevel.Fatal)
                Write("FATAL", message, arg);
        }

        internal static void PrintException(Exception ex, bool fatal = false)
        {
            string msg = $"An error of type {ex.GetType()} occured: {ex.Message}";

            if (fatal)
            {
                Fatal(msg);
                Fatal(ex.StackTrace);
            }
            else
            {
                Error(msg);
                Error(ex.StackTrace);
            }

        }

        internal static void Write(string tag, string text, params object[] arg)
        {
            if (arg.Length > 0)
                text = string.Format(text, arg);
            
            try
            {
                var dir = LogsDirectory;
                var file = LogFile;
                var now = (DateTime.Now - StartTime).ToString("G");

                if (dir != null && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                StreamWriter writer = null;

                if (writeToFile)
                    writer = new StreamWriter(file, true);
                
                foreach (var str in text.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries))
                {
                    var msg = $"{now} [{tag}] {str}";

                    Console.WriteLine(msg);
                        
                    if (writer != null)
                        writer.WriteLine(msg);
                }

                if (writer != null)
                    writer.Dispose();
            }
            catch(Exception ex)
            {
                writeToFile = false; // disable writing to file to avoid spam message boxes
                MessageBox.Show("Failed to write to log: " + ex.StackTrace);
            }
        }

        internal static void CleanFiles()
        {
            if (Directory.Exists(LogsDirectory))
            {
                FileInfo[] logFiles = new DirectoryInfo(LogsDirectory).GetFiles().OrderByDescending(f => f.LastWriteTime).ToArray();

                if (logFiles.Length > 50)
                {
                    Info($"Deleting {logFiles.Length - 50} old log files");

                    foreach (FileInfo logFile in logFiles.Skip(50))
                    {
                        try
                        {
                            Trace("Deleting " + logFile.Name);
                            logFile.Delete();
                        }
                        catch (Exception ex)
                        {
                            Error($"Failed to delete log file \"{logFile.Name}\": {ex.GetType()} - {ex.Message}");
                        }
                    }
                }
            }
        }
    }

    internal enum LogLevel
    {
        Trace,
        Debug,
        Info,
        Warning,
        Error,
        Fatal
    }
}