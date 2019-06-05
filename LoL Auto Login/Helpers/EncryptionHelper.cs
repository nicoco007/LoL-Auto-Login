using System.Security.Cryptography;
using System.Text;

namespace LoLAutoLogin.Utility
{
    public static class EncryptionHelper
    {
        /// <summary>
        /// Encrypt a string.
        /// </summary>
        /// <param name="input">Input string.</param>
        /// <returns>A byte array containing the encrypted string.</returns>
        public static byte[] Encrypt(string input, DataProtectionScope scope = DataProtectionScope.CurrentUser)
        {
            return ProtectedData.Protect(Encoding.UTF8.GetBytes(input), null, scope);
        }

        /// <summary>
        /// Decrypt a string.
        /// </summary>
        /// <param name="input">A byte array containing an encrypted string.</param>
        /// <returns>The original decrypted string.</returns>
        public static string Decrypt(byte[] input, DataProtectionScope scope = DataProtectionScope.CurrentUser)
        {
            return Encoding.UTF8.GetString(ProtectedData.Unprotect(input, null, scope));
        }
    }
}
