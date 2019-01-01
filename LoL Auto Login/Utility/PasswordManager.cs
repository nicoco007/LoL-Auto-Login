// Copyright © 2015-2019 Nicolas Gnyra

// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.

// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see http://www.gnu.org/licenses/.

using System.Text;
using System.Security.Cryptography;
using System.IO;
using System;
using System.Windows.Forms;

namespace LoLAutoLogin
{
    /// <summary>
    /// Uses the DPAPI to encrypt and decrypt strings.
    /// </summary>
    public class PasswordManager
    {
        private static readonly byte[] entropy = { 60, 87, 98, 89, 28, 77, 99, 62, 3, 62, 1, 39, 23, 34, 18, 47, 27, 3, 21, 7, 91, 71, 72, 60, 48, 64, 65, 62, 70, 71, 53, 16, 72, 38, 83, 41, 37, 17, 5, 8, 43, 92, 10, 76, 47, 47, 2, 20, 96, 36, 93, 1, 40, 90, 72, 41, 79, 92, 87, 9, 40, 27, 48, 98, 79, 76, 46, 29, 93, 80, 52, 86, 49, 46, 67, 90, 70, 16, 36, 50, 96, 31, 56, 63, 80, 80, 84, 28, 77, 63, 42, 95, 88, 25, 30, 90, 18, 53, 42, 20 };

        public static string Load()
        {
            string password;

            using (var file = new FileStream("password", FileMode.Open, FileAccess.Read))
            {
                var buffer = new byte[file.Length];
                file.Read(buffer, 0, (int)file.Length);

                password = Decrypt(buffer);
            }

            return password;
        }

        public static void Save(string password)
        {
            Logger.Info("Encrypting & saving password to file");

            try
            {
                using (var file = new FileStream("password", FileMode.OpenOrCreate, FileAccess.Write))
                {
                    var data = Encrypt(password);
                    file.Write(data, 0, data.Length);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Something went wrong when trying to save your password:" + Environment.NewLine + Environment.NewLine + ex.StackTrace, "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                
                Logger.PrintException("Could not save password to file", ex);

                return;
            }
        }

        /// <summary>
        /// Encrypt a string.
        /// </summary>
        /// <param name="input">Input string.</param>
        /// <returns>A byte array containing the encrypted string.</returns>
        private static byte[] Encrypt(string input)
        {
            // return byte array containing encrypted input, and protect for current user only
            // (so no one on the computer can decrypt it except the current user).
            return ProtectedData.Protect(Encoding.UTF8.GetBytes(input), entropy, DataProtectionScope.CurrentUser);
        }

        /// <summary>
        /// Decrypt a string.
        /// </summary>
        /// <param name="input">A byte array containing an encrypted string.</param>
        /// <returns>The original decrypted string.</returns>
        private static string Decrypt(byte[] input)
        {
            // Decrypt input
            return Encoding.UTF8.GetString(ProtectedData.Unprotect(input, entropy, DataProtectionScope.CurrentUser));
        }
    }
}
