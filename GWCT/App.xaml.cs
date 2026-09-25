using GWCT.CoreCtrl;
using GWCT.Tool;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace GWCT
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// 默认构造函数
        /// </summary>
        public App()
        {
            SelfTimer = new Stopwatch();
            SelfTimer.Start();
            if (!LoadCoreCode())
            {
                Popup("核心代码加载失败，以下为详细信息\ngwctcore, Version=0.0.0.0, Culture=neutral, PublicKeyToken=dab2c249d47b9d4f", MessageBoxImage.Error);
                Environment.Exit(0);
            }
        }
        /// <summary>
        /// 加载核心代码
        /// </summary>
        /// <returns>返回操作是否成功</returns>
        private bool LoadCoreCode()
        {
            byte[] dllBytes = null;
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("GWCT.gwctcore.dll.deflate"))
            {
                if (stream == null) return false;
                using (DeflateStream dz = new DeflateStream(stream, CompressionMode.Decompress))
                {
                    using (MemoryStream output = new MemoryStream())
                    {
                        dz.CopyTo(output);
                        dllBytes = output.ToArray();
                    }
                }
            }
            Assembly assembly = Assembly.Load(dllBytes);
            if (!VmVyaWZ5(null, ZGxsa2V5, assembly?.GetName()?.GetPublicKey())) return false;
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                if (args.Name == assembly.GetName().FullName)
                {
                    return assembly;
                }
                return null;
            };
            return true;
        }
        /// <summary>
        /// 静态主版本号
        /// </summary>
        public static readonly Version MainVersion = new Version(4, 8, 3, 2);

        /// <summary>
        /// 全局计时器
        /// </summary>
        private readonly Stopwatch SelfTimer;
        /// <summary>
        /// 全局主窗口实例
        /// </summary>
        private MainWindow window;
        /// <summary>
        /// 主窗口关闭状态标志
        /// </summary>
        private volatile bool window_isclosed = false;

        /// <summary>
        /// 应用程序启动逻辑
        /// </summary>
        /// <param name="sender">事件来源</param>
        /// <param name="e">事件数据</param>
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
            DWM.EnableDarkModeForNativeControls(false);
            ProgramStarted = new EventWaitHandle(false, EventResetMode.AutoReset, GUID, out bool createnew);
            if (createnew)
            {
                ThreadPool.RegisterWaitForSingleObject(ProgramStarted, OnProgramStarted, null, -1, false);
            }
            else
            {
                ProgramStarted.Set();
                Environment.Exit(0);
            }
            using (Process self = Process.GetCurrentProcess())
            {
                EcoQoS.Set(self.Id, true);
            }
            window = new MainWindow(SelfTimer);
            window.ShowDialog();
            window_isclosed = true;
            window.bin?.Writestat(SelfTimer.Elapsed);
            Task.Factory.StartNew(() =>
            {
                window.procmgr?.WaitForExit(2000);
                window.TaskWatcher?.Wait(2000);
                this.Dispatcher.Invoke(() => this.Shutdown());
            }, TaskCreationOptions.LongRunning);
        }

        /// <summary>
        /// 全局GUID常数
        /// </summary>
        public readonly string GUID = "1B1FBA27-60A8-4F8B-B107-73EA7DA0CE56";

        /// <summary>
        /// 全局线程同步事件
        /// </summary>
        private EventWaitHandle ProgramStarted;

        /// <summary>
        /// 当收到第二进程的通知时，响应消息
        /// </summary>
        /// <param name="state">消息参数</param>
        /// <param name="timeout">超时时间</param>
        private void OnProgramStarted(object state, bool timeout) => Dispatcher.Invoke(() => window?.Activate());

        /// <summary>
        /// 捕获非UI线程所有异常
        /// </summary>
        /// <param name="sender">抛出异常的?(object)</param>
        /// <param name="e">异常相关信息</param>
        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e) => Exception_throw(e.ExceptionObject as Exception);

        /// <summary>
        /// 捕获UI线程所有异常
        /// </summary>
        /// <param name="sender">抛出异常的?(object)</param>
        /// <param name="e">异常相关信息</param>
        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e) => Exception_throw(e.Exception);

        /// <summary>
        /// 捕获异步任务所有异常
        /// </summary>
        /// <param name="sender">抛出异常的?(object)</param>
        /// <param name="e">异常相关信息</param>
        private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e) => Exception_throw(e.Exception);

        /// <summary>
        /// 封装异常处理程序
        /// </summary>
        /// <param name="e">异常相关信息</param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Exception_throw(Exception e)
        {
            StringBuilder errorinfo = new StringBuilder(short.MaxValue);
            errorinfo.Append($"[应用程序内部异常] [{SelfTimer.Elapsed:hh\\:mm\\:ss}]\n\n根命名空间:{e.Source}\n方法体:{e.TargetSite}");
            if (e is AggregateException ae && ae.InnerException != null)
            {
                errorinfo.Append($"\nInnerException:{e.InnerException.GetType().Name}\n    根命名空间:{e.InnerException.Source}\n    方法体:{e.InnerException.TargetSite}\n    详细信息:\n        {e.InnerException.Message}{(Regex.IsMatch(e.InnerException.Message, @"\n\z") ? string.Empty : "\n")}    位置:");
                if (!string.IsNullOrEmpty(e.InnerException.StackTrace))
                {
                    foreach (string st in e.InnerException.StackTrace.Trim().Split('\n'))
                    {
                        errorinfo.Append($"\n        {st.Trim()}");
                    }
                }
                errorinfo.Append("\n\nGWCT - Exceptions Processed By FeiLingshu");
            }
            else
            {
                errorinfo.Append($"\n详细信息:{e.GetType().Name}\n    {e.Message}{(Regex.IsMatch(e.Message, @"\n\z") ? string.Empty : "\n")}位置:");
                if (!string.IsNullOrEmpty(e.StackTrace))
                {
                    foreach (string st in e.StackTrace.Trim().Split('\n'))
                    {
                        errorinfo.Append($"\n    {st.Trim()}");
                    }
                }
                errorinfo.Append("\n\nGWCT - Exceptions Processed By FeiLingshu");
            }
            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (window != null && !window_isclosed) window.Close();
                    Popup(errorinfo.ToString(), MessageBoxImage.Error);
                    window?.procmgr?.WaitForExit(2000);
                    window?.TaskWatcher?.Wait(2000);
                });
            }
            catch (Exception) { }
            finally { Environment.Exit(0); }
        }



        /// <summary>
        /// 调用系统提示窗口
        /// </summary>
        /// <param name="info">要显示的信息</param>
        /// <param name="type">提示类型</param>
        /// <param name="ui">用户交互类型</param>
        /// <param name="_">默认返回值</param>
        /// <param name="level">弹出式窗口等级</param>
        /// <returns>弹出式窗口返回值</returns>
        private MessageBoxResult Popup(string info, MessageBoxImage type, MessageBoxButton ui = MessageBoxButton.OK, MessageBoxResult _ = MessageBoxResult.OK, MessageBoxOptions level = MessageBoxOptions.DefaultDesktopOnly) => MessageBox.Show(info, "GWCT", ui, type, _, level);



        /// <summary>
        /// 立即重绘
        /// </summary>
        public const uint RDW_INVALIDATE = 0x0001;
        /// <summary>
        /// 重绘所有子代
        /// </summary>
        public const uint RDW_ALLCHILDREN = 0x0080;
        /// <summary>
        /// 强制重绘窗口
        /// </summary>
        /// <param name="hWnd">窗口句柄</param>
        /// <param name="lprcUpdate">包含更新矩形信息的指针</param>
        /// <param name="hrgnUpdate">更新区域句柄</param>
        /// <param name="flag">重绘方式枚举</param>
        /// <returns>返回操作是否成功</returns>
        [DllImport("user32.dll")]
        public extern static bool RedrawWindow(IntPtr hWnd, IntPtr lprcUpdate, IntPtr hrgnUpdate, uint flag);



        #region 内部安全代码

        /// <summary>
        /// 特征码
        /// </summary>
        public const string bWFpbmtleQ = "​​E​​​​​i​​​​​)a​​​​​​&​A​​​​​E​​​​stbR​​​a​​​​0​​0​*-.k'&(​Cb-.'@|,)&)(&++G~&'v&OY(,_+Dg&'-/(&/*,.4(zi**+'y'/V&&),((1+&,b)m​)5C'​+-A./-*F.'%.1(.+V​,*​E+,R*2,I+7-'),*1+P7'E​-&&St/&-+\\*-/{6f)*m​W.​HK​/.G'-'.+u​&,-.&_Zoyh/**M+L.mG7**+&&+0]JF-'h'-!";
        /// <summary>
        /// 特征码
        /// </summary>
        public const string ZGxsa2V5 = "​​E​​​​​i​​​​​)a​​​​​​&​A​​​​​E​​​​stbR​​​a​​​​0​​0​(v..--(k*​x-n*​e​2​;-v​'dS,​.&/)-(+((M(/+'a​qr*/&((e-r+B(r*Z)ZN,`+|>)+)(x**-.&)&//​​&,+(<0&,'**)&ct+fsW../}L-&T&T-'HY.hl('R-r./.&o5d&-a>0​(-,'Xx*'O&'7​-).​)'}>/​Wi|+FG*)&+&-9​./,&}/HJ'R-u-++$";

        /// <summary>
        /// 内部安全方法
        /// </summary>
        /// <param name="sender">调用方</param>
        /// <param name="ZmVhdHVyZQ">数据1</param>
        /// <param name="cHVibGlja2V5">数据2</param>
        /// <param name="testmode">测试模式</param>
        /// <returns>返回值</returns>
        [DebuggerHidden]
        [DebuggerStepThrough]
        public static bool VmVyaWZ5(object sender, string ZmVhdHVyZQ, byte[] cHVibGlja2V5, bool testmode = false)
        {
            if (cHVibGlja2V5 != null)
            {
                StringBuilder output = new StringBuilder(ZmVhdHVyZQ.Length * 2);
                byte[] bytes = Encoding.ASCII.GetBytes(ZmVhdHVyZQ);
                foreach (byte b in bytes)
                {
                    if (b == 0x3F)
                    {
                        output.Append((char)48);
                    }
                    else
                    {
                        int _b = b;
                        if (_b > 0x3F) _b--;
                        _b -= 32;
                        output.Append(_b.ToString("X2").TrimStart((char)48));
                    }
                }
                if (string.Equals(BitConverter.ToString(cHVibGlja2V5, 0, cHVibGlja2V5.Length).Replace("-", ""), output.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    if (sender == null || !testmode) return true;
                }
            }
            if (sender != null)
            {
                object SGFuZA = typeof(Trace).Assembly?.GetType(new string(Array.ConvertAll(new byte[25] { 0x53, 0x79, 0x73, 0x74, 0x65, 0x6D, 0x2E, 0x4D, 0x65, 0x64, 0x69, 0x61, 0x2E, 0x53, 0x79, 0x73, 0x74, 0x65, 0x6D, 0x53, 0x6F, 0x75, 0x6E, 0x64, 0x73 }, code => (char)code)))?.GetProperty(new string(Array.ConvertAll(new byte[4] { 0x48, 0x61, 0x6E, 0x64 }, code => (char)code)))?.GetValue(null);
                SGFuZA?.GetType()?.GetMethod(new string(Array.ConvertAll(new byte[4] { 0x50, 0x6C, 0x61, 0x79 }, code => (char)code)))?.Invoke(SGFuZA, null);
                var R1dDVC5Ob3RpZnk = Type.GetType(new string(Array.ConvertAll(new byte[11] { 0x47, 0x57, 0x43, 0x54, 0x2E, 0x4E, 0x6F, 0x74, 0x69, 0x66, 0x79 }, code => (char)code)));
                R1dDVC5Ob3RpZnk?.GetMethod(new string(Array.ConvertAll(new byte[10] { 0x53, 0x68, 0x6F, 0x77, 0x44, 0x69, 0x61, 0x6C, 0x6F, 0x67 }, code => (char)code))).Invoke(Activator.CreateInstance(R1dDVC5Ob3RpZnk, new object[] { (Window)sender, "\u5b8c\u6574\u6027\u6821\u9a8c\u5931\u8d25\n\n\u53ef\u80fd\u4f7f\u7528\u7b2c\u4e09\u65b9\u6216\u81ea\u6784\u5efa\u7248\u672c\uff0c\u8bf7\u81ea\u884c\u786e\u8ba4\u5b89\u5168\u6027\uff1b\u5982\u901a\u8fc7\u8d2d\u4e70\u83b7\u5f97\u672c\u7a0b\u5e8f\uff0c\u8bf7\u7acb\u5373\u9000\u6b3e" }), null);
            }
            return false;
        }

        #endregion
    }
}
