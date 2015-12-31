using System.Text;
using System.Security.Cryptography;

namespace LoLAutoLogin
{

    public class Encryption
    {

        // TODO: Find a way to make this unique to every user
        private static byte[] entropy = { 60, 87, 98, 89, 28, 77, 99, 62, 3, 62, 1, 39, 23, 34, 18, 47, 27, 3, 21, 7, 91, 71, 72, 60, 48, 64, 65, 62, 70, 71, 53, 16, 72, 38, 83, 41, 37, 17, 5, 8, 43, 92, 10, 76, 47, 47, 2, 20, 96, 36, 93, 1, 40, 90, 72, 41, 79, 92, 87, 9, 40, 27, 48, 98, 79, 76, 46, 29, 93, 80, 52, 86, 49, 46, 67, 90, 70, 16, 36, 50, 96, 31, 56, 63, 80, 80, 84, 28, 77, 63, 42, 95, 88, 25, 30, 90, 18, 53, 42, 20 };

        public static byte[] Encrypt(string input)
        {

            try
            {

                return ProtectedData.Protect(Encoding.ASCII.GetBytes(input), entropy, DataProtectionScope.CurrentUser);

            }
            catch (CryptographicException ex)
            {

                Log.Fatal("Data could not be encrypted.");
                Log.Fatal(ex.Message);
                Log.PrintStackTrace(ex.StackTrace);

                return null;

            }

        }

        public static string Decrypt(byte[] input)
        {

            try
            {

                return Encoding.ASCII.GetString(ProtectedData.Unprotect(input, entropy, DataProtectionScope.CurrentUser));

            }
            catch (CryptographicException ex)
            {

                Log.Fatal("Data could not be decrypted.");
                Log.Fatal(ex.Message);
                Log.PrintStackTrace(ex.StackTrace);

                return null;

            }

        }

    }

}
