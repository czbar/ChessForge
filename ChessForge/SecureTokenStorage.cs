using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows;

namespace ChessForge
{
    /// <summary>
    /// Manages Lichess API access token.
    /// </summary>
    public class SecureTokenStore
    {
        /// <summary>
        /// Path to the file where the token will be stored securely. 
        /// The file is stored in the user's Application Data folder and is named "lichess_token.dat".
        /// </summary>
        private static readonly string _path =
            Path.Combine(Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData),
                "lichess_token.dat");

        /// <summary>
        /// Saves the token securely using Windows Data Protection API (DPAPI).
        /// </summary>
        /// <param name="token"></param>
        public static void Save(string token)
        {
            try
            {
                var encrypted = ProtectedData.Protect(
                    Encoding.UTF8.GetBytes(token),
                    null,
                    DataProtectionScope.CurrentUser);

                File.WriteAllBytes(_path, encrypted);

                Configuration.LichessAuthToken = "";
                Configuration.LichessAuthTokenSavedInConfig = false;
            }
            catch
            {
                Configuration.LichessAuthToken = token;
                Configuration.LichessAuthTokenSavedInConfig = true;
            }
        }

        /// <summary>
        /// Loads the token securely using Windows Data Protection API (DPAPI).
        /// </summary>
        /// <returns></returns>
        public static string Load()
        {
            try
            {
                if (!File.Exists(_path))
                    return "";

                var encrypted = File.ReadAllBytes(_path);

                var decrypted = ProtectedData.Unprotect(
                    encrypted,
                    null,
                    DataProtectionScope.CurrentUser);

                return Encoding.UTF8.GetString(decrypted);
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        /// Clears the stored token by deleting the file.
        /// </summary>
        public static void Clear()
        {
            if (File.Exists(_path))
            {
                File.Delete(_path);
            }
        }
    }
}