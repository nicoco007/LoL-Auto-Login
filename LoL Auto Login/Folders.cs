using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoLAutoLogin
{
    internal static class Folders
    {
        public static DirectoryInfo Configuration
        {
            get
            {
                return new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), "Config"));
            }
        }

        public static DirectoryInfo Logs
        {
            get
            {
                return new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), "Logs", "LoL Auto Login Logs"));
            }
        }

        public static DirectoryInfo Debug
        {
            get
            {
                return new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), "Debug"));
            }
        }
    }
}
