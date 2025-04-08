using System;
using System.Security.Cryptography;
using System.Text;
using System.Management;

namespace ToolAutoLinks
{
    public static class MachineIDGenerator
    {
        public static string GetMachineID()
        {
            string cpuId = GetHardwareInfo("Win32_Processor", "ProcessorId");
            string hddSerial = GetHardwareInfo("Win32_DiskDrive", "SerialNumber");

            string rawData = cpuId + hddSerial;
            return GenerateSHA256(rawData);
        }

        private static string GetHardwareInfo(string wmiClass, string wmiProperty)
        {
            try
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher($"SELECT {wmiProperty} FROM {wmiClass}");
                foreach (ManagementObject obj in searcher.Get())
                {
                    return obj[wmiProperty]?.ToString().Trim();
                }
            }
            catch { }
            return "Unknown";
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
