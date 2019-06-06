using LoLAutoLogin.Utility;
using System;
using System.ComponentModel;

namespace LoLAutoLogin
{
    public class Profile
    {
        public string Username { get; private set; }

        [Browsable(false)]
        public byte[] EncryptedPassword { get; private set; }

        public Profile(string username, string password)
        {
            Username = username;

            EncryptPassword(password);
        }

        public Profile(string username, byte[] encryptedPassword)
        {
            Username = username;
            EncryptedPassword = encryptedPassword;
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
