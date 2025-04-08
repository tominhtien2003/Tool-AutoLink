using System;

namespace ToolAutoLinks
{
    public class LicenseValidator
    {
        private const string SecretKey = "MySecretKey"; 

        public static bool ValidateLicense(string machineID, string licenseKey, out DateTime expiryDate)
        {
            expiryDate = DateTime.MinValue;

            string[] keyParts = licenseKey.Split('|');
            if (keyParts.Length < 2)
                return false;

            if (!DateTime.TryParse(keyParts[1], out expiryDate))
                return false;

            string expectedKey = LicenseGenerator.GenerateLicenseKey(machineID, expiryDate);
            return expectedKey == licenseKey && expiryDate > DateTime.Now;
        }
    }
}
