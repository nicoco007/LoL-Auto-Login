using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using System.Text.RegularExpressions;

namespace LoLAutoLogin
{
    class Log
    {
        public static string logFile = String.Format(@"{0:yyyy-MM-dd\THH-mm-ss}_LoLAutoLogin.log", DateTime.Now);

        // write a message using the INFO tag
        public static void Verbose(string message)
        {
            if (Environment.GetCommandLineArgs().Contains("-v"))
            {
                Write(String.Format("{0:G} [VERBOSE] {1}", DateTime.Now, message));
            }
        }
        
        public static void Debug(string message)
        {
            if(Environment.GetCommandLineArgs().Contains("-d") || Environment.GetCommandLineArgs().Contains("--debug") || Environment.GetCommandLineArgs().Contains("-v") || Environment.GetCommandLineArgs().Contains("--verbose"))
            {
                Write(String.Format("{0:G} [DEBUG] {1}", DateTime.Now, message));
            }
        }

        // write a message using the INFO tag
        public static void Info(string message)
        {
            Write(String.Format("{0:G} [INFO] {1}", DateTime.Now, message));
        }

        // write a message using the WARN tag
        public static void Warn(string message)
        {
            Write(String.Format("{0:G} [WARN] {1}", DateTime.Now, message));
        }

        // write a message using the ERROR tag
        public static void Error(string message)
        {
            Write(String.Format("{0:G} [ERROR] {1}", DateTime.Now, message));
        }

        // write a message using the FATAL tag
        public static void Fatal(string message)
        {
            Write(String.Format("{0:G} [FATAL] {1}", DateTime.Now, message));
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
        public static void Write(string text)
        {
            Console.WriteLine(text);

            try
            {
                if(Directory.Exists(Directory.GetCurrentDirectory() + @"\Logs"))
                {
                    Directory.CreateDirectory(Directory.GetCurrentDirectory() + @"\Logs\LoL Auto Login Logs");
                    using (StreamWriter sw = new StreamWriter(Directory.GetCurrentDirectory() + @"\Logs\LoL Auto Login Logs\" + logFile, true))
                        sw.WriteLine(text);
                }
                else
                {
                    using (StreamWriter sw = new StreamWriter(Directory.GetCurrentDirectory() + @"\lolautologin.log", true))
                        sw.WriteLine(text);
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.StackTrace);
            }
        }
    }
}
