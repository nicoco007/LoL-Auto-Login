using System;
using System.Linq;
using System.IO;
using System.Windows.Forms;

namespace LoLAutoLogin
{
    public static class Log
    {

        private static DateTime startTime = DateTime.Now;
        public static string logFile = string.Format(@"{0:yyyy-MM-dd\THH-mm-ss}_LoLAutoLogin.log", DateTime.Now);

        // write a message using the INFO tag
        public static void Verbose(string message, params object[] arg)
        {

            if (Environment.GetCommandLineArgs().Contains("--verbose"))
            {

                Write("[VERBOSE] " + message, arg);

            }

        }
        
        public static void Debug(string message, params object[] arg)
        {

            if(Environment.GetCommandLineArgs().Contains("--debug") || Environment.GetCommandLineArgs().Contains("--verbose"))
            {

                Write("[DEBUG] " + message, arg);

            }

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

            foreach (string str in st.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries))
            {

                Log.Fatal(str);

            }

        }

        // write whatever to the debug log and the log file
        public static void Write(string text, params object[] arg)
        {

            text = (DateTime.Now - startTime).ToString("G") + " " + text;

            if (arg.Length > 0) Console.WriteLine(text, arg); else Console.WriteLine(text);

            try
            {

                string path = Directory.Exists(Directory.GetCurrentDirectory() + @"\Logs") ? Directory.GetCurrentDirectory() + @"\Logs\LoL Auto Login Logs\" + logFile : Directory.GetCurrentDirectory() + @"\lolautologin.log";
                string dir = Path.GetDirectoryName(path);

                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                using (StreamWriter sw = new StreamWriter(path, true))
                    if (arg.Length > 0) sw.WriteLine(text, arg); else sw.WriteLine(text);

            }
            catch(Exception ex)
            {

                MessageBox.Show(ex.StackTrace);

            }

        }

    }

}
