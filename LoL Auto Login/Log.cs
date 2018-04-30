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
    public static class Log
    {
        private static readonly DateTime StartTime = DateTime.Now;
        public static string LogFile = $@"{DateTime.Now:yyyy-MM-dd\THH-mm-ss}_LoLAutoLogin.log";

        // write a message using the INFO tag
        public static void Verbose(string message, params object[] arg)
        {
            if (Environment.GetCommandLineArgs().Contains("--verbose"))
                Write("[VERBOSE] " + message, arg);
        }
        
        public static void Debug(string message, params object[] arg)
        {
            if(Environment.GetCommandLineArgs().Contains("--debug") || Environment.GetCommandLineArgs().Contains("--verbose"))
                Write("[DEBUG] " + message, arg);
        }

        // write a message using the INFO tag
        public static void Info(string message, params object[] arg)
        {
            Write("[INFO] " + message, arg);
        }

        // write a message using the WARN tag
        public static void Warn(string message, params object[] arg)
        {
            Write("[WARN] " + message, arg);
        }

        // write a message using the ERROR tag
        public static void Error(string message, params object[] arg)
        {
            Write("[ERROR] " + message, arg);
        }

        // write a message using the FATAL tag
        public static void Fatal(string message, params object[] arg)
        {
            Write("[FATAL] " + message, arg);
        }

        // print a stacktrace string using the FATAL tag
        public static void PrintStackTrace(string st)
        {
            foreach (var str in st.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries))
            {
                Fatal(str);
            }
        }

        // write whatever to the debug log and the log file
        public static void Write(string text, params object[] arg)
        {
            if (arg.Length > 0)
                text = string.Format(text, arg);

            text = (DateTime.Now - StartTime).ToString("G") + " " + text;

            foreach (var str in text.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries))
                Console.WriteLine(str);

            try
            {
                var dir = GetLogFileDirectory();
                var file = GetLogFilePath();

                if (dir != null && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                using (var sw = new StreamWriter(file, true))
                    sw.WriteLine(text);
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.StackTrace);
            }
        }

        public static void PrintException(Exception ex)
        {
            Error($"An error of type {ex.GetType()} occured: {ex.Message}");
            Error(ex.StackTrace);
        }

        public static string GetLogFileDirectory()
        {
            return Path.Combine(Directory.GetCurrentDirectory(), "Logs", "LoL Auto Login Logs");
        }

        public static string GetLogFilePath()
        {
            return Path.Combine(GetLogFileDirectory(), LogFile);
        }
    }
}
