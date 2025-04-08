using System;
using System.Windows.Forms;

namespace ToolAutoLinks
{
    public partial class Form2 : Form
    {
        public Form2()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string machineID = MachineIDGenerator.GetMachineID();
            Clipboard.SetText(machineID);
            MessageBox.Show("Machine ID đã được sao chép vào clipboard!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        private void button2_Click(object sender, EventArgs e)
        {
            string savedKey = ActivationManager.GetSavedLicenseKey();
            string machineID = MachineIDGenerator.GetMachineID();
            if (!string.IsNullOrEmpty(savedKey) && LicenseValidator.ValidateLicense(machineID, savedKey, out DateTime expiryDate))
            {
                if (expiryDate > DateTime.Now)
                {
                    this.DialogResult = DialogResult.OK; 
                    this.Close();
                }
                else
                {
                    MessageBox.Show("License Key đã hết hạn! Vui lòng nhập key mới.", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }
        private void button3_Click(object sender, EventArgs e)
        {
            string machineID = MachineIDGenerator.GetMachineID();
            string licenseKeyUser = textBox1.Text;
            if (string.IsNullOrEmpty(licenseKeyUser))
            {
                MessageBox.Show("Vui lòng nhập License Key!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (LicenseValidator.ValidateLicense(machineID, licenseKeyUser, out DateTime expiryDate))
            {
                ActivationManager.SaveLicenseKey(licenseKeyUser);
                MessageBox.Show("Kích hoạt thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                button3.Enabled = false;

            }
            else
            {
                MessageBox.Show("License Key không hợp lệ!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void Form2_Load(object sender, EventArgs e)
        {
            string savedKey = ActivationManager.GetSavedLicenseKey();
            string machineID = MachineIDGenerator.GetMachineID();

            if (!string.IsNullOrEmpty(savedKey) && LicenseValidator.ValidateLicense(machineID, savedKey, out DateTime expiryDate))
            {
                button3.Enabled = expiryDate <= DateTime.Now;
            }
        }
    }
}
