using LoLAutoLogin.Utility;
using System;

namespace LoLAutoLogin
{
    public class Profile
    {
        public string Username { get; set; }
        public byte[] EncryptedPassword { get; set; }
        public DateTime LastUsed { get; set; }

        public string DecryptPassword()
        {
            return EncryptionHelper.Decrypt(EncryptedPassword);
        }

        public void SavePassword(string password)
        {
            EncryptedPassword = EncryptionHelper.Encrypt(password);
        }
    }
}
