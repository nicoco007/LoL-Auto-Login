using LoLAutoLogin.Utility;
using System;

namespace LoLAutoLogin
{
    public class Profile
    {
        public string Username { get; private set; }
        public byte[] EncryptedPassword { get; private set; }
        public DateTime LastUsed { get; private set; }

        public Profile(string username, string password, DateTime lastUsed)
        {
            Username = username;
            LastUsed = lastUsed;

            EncryptPassword(password);
        }

        public Profile(string username, byte[] encryptedPassword, DateTime lastUsed)
        {
            Username = username;
            EncryptedPassword = encryptedPassword;
            LastUsed = lastUsed;
        }

        public void EncryptPassword(string password)
        {
            EncryptedPassword = EncryptionHelper.Encrypt(password);
        }

        public string DecryptPassword()
        {
            return EncryptionHelper.Decrypt(EncryptedPassword);
        }
    }
}
