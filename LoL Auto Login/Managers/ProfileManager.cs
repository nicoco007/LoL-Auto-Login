using System;
using System.Collections.Generic;
using System.IO;

namespace LoLAutoLogin.Managers
{
    public static class ProfileManager
    {
        public static List<Profile> Profiles { get; private set; }

        private static readonly string PROFILES_FILE_PATH = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LoL Auto Login", "profiles");
        private static readonly byte[] MAGIC = new byte[] { 0x10, 0x14 };

        public static void LoadProfiles()
        {
            Profiles = new List<Profile>();

            if (!File.Exists(PROFILES_FILE_PATH)) return;

            FileStream fileStream = new FileStream(PROFILES_FILE_PATH, FileMode.Open, FileAccess.Read);

            if (fileStream.Length == 0) return;

            BinaryReader reader = new BinaryReader(fileStream);

            if (reader.ReadBytes(2) != MAGIC)
            {
                throw new IOException("Unknown file format");
            }

            if (reader.ReadByte() != 0x01)
            {
                throw new IOException("Unknown file version");
            }

            short count = reader.ReadInt16();

            for (int i = 0; i < count; i++)
            {
                Profiles.Add(new Profile()
                {

                });
            }
        }

        public static void SaveProfiles()
        {
            FileStream fileStream = new FileStream(PROFILES_FILE_PATH, FileMode.OpenOrCreate, FileAccess.Write);
            BinaryWriter writer = new BinaryWriter(fileStream);

            writer.Write(MAGIC);

            // file version
            writer.Write(new byte[] { 0x01 });
        }
    }
}
