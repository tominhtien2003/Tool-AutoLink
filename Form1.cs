using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ToolAutoLinks
{
    public partial class Form1 : Form
    {
        private static string LinksFile = Path.Combine(Application.StartupPath, "Links.txt");
        private static string profilePath = Path.Combine(Application.StartupPath, "UserProfile", "MainUser");
        private static string markerFile = Path.Combine(profilePath, "login.ok");
        private bool isLoggedIn;
        private bool isPaused = false;
        private CancellationTokenSource cts;
        public Form1()
        {
            InitializeComponent();
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }
        private void LoadLinksFromFile()
        {
            if (!File.Exists(LinksFile))
            {
                File.WriteAllText(LinksFile, "");
            }

            string[] links = File.ReadAllLines(LinksFile);

            foreach (var link in links)
            {
                int rowIndex = dataGridView1.Rows.Add();
                dataGridView1.Rows[rowIndex].Cells["STT"].Value = rowIndex + 1;
                dataGridView1.Rows[rowIndex].Cells["Link"].Value = link;
                dataGridView1.Rows[rowIndex].Cells["Copy"].Value = "Copy";
                dataGridView1.Rows[rowIndex].Cells["State"].Value = "";
            }
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            SetWidthAllColumnDatagridview();
            LoadLinksFromFile();
            SetexpiryDate();
        }
        private void SetexpiryDate()
        {
            string licenseKey = ActivationManager.GetSavedLicenseKey();
            string[] keyParts = licenseKey.Split('|');
            if (DateTime.TryParse(keyParts[1], out DateTime expiryDate))
            {
                label2.Text = expiryDate.ToString("dd/MM/yyyy");
            }
        }
        private void SaveAllLinksToFile()
        {
            List<string> links = new List<string>();

            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.Cells["Link"].Value != null)
                {
                    links.Add(row.Cells["Link"].Value.ToString());
                }
            }

            File.WriteAllLines(LinksFile, links);
        }
        private void SetWidthAllColumnDatagridview()
        {
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridView1.Columns[0].FillWeight = 5;
            dataGridView1.Columns[1].FillWeight = 5;
            dataGridView1.Columns[2].FillWeight = 75;
            dataGridView1.Columns[3].FillWeight = 5;
            dataGridView1.Columns[4].FillWeight = 10;
            dataGridView1.RowTemplate.Height = 24;
        }

        private void dataGridView1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == System.Windows.Forms.Keys.V)
            {
                if (Clipboard.ContainsText())
                {
                    string clipboardText = Clipboard.GetText();
                    string[] lines = clipboardText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (string line in lines)
                    {
                        if (line.StartsWith("https://g4b.giftee.biz/giftee_boxes"))
                        {
                            int rowIndex = dataGridView1.Rows.Add();
                            dataGridView1.Rows[rowIndex].Cells["STT"].Value = rowIndex + 1;
                            dataGridView1.Rows[rowIndex].Cells["Link"].Value = line;
                            dataGridView1.Rows[rowIndex].Cells["Copy"].Value = "Copy";
                            dataGridView1.Rows[rowIndex].Cells["State"].Value = "";
                        }
                    }

                    e.Handled = true;
                }
            }
            SaveAllLinksToFile();
        }
        bool IsDriverAlive(IWebDriver driver)
        {
            try
            {
                var _ = driver.WindowHandles;
                return true;
            }
            catch (WebDriverException)
            {
                return false;
            }
        }
        private async void btnPlay_Click(object sender, EventArgs e)
        {
            btnPlay.Enabled = false;
            cts = new CancellationTokenSource();
            int windowWidth = 400;
            int windowHeight = 600;

            //if (!Directory.Exists(profilePath))
            //{
            //    Directory.CreateDirectory(profilePath);
            //}
            //isLoggedIn = File.Exists(markerFile);
            await Task.Run(async () =>
            {
                using (var chromeDriverService = ChromeDriverService.CreateDefaultService())
                {
                    chromeDriverService.HideCommandPromptWindow = true;
                    var options = new ChromeOptions();
                    //options.AddArgument($"--user-data-dir={profilePath}");
                    options.AddArgument($"--window-size={windowWidth},{windowHeight}");
                    using (IWebDriver driver = new ChromeDriver(chromeDriverService, options))
                    {
                        WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(30));

                        foreach (DataGridViewRow row in dataGridView1.Rows)
                        {
                            if (cts.Token.IsCancellationRequested || !IsDriverAlive(driver))
                            {
                                btnPlay.Enabled = true;
                                break;
                            }
                            while (isPaused)
                            {
                                await Task.Delay(500);
                                if (cts.Token.IsCancellationRequested)
                                    break;
                            }

                            bool isChecked = row.Cells["CheckBox"].Value is bool value && value;
                            if (!isChecked) continue;

                            SetRowState(row, "Loading", Color.Pink);

                            string url = row.Cells["Link"].Value?.ToString();

                            try
                            {
                                ProcessUrl(driver, wait, row, url);
                            }
                            catch (Exception ex)
                            {
                                SetRowState(row, "Error", Color.Red);
                                Console.WriteLine($"Error: {ex.Message}");
                                break;
                            }
                            dataGridView1.Rows[row.Index].Cells["CheckBox"].Value = false;
                        }

                        try
                        {
                            driver.Quit();
                        }
                        catch { }
                    }
                }
            }, cts.Token);
            checkBoxAll.Checked = false;
            MessageBox.Show("Đã ting ting", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            btnPlay.Enabled = true;
        }
        private void ProcessUrl(IWebDriver driver, WebDriverWait wait, DataGridViewRow row, string url)
        {
            driver.Navigate().GoToUrl(url);
            wait.Until(d => ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState").Equals("complete"));
            Thread.Sleep(500);
            if (driver.Url.Contains("gifts"))
            {
                SetRowState(row, "Received", Color.Yellow);
                return;
            }
            if (IsSameUrl(driver.Url, url))
            {
                Thread.Sleep(6500);
                var checkbox = driver.FindElement(By.XPath(
                "//*[@id=\"root\"]/div[2]/div/div/div[2]/div/div[1]/span[1]/input"));
                if (!checkbox.Selected)
                    checkbox.Click();
                if (checkbox.Selected)
                {
                    ClickElement(driver, wait, "//*[@id=\"root\"]/div[2]/div/div/div[2]/div/button");

                    wait.Until(ExpectedConditions.UrlContains("home"));
                }
            }
            Thread.Sleep(1000);
            string urlHome = driver.Url;
            if (urlHome.Contains("home"))
            {
                bool clicked = false;
                var images = driver.FindElements(By.TagName("img"));

                foreach (var img in images)
                {
                    string alt = img.GetAttribute("alt")?.ToLower();

                    int width = Convert.ToInt32(((IJavaScriptExecutor)driver).ExecuteScript("return arguments[0].clientWidth;", img));
                    int height = Convert.ToInt32(((IJavaScriptExecutor)driver).ExecuteScript("return arguments[0].clientHeight;", img));

                    Console.WriteLine($"Size: {width}x{height} - ALT: {alt}");
                    bool isApproxSize = Math.Abs(width - 343) <= 5 && Math.Abs(height - 98) <= 5;

                    bool isDescriptiveAlt = alt != null && (
                        alt.Contains("えらべる") ||
                        alt.Contains("pay") ||
                        alt.Contains("合算") ||
                        alt.Contains("プレゼント") ||
                        alt.Contains("キャンペーン")
                    );

                    if (isApproxSize && isDescriptiveAlt)
                    {
                        ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].style.border='3px solid green'", img);

                        img.Click();
                        clicked = true;
                        break;
                    }
                }
                if (!clicked)
                {
                    SetRowState(row, "Error", Color.Red);
                    return;
                }
                Thread.Sleep(100);
                SwitchToLastTab(driver);

                wait.Until(ExpectedConditions.UrlContains("gift-wallet.app"));
                ClickElement(driver, wait, "//*[@id=\"root\"]/div/div/div/div/section[1]/div/div/div[3]/div/div/div[2]/button");

                wait.Until(ExpectedConditions.UrlContains("access.line.me"));
                PerformLineLogin(driver, wait);

                WaitForCompletionPage(driver, row);
            }
        }
        private bool IsSameUrl(string expected, string actual)
        {
            return expected.Trim().TrimEnd('/').Equals(actual.Trim().TrimEnd('/'), StringComparison.OrdinalIgnoreCase);
        }
        private void ClickElement(IWebDriver driver, WebDriverWait wait, string xpath)
        {
            var element = wait.Until(ExpectedConditions.ElementToBeClickable(By.XPath(xpath)));
            ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView(true);", element);
            Thread.Sleep(100);
            try
            {
                element.Click();
            }
            catch (ElementClickInterceptedException)
            {
                ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", element);
            }
        }
        private void SwitchToLastTab(IWebDriver driver)
        {
            var tabs = driver.WindowHandles;
            if (tabs.Count > 1)
            {
                driver.SwitchTo().Window(tabs[tabs.Count - 2]);
                driver.Close();
            }

            driver.SwitchTo().Window(tabs.Last());
        }

        private void PerformLineLogin(IWebDriver driver, WebDriverWait wait)
        {
            if (!isLoggedIn)
            {
                ClickElement(driver, wait, "//*[@id=\"app\"]/div/div/div/div[2]/div/div[2]/a");
            }
            else
            {
                ClickElement(driver, wait, "//*[@id=\"app\"]/div/div/div/div/div/div[2]/div/div[3]/button");
            }
        }

        private void WaitForCompletionPage(IWebDriver driver, DataGridViewRow row)
        {
            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromMinutes(1));
            try
            {
                wait.Until(d => IsOauthPage(d.Url) || IsGiftPage(d.Url) || IsCompletionOrHome(d.Url));

                string url = driver.Url;

                if (IsCompletionOrHome(url))
                {
                    HandleCompletion(row, url);
                    return;
                }

                if (IsOauthPage(url))
                {
                    bool movedFromOauth = WaitForNextPage(driver, "oauth", out url);
                    if (!movedFromOauth)
                    {
                        SetRowState(row, "Received", Color.Yellow);
                        return;
                    }

                    if (IsGiftPage(url))
                    {
                        bool movedFromGift = WaitForNextPage(driver, "gifts", out url);
                        if (!movedFromGift)
                        {
                            SetRowState(row, "Received", Color.Yellow);
                            return;
                        }
                        HandleCompletion(row, url);
                        return;
                    }

                    if (IsCompletionOrHome(url))
                    {
                        HandleCompletion(row, url);
                        return;
                    }
                }

                if (IsGiftPage(url))
                {
                    bool movedFromGift = WaitForNextPage(driver, "gifts", out url);
                    if (!movedFromGift)
                    {
                        SetRowState(row, "Received", Color.Yellow);
                        return;
                    }

                    HandleCompletion(row, url);
                    return;
                }
            }
            catch (WebDriverTimeoutException)
            {
                SetRowState(row, "Error", Color.Red);
            }
        }
        private bool WaitForNextPage(IWebDriver driver, string currentPageKeyword, out string nextUrl)
        {
            WebDriverWait shortWait = new WebDriverWait(driver, TimeSpan.FromSeconds(15));
            try
            {
                shortWait.Until(d => !d.Url.Contains(currentPageKeyword));
                nextUrl = driver.Url;
                return true;
            }
            catch (WebDriverTimeoutException)
            {
                nextUrl = driver.Url;
                return false;
            }
        }
        private bool IsOauthPage(string url)
            => url.StartsWith("https://www.gift-wallet.app/oauth");

        private bool IsGiftPage(string url)
            => url.StartsWith("https://www.gift-wallet.app/gifts");

        private bool IsCompletionOrHome(string url)
            => url.Contains("point/charge/completion") || url.StartsWith("https://www.gift-wallet.app/home");

        private void HandleCompletion(DataGridViewRow row, string url)
        {
            if (url.Contains("point/charge/completion"))
            {
                SetRowState(row, "Success", Color.Green);
                if (!isLoggedIn)
                {
                    File.WriteAllText(markerFile, DateTime.Now.ToString());
                    isLoggedIn = true;
                }
            }
            else if (url.StartsWith("https://www.gift-wallet.app/home"))
            {
                SetRowState(row, "Received", Color.Yellow);
            }
        }
        private void SetRowState(DataGridViewRow row, string state, Color color)
        {
            row.Cells["State"].Value = state;
            row.Cells["State"].Style.ForeColor = color;
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            DialogResult result = MessageBox.Show(
                "Bạn có muốn thoát tool?",
                "Thông báo",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
                );
            if (result == DialogResult.Yes)
            {
                e.Cancel = false;
            }
            else
            {
                e.Cancel = true;
            }
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex >= 0 && e.RowIndex >= 0)
            {
                if (dataGridView1.Columns[e.ColumnIndex].Name == "Copy")
                {
                    string linkText = dataGridView1.Rows[e.RowIndex].Cells["Link"].Value?.ToString();

                    if (!string.IsNullOrEmpty(linkText))
                    {
                        Clipboard.SetText(linkText);
                        MessageBox.Show("Đã sao chép: " + linkText, "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show("Không có link để sao chép!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
        }
        private void checkBoxAll_CheckedChanged(object sender, EventArgs e)
        {
            bool isChecked = checkBoxAll.Checked;
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                row.Cells["CheckBox"].Value = isChecked;
            }
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {

        }
        private void button1_Click(object sender, EventArgs e)
        {
            List<string> remainingLinks = new List<string>();
            DialogResult result = MessageBox.Show("Bạn có chắc chắn muốn xóa các hàng đã chọn không?", "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                for (int i = dataGridView1.Rows.Count - 1; i >= 0; i--)
                {
                    var cell = dataGridView1.Rows[i].Cells["CheckBox"];

                    if (cell is DataGridViewCheckBoxCell && cell.Value != null && bool.TryParse(cell.Value.ToString(), out bool isChecked) && isChecked)
                    {
                        dataGridView1.Rows.RemoveAt(i);
                    }
                }

                for (int i = 0; i < dataGridView1.Rows.Count; i++)
                {
                    dataGridView1.Rows[i].Cells["STT"].Value = i + 1;
                    remainingLinks.Add(dataGridView1.Rows[i].Cells["Link"].Value.ToString());
                }
                File.WriteAllLines(LinksFile, remainingLinks);
                MessageBox.Show("Đã xóa các hàng được chọn và cập nhật lại STT!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            checkBoxAll.Checked = false;
        }
        private void button2_Click(object sender, EventArgs e)
        {
            button2.Enabled = false;
            isPaused = true;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            button2.Enabled = true;
            isPaused = false;
        }

        private void dataGridView1_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {

        }
        private void dataGridView1_MouseDown(object sender, MouseEventArgs e)
        {

            if (e.Button == MouseButtons.Right)
            {
                if (Clipboard.ContainsText())
                {
                    string clipboardText = Clipboard.GetText();
                    string[] lines = clipboardText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (string line in lines)
                    {
                        if (line.StartsWith("https://g4b.giftee.biz/giftee_boxes"))
                        {
                            int rowIndex = dataGridView1.Rows.Add();
                            dataGridView1.Rows[rowIndex].Cells["STT"].Value = rowIndex + 1;
                            dataGridView1.Rows[rowIndex].Cells["Link"].Value = line;
                            dataGridView1.Rows[rowIndex].Cells["Copy"].Value = "Copy";
                            dataGridView1.Rows[rowIndex].Cells["State"].Value = "";
                        }
                    }
                }
            }
            SaveAllLinksToFile();
        }
        private void button4_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.Cells["State"].Value != null && row.Cells["State"].Value.ToString().StartsWith("Error"))
                {
                    bool isChecked = row.Cells["CheckBox"].Value != null && (bool)row.Cells["CheckBox"].Value;
                    row.Cells["CheckBox"].Value = !isChecked;
                }
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (!checkBox1.Checked)
                return;

            DialogResult result = MessageBox.Show(
                "Bạn có chắc chắn muốn xoá tài khoản đang đăng nhập không?\nThao tác này sẽ xoá toàn bộ dữ liệu đăng nhập.",
                "Xác nhận xoá tài khoản",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning
            );

            if (result == DialogResult.Yes)
            {
                try
                {
                    if (Directory.Exists(profilePath))
                    {
                        Directory.Delete(profilePath, true);
                        MessageBox.Show("✅ Đã xoá toàn bộ dữ liệu đăng nhập!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show("Không tìm thấy dữ liệu đăng nhập để xoá.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi khi xoá dữ liệu: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            checkBox1.CheckedChanged -= checkBox1_CheckedChanged; // Ngắt sự kiện để tránh lặp
            checkBox1.Checked = false;
            checkBox1.CheckedChanged += checkBox1_CheckedChanged; // Gắn lại
        }
    }
}