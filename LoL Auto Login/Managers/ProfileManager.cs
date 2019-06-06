using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LoLAutoLogin.Managers
{
    public static class ProfileManager
    {
        private static List<Profile> profiles;

        private static readonly string PROFILES_DIRECTORY = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LoL Auto Login");
        private static readonly string PROFILES_FILE_PATH = Path.Combine(PROFILES_DIRECTORY, "profiles");
        private static readonly byte[] MAGIC = new byte[] { 0x15, 0x1b, 0xab, 0x76 };

        public static void LoadProfiles()
        {
            profiles = new List<Profile>();

            if (!File.Exists(PROFILES_FILE_PATH)) return;

            using (FileStream fileStream = new FileStream(PROFILES_FILE_PATH, FileMode.Open, FileAccess.Read))
            {
                if (fileStream.Length == 0) return;

                using (BinaryReader reader = new BinaryReader(fileStream))
                {
                    if (!reader.ReadBytes(MAGIC.Length).SequenceEqual(MAGIC))
                    {
                        throw new IOException("Unknown file format");
                    }

                    if (reader.ReadByte() != 0x01)
                    {
                        throw new IOException("Unknown file version");
                    }

                    byte count = reader.ReadByte();

                    for (byte i = 0; i < count; i++)
                    {
                        profiles.Add(new Profile(
                            reader.ReadString(),
                            reader.ReadBytes(reader.ReadInt32()),
                            new DateTime(reader.ReadInt64())
                        ));
                    }
                }
            }
        }

        public static void SaveProfiles()
        {
            if (profiles.Count > sbyte.MaxValue)
            {
                throw new InvalidOperationException($"Profile count cannot be over {sbyte.MaxValue}!");
            }

            if (!Directory.Exists(PROFILES_DIRECTORY)) Directory.CreateDirectory(PROFILES_DIRECTORY);

            using (FileStream fileStream = new FileStream(PROFILES_FILE_PATH, FileMode.OpenOrCreate, FileAccess.Write))
            {
                using (BinaryWriter writer = new BinaryWriter(fileStream))
                {
                    writer.Write(MAGIC);
                    
                    // file version
                    writer.Write(new byte[] { 0x01 });

                    writer.Write((byte)profiles.Count);

                    foreach (Profile profile in profiles)
                    {
                        writer.Write(profile.Username);
                        writer.Write(profile.EncryptedPassword.Length);
                        writer.Write(profile.EncryptedPassword);
                        writer.Write(profile.LastUsed.Ticks);
                    }
                }
            }
        }

        public static bool HasProfiles()
        {
            return profiles.Count > 0;
        }

        public static IReadOnlyList<Profile> GetProfiles()
        {
            return profiles.AsReadOnly();
        }

        public static Profile GetDefaultProfile()
        {
            return profiles[0];
        }
    }
}
