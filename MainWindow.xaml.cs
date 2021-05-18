using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Forms;

namespace HearthbuddyHelper
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        [DllImport("kernel32.dll")]
        private static extern int WinExec(string exeName, int operType);
        private static void Delay(int mm)
        {
            DateTime current = DateTime.Now;
            while (current.AddMilliseconds(mm) > DateTime.Now)
            {
                System.Windows.Forms.Application.DoEvents();
            }
        }
        /// <summary>
        /// 选择文件
        /// </summary>
        /// <returns>文件完整路径</returns>
        private static string SelectExeFile(string filter)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog()
            {
                Filter = filter,
                DereferenceLinks = false
            };
            var result = openFileDialog.ShowDialog();
            if (result == true)
            {
                return openFileDialog.FileName;
            }
            else
            {
                return string.Empty;
            }
        }


        private bool IsRunning = false;

        private readonly Timer Timer = new Timer();

        private DateTime LastDateTime = new DateTime();

        private Stopwatch stopwatch = new Stopwatch();

        const int DefaultBNHSInterval = 20;
        const int DefaultHSHBInterval = 60;
        const int DefaultCheckInterval = 5;

        private string BaseDirectory
        {
            get
            {
                return AppDomain.CurrentDomain.BaseDirectory;
            }
        }
        private Process[] BattleNetProcess
        {
            get
            {
                return Process.GetProcessesByName("Battle.net");
            }
        }
        private Process[] HearthstoneProcess
        {
            get
            {
                return Process.GetProcessesByName("Hearthstone");
            }
        }
        private string BattleNetPath
        {
            get
            {
                return BattleNetPathTextBox.Text;
            }
            set
            {
                BattleNetPathTextBox.Text = value;
            }
        }
        private string HearthbuddyPath
        {
            get
            {
                return HearthbuddyPathTextBox.Text;
            }
            set
            {
                HearthbuddyPathTextBox.Text = value;
            }
        }
        
        /// <summary>
        /// 战网——炉石启动时间间隔
        /// </summary>
        private int BNHSInterval 
        { 
            get
            {
                int x;
                return int.TryParse(this.BNHSIntervalTextBox.Text,out x) ? x : DefaultBNHSInterval;
            }
            set
            {
                BNHSIntervalTextBox.Text = value.ToString();
            }
        }

        /// <summary>
        /// 炉石——兄弟启动时间间隔
        /// </summary>
        private int HSHBInterval
        {
            get
            {
                int x;
                return int.TryParse(this.HSHBIntervalTextBox.Text,out x) ? x : DefaultHSHBInterval;
            }
            set
            {
                HSHBIntervalTextBox.Text = value.ToString();
            }
        }

        /// <summary>
        /// 检测时间间隔
        /// </summary>
        private int CheckInterval
        {
            get
            {
                int x;
                return int.TryParse(this.CheckIntervalTextBox.Text,out x) ? x : DefaultCheckInterval;
            }
            set
            {
                CheckIntervalTextBox.Text = value.ToString();
            }
        }

        /// <summary>
        /// 开始时间
        /// </summary>
        private DateTime StartTime
        {
            get
            {
                int x = int.TryParse(this.StartTimeTextBox.Text, out x) ? x : 0;
                x = (x >= 0 && x <= 23) ? x : 0;
                DateTime dateTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, x, 0, 0);
                return dateTime;
            }
            set
            {
                StartTimeTextBox.Text = value.Hour.ToString();
            }
        }

        /// <summary>
        /// 停止时间
        /// </summary>
        private DateTime EndTime
        {
            get
            {
                int x = int.TryParse(this.EndTimeTextBox.Text, out x) ? x : 0;
                x = (x >= 0 && x <= 23) ? x : 0;
                DateTime dateTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, x, 0, 0);
                return dateTime;
            }
            set
            {
                EndTimeTextBox.Text = value.Hour.ToString();
            }
        }





        private void Log(string log)
        {
            this.LogTextBox.AppendText(log + "\n");
            this.LogTextBox.ScrollToEnd();
        }

        private string AutoGetBattleNetPath()
        {
            string BattleNetPath = PathUtil.FindInstallPathFromRegistry("Battle.net");
            if (!string.IsNullOrEmpty(BattleNetPath)
                && Directory.Exists(BattleNetPath)
                && File.Exists(Path.Combine(BattleNetPath, "Battle.net Launcher.exe")))
            {
                Log("自动获取战网路径成功");
                return Path.Combine(BattleNetPath, "Battle.net Launcher.exe");

            }
            Log("自动获取战网路径失败，请手动选择");
            return string.Empty;
        }

        private void StopHearthstone()
        {
            Log("尝试停止战网以及炉石进程");
            try
            {
                Process[] processes = HearthstoneProcess;
                if (processes != null && processes.Length > 0)
                    foreach (Process process in processes)
                    {
                        process.Kill();
                        Delay(1000);
                    }
                processes = BattleNetProcess;
                if (processes != null && processes.Length > 0)
                    foreach (Process process in processes)
                    {
                        process.Kill();
                        Delay(1000);
                    }
                Log("战网和炉石进程已停止");
            }
            catch
            {
            }
        }
        
        private void StartHearthstone()
        {
            //启动战网
            while (BattleNetProcess.Length < 1)
            {
                Log($"未找到战网进程，尝试启动战网({BNHSInterval})");
                WinExec(BattleNetPath, 2);
                Delay(1000 * BNHSInterval);
            }
            Log("战网运行中，检测游戏运行状态");
            //启动炉石
            while (HearthstoneProcess.Length < 1)
            {
                Log($"未找到炉石进程，尝试启动炉石({HSHBInterval})");
                foreach (Process process in BattleNetProcess)
                {
                    Process.Start(process.MainModule.FileName, "--exec=\"launch WTCG\"");
                    break;
                }
                Delay(1000 * HSHBInterval);
            }
            Log("炉石运行中");
        }

        private void StartHearthbuddy()
        {
            Log("尝试启动炉石兄弟");
            Process process = new Process();
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.FileName = HearthbuddyPath;
            process.StartInfo.Arguments = "--autostart --config:Default";
            process.Start();
            Delay(1000);
            Log("炉石兄弟已启动");
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.MessageBox.Show("本程序来自百度【炉石兄弟】吧，仅供学习使用，免费分享，严禁贩卖。");
            Log("■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■");
            Log("■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■");
            Log("使用帮助与常见问题详见：\nhttps://www.wulihub.com.cn/gc/QRw7oB/index.html");
            Log("■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■");
            Log("■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■");
            #region 加载xml配置文件
            XmlConfigUtil util = new XmlConfigUtil("config.xml");

            BattleNetPath = util.Read("BattleNetPath");
            HearthbuddyPath = util.Read("HearthbuddyPath");

            try
            {
                BNHSInterval = int.Parse(util.Read("BNHSInterval"));
                HSHBInterval = int.Parse(util.Read("HSHBInterval"));
                CheckInterval = int.Parse(util.Read("CheckInterval"));
            }
            catch
            {
                Log("读取时间间隔错误，恢复默认值");
                BNHSInterval = DefaultBNHSInterval;
                HSHBInterval = DefaultHSHBInterval;
                CheckInterval = DefaultCheckInterval;
            }

            try
            {
                StartTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, int.Parse(util.Read("StartTime")), 0, 0);
                EndTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, int.Parse(util.Read("EndTime")), 0, 0);
            }
            catch
            {
                Log("读取挂机时间段错误，恢复默认值");
                StartTime = new DateTime();
                EndTime = new DateTime();
            }


            if (string.IsNullOrEmpty(BattleNetPath))
            {
                BattleNetPath = AutoGetBattleNetPath();
            }

            if (string.IsNullOrEmpty(HearthbuddyPath))
            {
                Log("请配置兄弟路径");
            }
            #endregion

            this.Timer.Interval = 1;
            this.Timer.Tick += Check;
            
        }

        private void Check(object sender, EventArgs e)
        {
            this.Timer.Interval = 1000 * 60 * CheckInterval;

            bool flag = (DateTime.Compare(StartTime, EndTime) < 0) ?
                (DateTime.Compare(DateTime.Now, StartTime) < 0 || DateTime.Compare(EndTime, DateTime.Now) < 0) :
                (DateTime.Compare(DateTime.Now, StartTime) < 0 && DateTime.Compare(EndTime, DateTime.Now) < 0);
            if (flag)
            {
                Log($"当前时间为{DateTime.Now},未达到设定时间");
                StopHearthstone();
                return;
            }

            if (HearthstoneProcess.Length < 1)
            {
                Log("未找到炉石进程，开始启动");
                StopHearthstone();
                Delay(5000);
                StartHearthstone();
                Delay(5000);
                StartHearthbuddy();
                Log("已全部启动");
                return;
            }

            string logsPath = Path.Combine(BaseDirectory, @"Routines\DefaultRoutine\Silverfish\UltimateLogs\");

            DirectoryInfo folder = new DirectoryInfo(logsPath);
            DateTime lastModifiedTime = new DateTime();
            foreach (FileInfo file in folder.GetFiles("*.txt"))
            {
                if (DateTime.Compare(file.LastWriteTime,lastModifiedTime) > 0)
                {
                    lastModifiedTime = file.LastWriteTime;
                }
            }
            Log($"读取到最近运行时间：{lastModifiedTime.ToString("d")} {lastModifiedTime.ToString("t")}");
            if (this.LastDateTime != lastModifiedTime)
            {
                this.LastDateTime = lastModifiedTime;
#pragma warning disable IDE0071 // 简化内插
                Log($"{DateTime.Now.ToString("d")} {DateTime.Now.ToString("t")}运行正常");
#pragma warning restore IDE0071 // 简化内插
            }
            else
            {
                Log($"{CheckInterval}分钟内无日志变化，重新启动");
                StopHearthstone();
                Delay(5000);
                StartHearthstone();
                Delay(5000);
                StartHearthbuddy();
                Log("已全部重新启动");
            }
        }

        private void SelectBattleNetFileButton_Click(object sender, RoutedEventArgs e)
        {
            string str = SelectExeFile("Battle.net Launcher.exe|*.exe");
            if (!string.IsNullOrEmpty(str))
            {
                BattleNetPath = str;
                Log("战网路径配置成功");
            }
            
        }

        private void SelectHearthbuddyFileButton_Click(object sender, RoutedEventArgs e)
        {
            string str = SelectExeFile("All Files|*.*");
            if (!string.IsNullOrEmpty(str))
            {
                HearthbuddyPath = str;
                Log("兄弟路径配置成功");
            }
        }

        /// <summary>
        /// 限制textbox只能输入数字
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        /// <summary>
        /// 屏蔽textbox的复制、剪切、粘贴功能
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CommandBinding_CanExecute(object sender, System.Windows.Input.CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = false;
            e.Handled = true;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            XmlConfigUtil util = new XmlConfigUtil("config.xml");
            util.Write(BattleNetPath, "BattleNetPath");
            util.Write(HearthbuddyPath, "HearthbuddyPath");
            util.Write(BNHSInterval.ToString(), "BNHSInterval");
            util.Write(HSHBInterval.ToString(), "HSHBInterval");
            util.Write(CheckInterval.ToString(), "CheckInterval");

            util.Write(StartTime.Hour.ToString(), "StartTime");
            util.Write(EndTime.Hour.ToString(), "EndTime");

            string logDirectory = Path.Combine(this.BaseDirectory, "Logs");
            if (!Directory.Exists(logDirectory)) //防止目录不存在报错
                Directory.CreateDirectory(logDirectory);
            string logPath = Path.Combine(logDirectory, $"HearthbuddyHelper_{DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss")}.log");
            using (StreamWriter sw = new StreamWriter(logPath, true, System.Text.Encoding.UTF8))
            {
                sw.Write(LogTextBox.Text);
            }

            Environment.Exit(0);
        }

        private void StartOrStopButton_Click(object sender, RoutedEventArgs e)
        {
            if (!IsRunning)
            {
                bool error = false;
                int temp;
                BNHSInterval = int.TryParse(BNHSIntervalTextBox.Text, out temp) ? temp : DefaultBNHSInterval;
                HSHBInterval = int.TryParse(HSHBIntervalTextBox.Text, out temp) ? temp : DefaultHSHBInterval;
                CheckInterval = int.TryParse(CheckIntervalTextBox.Text, out temp) ? temp : DefaultCheckInterval;

                if (!File.Exists(BattleNetPath))
                {
                    error = true;
                    Log("战网路径配置错误");
                }
                if (!File.Exists(HearthbuddyPath))
                {
                    error = true;
                    Log("兄弟路径配置错误");

                }
                if (!error && BaseDirectory != Directory.GetParent(this.HearthbuddyPath).FullName + "\\")
                {
                    error = true;
                    Log("本程序未放置在兄弟根目录");
                    Log("当前目录：" + BaseDirectory);
                    Log("所配置兄弟目录：" + Directory.GetParent(this.HearthbuddyPath).FullName + "\\");
                }

                if (error)
                {
                    this.Title = "HearthbuddyHelper——未启动";
                    Log("--------------------");
                    return;
                }
                else
                {
                    IsRunning = true;
                    StartOrStopButton.Content = "停止";
                    BattleNetPathTextBox.IsEnabled = false;
                    HearthbuddyPathTextBox.IsEnabled = false;
                    BNHSIntervalTextBox.IsEnabled = false;
                    HSHBIntervalTextBox.IsEnabled = false;
                    CheckIntervalTextBox.IsEnabled = false;
                    StartTimeTextBox.IsEnabled = false;
                    EndTimeTextBox.IsEnabled = false;

                    Log($"配置成功，开始启动(60 * {CheckInterval})");
                    this.Title = "HearthbuddyHelper——已启动";
                    this.Timer.Start();
                }
            }
            else
            {
                IsRunning = false;
                StartOrStopButton.Content = "开始";
                BattleNetPathTextBox.IsEnabled = true;
                HearthbuddyPathTextBox.IsEnabled = true;
                BNHSIntervalTextBox.IsEnabled = true;
                HSHBIntervalTextBox.IsEnabled = true;
                CheckIntervalTextBox.IsEnabled = true;
                StartTimeTextBox.IsEnabled = true;
                EndTimeTextBox.IsEnabled = true;

                Log($"已停止（可能存在延迟情况，可以选择直接关掉程序）");
                this.Title = "HearthbuddyHelper——未启动";
                this.Timer.Stop();
                this.Timer.Interval = 1;
            }
        }
    }
}
