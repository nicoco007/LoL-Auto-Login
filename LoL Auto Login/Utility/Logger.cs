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
using System.Linq;
using System.IO;
using System.Windows.Forms;

namespace LoLAutoLogin
{
    internal enum LogLevel
    {
        Trace,
        Debug,
        Info,
        Warning,
        Error,
        Fatal
    }

    internal static class Logger
    {
        internal static bool WriteToFile { get; set; }
        internal static LogLevel Level { get; private set; }
        internal static string LogFile { get; private set; }

        private static readonly DateTime StartTime = DateTime.Now;

        internal static void Setup()
        {
            Level = LogLevel.Info;
            LogFile = Path.Combine(Folders.Logs, $@"{DateTime.Now:yyyy-MM-dd\THH-mm-ss}_LoLAutoLogin.log");
        }

        internal static void SetLogLevel(string level)
        {
            if (Enum.TryParse(level, true, out LogLevel logLevel))
                Level = logLevel;
            else
                Info($"Invalid log level \"{level}\", defaulting to INFO");
        }

        internal static void Trace(object message, params object[] arg)
        {
            if (Level <= LogLevel.Trace)
                Write("TRACE", message.ToString(), arg);
        }

        internal static void Debug(object message, params object[] arg)
        {
            if (Level <= LogLevel.Debug)
                Write("DEBUG", message.ToString(), arg);
        }

        internal static void Info(object message, params object[] arg)
        {
            if (Level <= LogLevel.Info)
                Write("INFO", message.ToString(), arg);
        }

        internal static void Warn(object message, params object[] arg)
        {
            if (Level <= LogLevel.Warning)
                Write("WARN", message.ToString(), arg);
        }

        internal static void Error(object message, params object[] arg)
        {
            if (Level <= LogLevel.Error)
                Write("ERROR", message.ToString(), arg);
        }

        internal static void Fatal(object message, params object[] arg)
        {
            if (Level <= LogLevel.Fatal)
                Write("FATAL", message.ToString(), arg);
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

        internal static void CleanFiles()
        {
            if (Directory.Exists(Folders.Logs))
            {
                FileInfo[] logFiles = new DirectoryInfo(Folders.Logs).GetFiles().OrderByDescending(f => f.LastWriteTime).ToArray();

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

        private static void Write(string tag, string text, params object[] arg)
        {
            if (arg.Length > 0)
                text = string.Format(text, arg);

            var st = new System.Diagnostics.StackTrace(true);
            var frame = st.GetFrame(2);
            var fileName = Path.GetFileNameWithoutExtension(frame.GetFileName());
            var line = frame.GetFileLineNumber();
            var now = (DateTime.Now - StartTime).ToString("G");

            StreamWriter writer = null;

            try
            {
                string directory = Path.GetDirectoryName(LogFile);

                if (WriteToFile)
                {
                    if (!Directory.Exists(directory))
                        Directory.CreateDirectory(directory);

                    writer = new StreamWriter(LogFile, true);
                }

                foreach (var str in text.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries))
                {
                    var filteredStr = str.Replace(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "~");
                    var msg = $"{now} | {tag,5} | <{fileName}:{line}> {filteredStr}";

                    System.Diagnostics.Debug.WriteLine(msg);

                    if (writer != null)
                        writer.WriteLine(msg);
                }

                if (writer != null)
                    writer.Flush();
            }
            catch (Exception ex)
            {
                WriteToFile = false; // disable writing to file to avoid spam message boxes
                PrintException(ex);
                MessageBox.Show($"Logging to file has been disabled due to an error. Please check permissions on the \"{Folders.Logs}\" folder or disable logging through the settings file.", "LoL Auto Login", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (writer != null)
                    writer.Close();
            }
        }
    }
}
