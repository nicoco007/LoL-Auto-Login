using LoLAutoLogin.Utility;
using System.ComponentModel;

namespace LoLAutoLogin.Model
{
    public class Profile
    {
        public string Username { get; }

        [DisplayName("Default")]
        public bool IsDefault { get; set; }

        [Browsable(false)]
        public string Password
        {
            get => EncryptionHelper.Decrypt(EncryptedPassword);
            set => EncryptedPassword = EncryptionHelper.Encrypt(value);
        }
        
        [Browsable(false)]
        public byte[] EncryptedPassword { get; set; }

        public Profile(string username, string password)
        {
            Username = username;
            Password = password;
        }

        public Profile(string username, byte[] encryptedPassword, bool isDefault)
        {
            Username = username;
            EncryptedPassword = encryptedPassword;
            IsDefault = isDefault;
        }
    }
}
