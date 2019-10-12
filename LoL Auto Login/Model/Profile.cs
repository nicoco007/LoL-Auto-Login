using LoLAutoLogin.Utility;
using System.ComponentModel;

namespace LoLAutoLogin.Model
{
    public class Profile
    {
        [ReadOnly(true)]
        public string Username { get; set; }

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
    }
}
