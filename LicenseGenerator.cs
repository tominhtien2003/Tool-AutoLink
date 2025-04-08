using System;
using System.Security.Cryptography;
using System.Text;

namespace ToolAutoLinks
{
    public class LicenseGenerator
    {
        private const string SecretKey = "MySecretKey";

        public static string GenerateLicenseKey(string machineID, DateTime expiryDate)
        {
            string rawData = $"{machineID}|{expiryDate:yyyy-MM-dd}|{SecretKey}";
            string hash = GenerateSHA256(rawData);
            return $"{hash}|{expiryDate:yyyy-MM-dd}";
        }

        private static string GenerateSHA256(string input)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
                StringBuilder sb = new StringBuilder();
                foreach (byte b in bytes) sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }
    }
}
