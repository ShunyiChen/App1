using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace App1
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private DispatcherTimer _timer;
        private int TeamsAwaySeconds = 300;     // 5 分钟
        private int EarlyWarningSeconds = 10;   // 提前 10 秒
        private bool ContinuousReminderEnabled = false;
        private bool _hasWarned = false;              // 避免重复提醒
        private int _lastIdleSeconds = 0;             // 用来判断是否有用户活动
        private bool _timerStarted = false;            // 防止重复启动定时器
        private int _startupIdleSeconds = 0;           // 记录窗口显示时的空闲基线
        public MainWindow()
        {
            InitializeComponent();
            LoadSettings();
        }

        // 加载配置文件
        private void LoadSettings()
        {
            string filePath = GetSettingsFilePath();
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                var settings = JsonSerializer.Deserialize<Settings>(json);
                if (settings is not null)
                {
                    IntervalTextBox.Text = settings.TeamsAwaySeconds.ToString();
                    ReminderTextBox.Text = settings.EarlyWarningSeconds.ToString();
                    ContinuousReminderCheckBox.IsChecked = settings.ContinuousReminderEnabled;

                    TeamsAwaySeconds = settings.TeamsAwaySeconds;
                    EarlyWarningSeconds = settings.EarlyWarningSeconds;
                    ContinuousReminderEnabled = settings.ContinuousReminderEnabled;
                }
                else
                {
                    IntervalTextBox.Text = "300";
                    ReminderTextBox.Text = "15";
                    ContinuousReminderCheckBox.IsChecked = false;
                }
            }
            else
            {
                // 初始值设定
                IntervalTextBox.Text = "300";
                ReminderTextBox.Text = "15";
                ContinuousReminderCheckBox.IsChecked = false;
            }
        }
        private static string GetSettingsFilePath()
        {
            string homePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string directoryPath = Path.Combine(homePath, ".app1");

            // 确保文件夹存在
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            return Path.Combine(directoryPath, "settings.json");
        }

        public void StartTimer() {
            if (_timerStarted)
            {
                return;
            }

            // 计时器每秒触发一次，用于更新倒计时和检查空闲状态。
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += TimerElapsed;
            timer.Start();
            _timer = timer;
            _timerStarted = true;
        }

        private void StopTimer()
        {
            if (_timer is null)
            {
                return;
            }

            _timer.Stop();
            _timer.Tick -= TimerElapsed;
            _timerStarted = false;
            _timer = null;
        }

        private void RootPanel_Loaded(object sender, RoutedEventArgs e)
        {
            if (_timerStarted)
            {
                return;
            }

            // 在窗口加载完成后记录当前空闲时间，
            // 这样倒计时从窗口显示时刻开始，而非应用启动时刻。
            _startupIdleSeconds = GetIdleTimeSeconds();
            UpdateCountdownLabel(TeamsAwaySeconds);
            StartTimer();
        }

        private void TimerElapsed(object? sender, object? e)
        {
            int currentIdleSeconds = GetIdleTimeSeconds();
            // 计算从窗口显示后经过的空闲秒数，避免启动前的系统空闲时间影响倒计时。
            int idleSeconds = currentIdleSeconds - _startupIdleSeconds;
            if (idleSeconds < 0)
            {
                idleSeconds = 0;
            }

            // 在 UI 显示当前空闲秒数
            //Console.WriteLine($"空闲时间：{idleSeconds} 秒");
            System.Diagnostics.Debug.WriteLine($"空闲时间：{idleSeconds} 秒");

            // ★★ 判断是否有用户活动（如果 idle 时间减少 → 用户动了）
            if (idleSeconds < _lastIdleSeconds)
            {
                // 用户有操作 → 重置提醒状态
                _hasWarned = false;
            }

            _lastIdleSeconds = idleSeconds;

            // 提前提醒秒数
            int warningPoint = TeamsAwaySeconds - EarlyWarningSeconds;

            // ★★ 到达提前提醒点并且未提醒过 → 播放提示音
            System.Diagnostics.Debug.WriteLine($"{TeamsAwaySeconds - idleSeconds} - {warningPoint} - {_hasWarned}");
            UpdateCountdownLabel(TeamsAwaySeconds - idleSeconds);

            if (idleSeconds >= warningPoint && !_hasWarned)
            {
                System.Diagnostics.Debug.WriteLine($"ContinuousReminderEnabled={ContinuousReminderEnabled}");
                if (!ContinuousReminderEnabled)
                {
                    _hasWarned = true;
                    PlayMp3File();
                }
                else {
                    if (idleSeconds % 5 == 0) {
                        PlayMp3File();
                    }
                }
            }
        }

        private void PlayMp3File()
        {
            // 获取本地MP3文件的路径
            //string filePath = "ms-appx:///Assets/alert.wav"; // 使用相对路径或绝对路径
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "alert.wav");
            MediaSource mediaSource = MediaSource.CreateFromUri(new Uri(filePath));

            // 创建MediaPlayer对象
            MediaPlayer mediaPlayer = new MediaPlayer();
            mediaPlayer.AutoPlay = false; // 设置为true以自动播放
            mediaPlayer.Source = mediaSource;

            // 播放音乐
            mediaPlayer.Play();
        }

        #region Win32 API - 获取系统空闲时间
        [StructLayout(LayoutKind.Sequential)]
        private struct LASTINPUTINFO
        {
            public uint cbSize;
            public uint dwTime;
        }

        [DllImport("user32.dll")]
        private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        private static int GetIdleTimeSeconds()
        {
            LASTINPUTINFO info = new LASTINPUTINFO();
            info.cbSize = (uint)Marshal.SizeOf(info);

            if (!GetLastInputInfo(ref info))
                return 0;

            uint idle = (uint)Environment.TickCount - info.dwTime;
            return (int)(idle / 1000);
        }
        #endregion

        // 保存设置
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(IntervalTextBox.Text, out int interval) && int.TryParse(ReminderTextBox.Text, out int reminder))
            {
                TeamsAwaySeconds = interval;
                EarlyWarningSeconds = reminder;
                bool continuous = ContinuousReminderCheckBox.IsChecked ?? false;
                ContinuousReminderEnabled = continuous;

                var settings = new Settings { TeamsAwaySeconds = interval, EarlyWarningSeconds = reminder, ContinuousReminderEnabled = continuous };
                string jsonString;
                try
                {
                    var options = new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        ReferenceHandler = ReferenceHandler.IgnoreCycles  // 忽略循环引用
                    };
                    jsonString = JsonSerializer.Serialize(settings, options);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"序列化失败: {ex}");
                    var dialog = new ContentDialog
                    {
                        Title = "保存失败",
                        Content = $"保存设置时发生错误：{ex.Message}",
                        CloseButtonText = "确定",
                        XamlRoot = this.Content.XamlRoot
                    };
                    _ = dialog.ShowAsync();
                    return;
                }
                string filePath = GetSettingsFilePath();
                File.WriteAllText(filePath, jsonString);
            }
            else
            {
                //System.Diagnostics.Debug.WriteLine($"请输入有效的数字");
                // 输入无效时的提示
                var dialog = new ContentDialog
                {
                    Title = "错误",
                    Content = "请输入有效的数字（秒）",
                    CloseButtonText = "确定",
                    XamlRoot = this.Content.XamlRoot
                };
                _ = dialog.ShowAsync();
            }
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            if (StopButton.IsChecked == true)
            {
                // 按钮被按下 - 暂停计时器
                StopTimer();
            }
            else
            {
                // 按钮弹起 - 继续计时器
                StartTimer();
            }
        }

        // 更新倒计时标签
        private void UpdateCountdownLabel(int _countdownValue)
        {
            if (_countdownValue >= 0)
            {
                CountdownLabel.Text = $"倒计时: {_countdownValue}秒";
            }
        }

    }

    // 配置文件模型
    public class Settings
    {
        public int TeamsAwaySeconds { get; set; }
        public int EarlyWarningSeconds { get; set; }
        public bool ContinuousReminderEnabled { get; set; }
    }
}
