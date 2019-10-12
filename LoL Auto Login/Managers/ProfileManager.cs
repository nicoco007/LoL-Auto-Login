using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LoLAutoLogin.Model;

namespace LoLAutoLogin.Managers
{
    public static class ProfileManager
    {
        public static readonly string ProfilesDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LoL Auto Login");
        public static readonly string ProfilesFilePath = Path.Combine(ProfilesDirectory, "profiles");
        public static readonly byte[] Magic = { 0x15, 0x1b, 0xab, 0x76 };

        public static bool ProfilesFileExists => File.Exists(ProfilesFilePath);
        public static IReadOnlyList<Profile> Profiles => profiles;

        private static List<Profile> profiles;

        public static void LoadProfiles()
        {
            profiles = new List<Profile>();

            if (!File.Exists(ProfilesFilePath)) return;

            using (FileStream fileStream = new FileStream(ProfilesFilePath, FileMode.Open, FileAccess.Read))
            {
                if (fileStream.Length == 0) return;

                using (BinaryReader reader = new BinaryReader(fileStream))
                {
                    if (!reader.ReadBytes(Magic.Length).SequenceEqual(Magic))
                    {
                        throw new IOException("Unknown file format");
                    }

                    if (reader.ReadByte() != 0x01)
                    {
                        throw new IOException("Unknown file version");
                    }

                    byte defaultIndex = reader.ReadByte();
                    byte count = reader.ReadByte();

                    for (byte i = 0; i < count; i++)
                    {
                        profiles.Add(new Profile(
                            reader.ReadString(),
                            reader.ReadBytes(reader.ReadInt32()),
                            i == defaultIndex
                        ));
                    }

                    if (profiles.Count > 0 && !profiles.Exists(p => p.IsDefault))
                    {
                        profiles[0].IsDefault = true;
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

            if (!Directory.Exists(ProfilesDirectory)) Directory.CreateDirectory(ProfilesDirectory);

            using (FileStream fileStream = new FileStream(ProfilesFilePath, FileMode.OpenOrCreate, FileAccess.Write))
            {
                using (BinaryWriter writer = new BinaryWriter(fileStream))
                {
                    writer.Write(Magic);
                    
                    // file version
                    writer.Write((byte)0x01);
                    writer.Write((byte)profiles.FindIndex(p => p.IsDefault));
                    writer.Write((byte)profiles.Count);

                    foreach (Profile profile in profiles)
                    {
                        writer.Write(profile.Username);
                        writer.Write(profile.EncryptedPassword.Length);
                        writer.Write(profile.EncryptedPassword);
                    }
                }
            }
        }

        public static bool HasProfiles()
        {
            return profiles.Count > 0;
        }

        public static Profile GetDefaultProfile()
        {
            return profiles.FirstOrDefault(p => p.IsDefault);
        }

        public static void AddProfile(Profile profile)
        {
            profile.IsDefault = !profiles.Exists(p => p.IsDefault);
            profiles.Add(profile);
        }

        public static void DeleteProfile(int profileIndex)
        {
            profiles.RemoveAt(profileIndex);
        }
    }
}
