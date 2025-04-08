using System;
using System.IO;

namespace ToolAutoLinks
{
    internal class ActivationManager
    {
        private static string activationFile = @"C:\ProgramData\MyTool\activation.dat";

        public static void SaveLicenseKey(string licenseKey)
        {
            CleanupExpiredKeys();
            string directory = Path.GetDirectoryName(activationFile);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            File.WriteAllText(activationFile, licenseKey);
        }
        public static string GetSavedLicenseKey()
        {
            if (!File.Exists(activationFile))
                return null;

            string savedKey = File.ReadAllText(activationFile).Trim();
            string machineID = MachineIDGenerator.GetMachineID();

            return LicenseValidator.ValidateLicense(machineID, savedKey, out DateTime expiryDate) && expiryDate > DateTime.Now
                ? savedKey
                : null;
        }
        public static void CleanupExpiredKeys()
        {
            if (!File.Exists(activationFile))
                return;

            string savedKey = File.ReadAllText(activationFile).Trim();
            string machineID = MachineIDGenerator.GetMachineID();

            if (!LicenseValidator.ValidateLicense(machineID, savedKey, out DateTime expiryDate) || expiryDate <= DateTime.Now)
            {
                File.WriteAllText(activationFile, ""); 
            }
        }
    }
}
