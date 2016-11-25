using System.Text;
using System.Security.Cryptography;

/// Copyright © 2015-2016 nicoco007
///
/// Licensed under the Apache License, Version 2.0 (the "License");
/// you may not use this file except in compliance with the License.
/// You may obtain a copy of the License at
///
///     http://www.apache.org/licenses/LICENSE-2.0
///
/// Unless required by applicable law or agreed to in writing, software
/// distributed under the License is distributed on an "AS IS" BASIS,
/// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
/// See the License for the specific language governing permissions and
/// limitations under the License.
namespace LoLAutoLogin
{

    /// <summary>
    /// Uses the DPAPI to encrypt and decrypt strings.
    /// </summary>
    public class Encryption
    {

        // TODO: Find a way to make this unique to every user
        private static readonly byte[] Entropy = { 60, 87, 98, 89, 28, 77, 99, 62, 3, 62, 1, 39, 23, 34, 18, 47, 27, 3, 21, 7, 91, 71, 72, 60, 48, 64, 65, 62, 70, 71, 53, 16, 72, 38, 83, 41, 37, 17, 5, 8, 43, 92, 10, 76, 47, 47, 2, 20, 96, 36, 93, 1, 40, 90, 72, 41, 79, 92, 87, 9, 40, 27, 48, 98, 79, 76, 46, 29, 93, 80, 52, 86, 49, 46, 67, 90, 70, 16, 36, 50, 96, 31, 56, 63, 80, 80, 84, 28, 77, 63, 42, 95, 88, 25, 30, 90, 18, 53, 42, 20 };

        /// <summary>
        /// Encrypt a string.
        /// </summary>
        /// <param name="input">Input string.</param>
        /// <returns>A byte array containing the encrypted string.</returns>
        public static byte[] Encrypt(string input)
        {

            // return byte array containing encrypted input, and protect for current user only
            // (so no one on the computer can decrypt it except the current user).
            return ProtectedData.Protect(Encoding.UTF8.GetBytes(input), Entropy, DataProtectionScope.CurrentUser);

        }

        /// <summary>
        /// Decrypt a string.
        /// </summary>
        /// <param name="input">A byte array containing an encrypted string.</param>
        /// <returns>The original decrypted string.</returns>
        public static string Decrypt(byte[] input)
        {

            // Decrypt input
            return Encoding.UTF8.GetString(ProtectedData.Unprotect(input, Entropy, DataProtectionScope.CurrentUser));

        }

    }

}
