using GWCT.Cfg;
using GWCT.CoreCtrl;
using GWCT.Datas;
using GWCT.Tool;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Media;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace GWCT
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// 窗口默认构造函数
        /// </summary>
        /// <param name="timer">全局计时器实例</param>
        public MainWindow(Stopwatch timer)
        {
            InitializeComponent();
            IntPtr handle = IntPtr.Zero;
            SelfTimer = timer;
            // 初始化事件绑定
            this.MouseLeftButtonDown += (s, e) => MLBD = e.OriginalSource;
            this.MouseRightButtonDown += (s, e) => MRBD = e.OriginalSource;
            this.TITLE.MouseLeftButtonDown += (s, e) => this.DragMove();
            this.TITLE.PreviewMouseUp += (s, e) =>
            {
                if (e.ChangedButton == MouseButton.Right && MRBD != e.OriginalSource) e.Handled = true;
            };
            void blockmd(object s, MouseButtonEventArgs e)
            {
                switch (e.ChangedButton)
                {
                    case MouseButton.Left:
                        MLBD = s;
                        break;
                    case MouseButton.Right:
                        MRBD = s;
                        break;
                }
                e.Handled = true;
            }
            this.CLOSE.PreviewMouseDown += blockmd;
            this.MINI.PreviewMouseDown += blockmd;
            this.STAT.PreviewMouseDown += blockmd;
            bool Win32CloseSignal = false;
            this.ICON.PreviewMouseDown += (s, e) =>
            {
                switch (e.ChangedButton)
                {
                    case MouseButton.Left:
                        MLBD = s;
                        Win32CloseSignal = e.ClickCount >= 2;
                        break;
                    case MouseButton.Right:
                        MRBD = s;
                        break;
                }
                e.Handled = true;
            };
            this.CLOSE.PreviewMouseUp += (s, e) =>
            {
                if (e.ChangedButton == MouseButton.Left && MLBD == e.OriginalSource) this.Close();
                e.Handled = true;
            };
            this.MINI.PreviewMouseUp += (s, e) =>
            {
                if (e.ChangedButton == MouseButton.Left && MLBD == e.OriginalSource) this.WindowState = WindowState.Minimized;
                e.Handled = true;
            };
            this.ICON.PreviewMouseUp += (s, e) =>
            {
                switch (e.ChangedButton)
                {
                    case MouseButton.Left:
                        if (MLBD == e.OriginalSource && Win32CloseSignal) Win32PopupMenu.CloseWindow(handle);
                        break;
                    case MouseButton.Right:
                        if (MRBD == e.OriginalSource) Win32PopupMenu.ShowSystemMenu(handle, this.ICON);
                        break;
                }
                e.Handled = true;
            };
            void ResetSelect(object sender, EventArgs e)
            {
                SettingList.SelectedIndex = -1;
                LauncherList.SelectedIndex = -1;
            }
            this.S.MouseLeftButtonUp += (s, e) =>
            {
                if (MLBD != e.OriginalSource) return;
                if (e.OriginalSource is ScrollViewer) ResetSelect(s, e);
                this.NULLPART.Focus();
            };
            this.Launcher.Checked += ResetSelect;
            this.Launcher.Unchecked += ResetSelect;
            this.L.MouseLeftButtonUp += (s, e) =>
            {
                if (MLBD != e.OriginalSource) return;
                if (e.OriginalSource is ScrollViewer) LogList.SelectedIndex = -1;
                this.NULLPART.Focus();
            };
            void KillFocus(object sender, MouseButtonEventArgs e)
            {
                if (e.OriginalSource == MLBD && (e.OriginalSource == this.SETTINGS || e.OriginalSource == this.LOGS)) this.NULLPART.Focus();
            }
            this.MouseLeftButtonUp += KillFocus;
            // UI数据更新
            this.SettingList.MouseRightButtonUp += PauseTag;
            this.Add.MouseLeftButtonUp += AddSetting;
            this.Remove.MouseLeftButtonUp += RemoveSetting;
            this.Start.MouseLeftButtonUp += StartLauncher;
            this.WidthData.PreviewTextInput += Number_PreviewTextInput;
            this.HeightData.PreviewTextInput += Number_PreviewTextInput;
            DataObject.AddPastingHandler(this.WidthData, OnPastingEvent);
            DataObject.AddPastingHandler(this.HeightData, OnPastingEvent);
            this.WidthData.TextChanged += WidthData_TextChanged;
            this.HeightData.TextChanged += HeightData_TextChanged;
            this.CPUCore.Checked += CPUCore_Checked;
            this.CPUCore.Unchecked += CPUCore_Unchecked;
            this.Save.MouseLeftButtonUp += SaveCfg;
            // 初始属性配置
            this.BarMaskS.Width = this.BarMaskL.Width = SystemParameters.VerticalScrollBarWidth + 5;
            // 初始化数据绑定
            this.SettingList.DataContext = settings = new Settings();
            settings.PropertyChanged += (s, e) =>
            {
                if (!settings.IsEmpty()) SettingList.ScrollIntoView(settings.SettingEntries.Last());
            };
            this.LauncherList.DataContext = launchers = new Settings();
            launchers.PropertyChanged += (s, e) =>
            {
                if (!launchers.IsEmpty()) LauncherList.ScrollIntoView(launchers.SettingEntries.Last());
            };
            this.LogList.DataContext = logs = new Logs();
            logs.PropertyChanged += (s, e) => LogList.ScrollIntoView(logs.LogEntries.Last());
            // 初始化模块
            bin = new Bin();
            winctrl = new WinCtrl((output, error) => Dispatcher.Invoke(() => PrintLog(output, error)));
            procmgr = new ProcMgr(bin, winctrl);
            // 窗口事件节点
            this.Loaded += (s, e) =>
            {
                handle = new WindowInteropHelper(this).Handle;
                this.Topmost = false;
                try
                {
                    handle.Set(false, true, DWM.TO_COLORREF((Application.Current.Resources["DarkBg"] as SolidColorBrush).Color), DWM.TO_COLORREF((Application.Current.Resources["WhiteText"] as SolidColorBrush).Color), true);
                }
                catch (Exception) { }
            };
            this.ContentRendered += (s, e) =>
            {
                App.RedrawWindow(handle, IntPtr.Zero, IntPtr.Zero, App.RDW_INVALIDATE | App.RDW_ALLCHILDREN);
                App.VmVyaWZ5(this, App.bWFpbmtleQ, Assembly.GetEntryAssembly()?.GetName()?.GetPublicKey());
                StartTimer();
                SetUIbyBin();
                winctrl.BeginEventHook();
                procmgr.Start();
            };
            this.Closing += (s, e) =>
            {
                EndTimer();
                winctrl.EndEventHook();
                procmgr.Stop();
            };
            this.OnTop.Click += (s, e) =>
            {
                bool value = !this.Topmost;
                this.Topmost = value;
                this.OnTop.Header = value ? "停用窗口置顶" : "启用窗口置顶";
            };
            this.Link.Click += (s, e) => Clipboard.SetText("https://github.com/FeiLingshu/GWCT");
            this.STAT.PreviewMouseUp += (s, e) =>
            {
                if (e.ChangedButton == MouseButton.Left && MLBD == e.OriginalSource)
                {
                    bin.GetStat(out uint C, SelfTimer.Elapsed, out TimeSpan R, out TimeSpan G);
                    SystemSounds.Asterisk.Play();
                    new Notify(this, $"应用程序统计信息\n\n版本信息　　  {App.MainVersion}\n加载次数　　  {C:N0} 次\n累计游戏时长  {G.Days:N0} 天 {G.Hours} 小时 {G.Minutes} 分 {G.Seconds} 秒\n累计运行时长  {R.Days:N0} 天 {R.Hours} 小时 {R.Minutes} 分 {R.Seconds} 秒").ShowDialog();
                }
                e.Handled = true;
            };
        }

        // UI响应

        /// <summary>
        /// 存储左键点击记录
        /// </summary>
        private object MLBD = null;
        /// <summary>
        /// 存储左键点击记录
        /// </summary>
        private object MRBD = null;

        /// <summary>
        /// 存储全局计时器实例
        /// </summary>
        private readonly Stopwatch SelfTimer;

        /// <summary>
        /// 存储 <see cref="Settings"/> 组件实例 (配置信息)
        /// </summary>
        private readonly Settings settings = null;
        /// <summary>
        /// 存储 <see cref="Settings"/> 组件实例 (启动器信息)
        /// </summary>
        private readonly Settings launchers = null;
        /// <summary>
        /// 存储 <see cref="Logs"/> 组件实例 (日志)
        /// </summary>
        private readonly Logs logs = null;

        private void PauseTag(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is TextBlock item && SettingList.SelectedIndex != -1 && item.Tag as string == (SettingList.SelectedItem as SettingEntry).P)
            {
                if (item.Text.StartsWith("*"))
                {
                    ChangePause(settings.Get(SettingList.SelectedIndex), false);
                    item.Text = item.Text.Substring(1);
                    item.Foreground = Application.Current.Resources["LightText"] as SolidColorBrush;
                }
                else
                {
                    ChangePause(settings.Get(SettingList.SelectedIndex), true);
                    item.Text = $"*{item.Text}";
                    item.Foreground = Application.Current.Resources["DimText"] as SolidColorBrush;
                }
            }
        }

        /// <summary>
        /// 添加配置
        /// </summary>
        /// <param name="sender">事件来源</param>
        /// <param name="e">事件数据</param>
        private void AddSetting(object sender, MouseButtonEventArgs e)
        {
            if (MLBD == e.OriginalSource)
            {
                if (Launcher.IsChecked == true)
                {
                    AddFile(true);
                }
                else
                {
                    AddFile();
                }
            }
        }

        /// <summary>
        /// 移除配置
        /// </summary>
        /// <param name="sender">事件来源</param>
        /// <param name="e">事件数据</param>
        private void RemoveSetting(object sender, MouseButtonEventArgs e)
        {
            if (MLBD == e.OriginalSource)
            {
                if (Launcher.IsChecked == true)
                {
                    RemoveFile(true);
                }
                else
                {
                    RemoveFile();
                }
            }
        }

        /// <summary>
        /// 尝试唤起启动器进程
        /// </summary>
        /// <param name="sender">事件来源</param>
        /// <param name="e">事件数据</param>
        private void StartLauncher(object sender, MouseButtonEventArgs e)
        {
            if (MLBD == e.OriginalSource && (CPUCore.IsChecked == false || CPUCore.IsEnabled) && Launcher.IsChecked == true) RunLauncher(CPUCore.IsChecked == true);
        }

        /// <summary>
        /// 缓存正则表达式
        /// </summary>
        private readonly Regex numregex = new Regex("[^0-9]");
        /// <summary>
        /// 过滤非法输入
        /// </summary>
        /// <param name="sender">事件来源</param>
        /// <param name="e">事件数据</param>
        private void Number_PreviewTextInput(object sender, TextCompositionEventArgs e) => e.Handled = numregex.IsMatch(e.Text) || !ushort.TryParse($"{((TextBox)sender).Text}{e.Text}", out _);
        /// <summary>
        /// 屏蔽复制操作
        /// </summary>
        /// <param name="sender">事件来源</param>
        /// <param name="e">事件数据</param>
        private void OnPastingEvent(object sender, DataObjectPastingEventArgs e) => e.CancelCommand();
        /// <summary>
        /// 同步值修改
        /// </summary>
        /// <param name="sender">事件来源</param>
        /// <param name="e">事件数据</param>
        private void WidthData_TextChanged(object sender, TextChangedEventArgs e) => SetWidth(ushort.Parse(this.WidthData.Text));
        /// <summary>
        /// 同步值修改
        /// </summary>
        /// <param name="sender">事件来源</param>
        /// <param name="e">事件数据</param>
        private void HeightData_TextChanged(object sender, TextChangedEventArgs e) => SetHeight(ushort.Parse(this.HeightData.Text));

        /// <summary>
        /// 配置页状态切换
        /// </summary>
        /// <param name="sender">事件来源</param>
        /// <param name="e">事件数据</param>
        private void CPUCore_Checked(object sender, RoutedEventArgs e) => SetFlag(true);
        /// <summary>
        /// 配置页状态切换
        /// </summary>
        /// <param name="sender">事件来源</param>
        /// <param name="e">事件数据</param>
        private void CPUCore_Unchecked(object sender, RoutedEventArgs e) => SetFlag(false);

        /// <summary>
        /// 推送配置信息
        /// </summary>
        /// <param name="sender">事件来源</param>
        /// <param name="e">事件数据</param>
        private void SaveCfg(object sender, MouseButtonEventArgs e)
        {
            if (MLBD == e.OriginalSource) Save2bin();
        }

        // 数据处理

        /// <summary>
        /// 存储 <see cref="Bin"/> 实例
        /// </summary>
        public readonly Bin bin;
        /// <summary>
        /// 存储 <see cref="WinCtrl"/> 实例
        /// </summary>
        public readonly WinCtrl winctrl;
        /// <summary>
        /// 存储 <see cref="ProcMgr"/> 实例
        /// </summary>
        public readonly ProcMgr procmgr;

        /// <summary>
        /// 预先配置 <see href="UI"/> 元素
        /// </summary>
        private void SetUIbyBin()
        {
            this.WidthData.Text = bin.BINDATA.SET_1.ToString();
            this.HeightData.Text = bin.BINDATA.SET_2.ToString();
            this.CPUCore.IsChecked = bin.BINDATA.SET_3;
            foreach (string path in bin.PATHS)
            {
                settings.Add(path);
            }
            foreach (string path in bin._PATHS)
            {
                launchers.Add(path, true);
            }
            this.CPUCore.IsEnabled = false;
            async void warking()
            {
                TaskCount.Add(out long tidvalue);
                await Task.Delay(1000); // 等待自身进程初始化
                procmgr.WriteValues(bin.PATHS.ToArray(), bin.BINDATA.SET_1, bin.BINDATA.SET_2, bin.BINDATA.SET_3);
                short report = Core.GetCoreMap(out bool is_override);
                this.CPUCore.IsEnabled = true;
                if (report == 0)
                {
                    if (is_override)
                    {
                        PrintLog($"已自动继承CPU核心掩码 (0x{Core.PMap:x16})", null);
                    }
                    else
                    {
                        PrintLog($"CPU核心掩码已生成 (0x{Core.PMap:x16})", false);
                    }
                }
                else
                {
                    PrintLog($"获取CPU核心掩码失败 (0x{report:x4})", true);
                }
                string updstd = string.Empty;
                bool? updflg = false;
                var result = await Update.CheckUpdateAsync(App.MainVersion);
                switch (result.Status)
                {
                    case Update.HttpStatus.OK:
                        updstd = $"更新检查完成，已是最新版本 ({App.MainVersion})";
                        updflg = false;
                        break;
                    case Update.HttpStatus.NotFound:
                        updstd = "更新检查完成，存在版本更新 (?)";
                        updflg = null;
                        break;
                    case Update.HttpStatus.Timeout:
                        updstd = "更新检查失败，连接超时 (timeout)";
                        updflg = true;
                        break;
                    case Update.HttpStatus.HttpError:
                        updstd = $"更新检查失败，Http错误 ({result.Code})";
                        updflg = true;
                        break;
                    case Update.HttpStatus.Unknown:
                        updstd = "更新检查失败，未知错误 (unknown)";
                        updflg = true;
                        break;
                }
                PrintLog(updstd, updflg);
                TaskCount.Release(tidvalue);
            }
            warking();
        }

        private void ChangePause(string path, bool add)
        {
            procmgr.WritePause(path, add);
        }

        /// <summary>
        /// 添加文件功能
        /// </summary>
        /// <param name="islauncher">指示是否为启动器类型</param>
        private void AddFile(bool islauncher = false)
        {
            OpenFileDialog openfileDialog = new OpenFileDialog
            {
                Title = islauncher ? "添加启动器路径..." : "添加游戏路径...",
                Filter = "可执行文件 (*.exe)|*.exe",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyComputer),
                CheckFileExists = true
            };
            if (openfileDialog.ShowDialog() == true)
            {
                string filepath = openfileDialog.FileName;
                if (islauncher)
                {
                    if (!bin._PATHS.Contains(filepath))
                    {
                        launchers.Add(filepath, true);
                        bin._PATHS.Add(filepath);
                    }
                }
                else
                {
                    if (!bin.PATHS.Contains(filepath))
                    {
                        settings.Add(filepath);
                        bin.PATHS.Add(filepath);
                    }
                }
            }
        }

        /// <summary>
        /// 移除文件功能
        /// </summary>
        /// <param name="islauncher">指示是否为启动器类型</param>
        private void RemoveFile(bool islauncher = false)
        {
            if (islauncher)
            {
                int index = LauncherList.SelectedIndex;
                if (index != -1)
                {
                    string filepath = launchers.Get(index);
                    launchers.Remove(index);
                    bin._PATHS.Remove(filepath);
                }
            }
            else
            {
                int index = SettingList.SelectedIndex;
                if (index != -1)
                {
                    string filepath = settings.Get(index);
                    ChangePause(filepath, false);
                    settings.Remove(index);
                    bin.PATHS.Remove(filepath);
                }
            }
        }

        /// <summary>
        /// 指示是否有启动器正在唤起
        /// </summary>
        private volatile bool islauncher = false;
        /// <summary>
        /// 唤起启动器进程
        /// </summary>
        /// <param name="core">是否执行核心亲和性配置</param>
        private async void RunLauncher(bool core)
        {
            int index = LauncherList.SelectedIndex;
            if (index != -1)
            {
                if (islauncher) return;
                islauncher = true;
                if (SELF == null) SELF = Process.GetCurrentProcess();
                IntPtr origin = SELF.ProcessorAffinity;
                bool ischanged = false;
                try
                {
                    if (core && Core.IsReady)
                    {
                        long map = Environment.Is64BitProcess ? (long)Core.PMap : (int)Core.PMap;
                        SELF.ProcessorAffinity = new IntPtr(map);
                        ischanged = true;
                        PrintLog("已创建临时CPU亲和性配置 (Idle)", false);
                    }
                    string starter = launchers.Get(index);
                    ProcessStartInfo info = new ProcessStartInfo()
                    {
                        FileName = starter,
                        WorkingDirectory = Path.GetDirectoryName(starter),
                        UseShellExecute = false
                    };
                    int pid = -1;
                    await Task.Factory.StartNew(() =>
                    {
                        TaskCount.Add(out long tidvalue);
                        Process p = Process.Start(info);
                        pid = p.Id;
                        p.Dispose();
                        TaskCount.Release(tidvalue);
                    }, TaskCreationOptions.LongRunning);
                    PrintLog($"启动器进程已唤起 ({pid})", false);
                }
                catch (Exception exp)
                {
                    PrintLog($"启动器进程唤起失败 ({exp.GetType().Name})", true);
                }
                finally
                {
                    if (ischanged) SELF.ProcessorAffinity = origin;
                }
                islauncher = false;
            }
        }

        /// <summary>
        /// 配置宽度信息
        /// </summary>
        /// <param name="num">数据值</param>
        private void SetWidth(ushort num) => bin.BINDATA.SET_1 = num;
        /// <summary>
        /// 配置高度信息
        /// </summary>
        /// <param name="num">数据值</param>
        private void SetHeight(ushort num) => bin.BINDATA.SET_2 = num;
        /// <summary>
        /// 配置核心亲和性开关
        /// </summary>
        /// <param name="num">数据值</param>
        private void SetFlag(bool flag) => bin.BINDATA.SET_3 = flag;
        /// <summary>
        /// 写入配置文件，并向核心组件发送数据
        /// </summary>
        private void Save2bin()
        {
            bin.SetData();
            procmgr.WriteValues(bin.PATHS.ToArray(), bin.BINDATA.SET_1, bin.BINDATA.SET_2, bin.BINDATA.SET_3);
            PrintLog("配置数据已更新 (GWCT.bin)", false);
        }

        // 日志功能

        /// <summary>
        /// 打印日志信息
        /// </summary>
        /// <param name="log">日志数据</param>
        /// <param name="error">错误等级</param>
        private void PrintLog(string log, bool? error) => logs.Add(SelfTimer.Elapsed.ToString(@"hh\:mm\:ss"), log, error);

        /// <summary>
        /// 保存全局自身进程实例
        /// </summary>
        private Process SELF;
        /// <summary>
        /// 保存 <see cref="TaskCount"/> 循环任务
        /// </summary>
        public Task TaskWatcher = null;
        /// <summary>
        /// 启动任务监视服务
        /// </summary>
        private void StartTimer()
        {
            if (SELF == null) SELF = Process.GetCurrentProcess();
            TaskCount.Input(
                Application.Current.Resources["Green"] as SolidColorBrush,
                Application.Current.Resources["Yellow"] as SolidColorBrush,
                Application.Current.Resources["Red"] as SolidColorBrush);
            TaskCount.PropertyChanged += Tick;
            ThreadPool.GetMaxThreads(out int max, out _);
            ThreadPool.GetMinThreads(out int min, out _);
            TaskWatcher = TaskCount.StartThreadPoolWatch(max - min);
            TaskWatcher.ContinueWith(task => throw task.Exception, TaskContinuationOptions.OnlyOnFaulted);
        }
        /// <summary>
        /// 终止任务监视服务
        /// </summary>
        private void EndTimer()
        {
            TaskCount.Watcher = false;
            TaskCount.PropertyChanged -= Tick;
            SELF?.Dispose();
        }
        /// <summary>
        /// 任务监视相关消息处理函数
        /// </summary>
        /// <param name="sender">消息来源</param>
        /// <param name="e">消息参数</param>
        private void Tick(object sender, TaskEventArgs e)
        {
            if (e.Type)
            {
                this.TASKFREE.Text = e.Data.Free.ToString();
                this.TASKFREE.Foreground = e.Data.Color;
            }
            else
            {
                this.TASK.Text = e.Data.Count.ToString();
                this.LAST.Text = e.Data.ID.ToString();
            }
        }
    }



    /// <summary>
    /// 窗口原生右键菜单支持
    /// </summary>
    internal static class Win32PopupMenu
    {
        /// <summary>
        /// 获取窗口菜单句柄
        /// </summary>
        /// <param name="hWnd">目标窗口句柄</param>
        /// <param name="bRevert">是否恢复原始菜单</param>
        /// <returns>返回目标窗口的菜单句柄</returns>
        private static IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert) => WinCtrl.GetSystemMenu(hWnd, bRevert);

        /// <summary>
        /// 指示菜单项应响应左键输入
        /// </summary>
        private const uint TPM_LEFTBUTTON = 0x0000;
        /// <summary>
        /// 指示菜单项应响应右键输入
        /// </summary>
        private const uint TPM_RIGHTBUTTON = 0x0002;
        /// <summary>
        /// 指示函数应返回菜单项命令常量
        /// </summary>
        private const uint TPM_RETURNCMD = 0x0100;
        /// <summary>
        /// 指示菜单应具有垂直动画 (如系统存在渐隐动画则不会生效)
        /// </summary>
        private const uint TPM_VERPOSANIMATION = 0x1000;

        /// <summary>
        /// 启动右键菜单
        /// </summary>
        /// <param name="hMenu">目标菜单句柄</param>
        /// <param name="uFlags">启动方式</param>
        /// <param name="x">菜单左上角 <see langword="x"/> 坐标</param>
        /// <param name="y">菜单左上角 <see langword="y"/> 坐标</param>
        /// <param name="nReserved">保留字段，置 <see langword="0"/></param>
        /// <param name="hWnd">目标窗口句柄</param>
        /// <param name="prcRect">菜单显示大小(不使用)</param>
        /// <returns>按传递标志位返回值，返回执行结果</returns>
        [DllImport("user32.dll")]
        private static extern int TrackPopupMenu(IntPtr hMenu, uint uFlags, int x, int y, int nReserved, IntPtr hWnd, IntPtr prcRect);

        /// <summary>
        /// 指示系统命令类型
        /// </summary>
        private const uint WM_SYSCOMMAND = 0x0112;
        /// <summary>
        /// 关闭窗口常量
        /// </summary>
        private const int SC_CLOSE = 0xF060;

        /// <summary>
        /// 向窗口发送消息
        /// </summary>
        /// <param name="hWnd">目标窗口</param>
        /// <param name="Msg">消息值</param>
        /// <param name="wParam">参数1</param>
        /// <param name="lParam">参数2</param>
        /// <returns>返回消息结果</returns>
        private static IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam) => WinCtrl.SendMessage(hWnd, Msg, wParam, lParam);

        /// <summary>
        /// 启动系统原生窗口右键菜单
        /// </summary>
        /// <param name="windowhandle">目标窗口句柄</param>
        /// <param name="element">要执行坐标计算的基准窗口元素</param>
        internal static void ShowSystemMenu(IntPtr windowhandle, FrameworkElement element)
        {
            IntPtr hMenu = GetSystemMenu(windowhandle, false);
            if (hMenu != IntPtr.Zero)
            {
                Point point = element.PointToScreen(new Point(element.Width + 5D, 0D));
                int commandId = TrackPopupMenu(hMenu, TPM_LEFTBUTTON | TPM_RIGHTBUTTON | TPM_RETURNCMD | TPM_VERPOSANIMATION, (int)point.X, (int)point.Y, 0, windowhandle, IntPtr.Zero);
                if (commandId != 0) SendMessage(windowhandle, WM_SYSCOMMAND, new IntPtr(commandId), IntPtr.Zero);
            }
        }

        /// <summary>
        /// 调用系统命令关闭窗口
        /// </summary>
        /// <param name="windowhandle"></param>
        internal static void CloseWindow(IntPtr windowhandle) => SendMessage(windowhandle, WM_SYSCOMMAND, new IntPtr(SC_CLOSE), IntPtr.Zero);
    }
}
