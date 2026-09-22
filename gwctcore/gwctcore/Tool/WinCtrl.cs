using GWCT.Datas;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GWCT.Tool
{
    /// <summary>
    /// 核心组件，窗口管理器
    /// </summary>
    public class WinCtrl
    {
        /// <summary>
        /// 用于打印日志的委托类型
        /// </summary>
        /// <param name="log">日志信息</param>
        /// <param name="error">日志等级</param>
        public delegate void OutputLog(string log, bool? error);

        /// <summary>
        /// 用于打印日志的委托实例
        /// </summary>
        public readonly OutputLog output;

        /// <summary>
        /// 指示是否已加载 (<see cref="WinCtrl"/>)
        /// </summary>
        public volatile bool IsLoaded = false;

        /// <summary>
        /// 指示是否正在运行 (<see cref="ProcMgr"/>)
        /// </summary>
        public volatile bool IsWorking = false;

        /// <summary>
        /// 默认构造函数
        /// </summary>
        /// <param name="output">传递打印日志所用委托</param>
        public WinCtrl(OutputLog output)
        {
            this.output = output;
        }

        /// <summary>
        /// 读取屏幕空间数据
        /// </summary>
        private RECT GetScreen()
        {
            float DPIX = 96;
            float DPIY = 96;
            IntPtr hdc = GetDC(IntPtr.Zero);
            if (hdc != IntPtr.Zero)
            {
                DPIX = GetDeviceCaps(hdc, LOGPIXELSX) / 96F;
                DPIY = GetDeviceCaps(hdc, LOGPIXELSY) / 96F;
                ReleaseDC(IntPtr.Zero, hdc);
            }
            IntPtr hMonitor = MonitorFromPoint(new POINT() { X = 0, Y = 0 }, MONITOR_DEFAULTTOPRIMARY);
            if (hMonitor != IntPtr.Zero)
            {
                MONITORINFO mi = new MONITORINFO
                {
                    cbSize = (uint)Marshal.SizeOf(typeof(MONITORINFO))
                };
                if (GetMonitorInfo(hMonitor, ref mi))
                {
                    RECT rect = new RECT
                    {
                        Left = (int)(mi.rcWork.Left * DPIX),
                        Top = (int)(mi.rcWork.Top * DPIY),
                        Right = (int)(mi.rcWork.Right * DPIX),
                        Bottom = (int)(mi.rcWork.Bottom * DPIY)
                    };
                    return rect;
                }
            }
            return new RECT() { Left = 0, Top = 0, Right = 1920, Bottom = 1080 };
        }



        /// <summary>
        /// win32api::GetDC
        /// </summary>
        /// <param name="hWnd">param#1</param>
        /// <returns>returns</returns>
        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hWnd);

        /// <summary>
        /// win32api::ReleaseDC
        /// </summary>
        /// <param name="hwnd">param#1</param>
        /// <param name="hdc">param#2</param>
        /// <returns>returns</returns>
        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);

        /// <summary>
        /// win32const::LOGPIXELSX
        /// </summary>
        private const int LOGPIXELSX = 88;
        /// <summary>
        /// win32const::LOGPIXELSY
        /// </summary>
        private const int LOGPIXELSY = 90;

        /// <summary>
        /// win32api::GetDeviceCaps
        /// </summary>
        /// <param name="hdc">param#1</param>
        /// <param name="index">param#2</param>
        /// <returns>returns</returns>
        [DllImport("gdi32.dll")]
        private static extern int GetDeviceCaps(IntPtr hdc, int index);

        /// <summary>
        /// win32const::MONITOR_DEFAULTTOPRIMARY
        /// </summary>
        private const uint MONITOR_DEFAULTTOPRIMARY = 0x00000001;

        /// <summary>
        /// win32struct::POINT
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        /// <summary>
        /// win32struct::RECT
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
            public int X => Left;
            public int Y => Top;
            public int Width => Right - Left;
            public int Height => Bottom - Top;
        }

        /// <summary>
        /// win32struct::MONITORINFO
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct MONITORINFO
        {
            public uint cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
        }

        /// <summary>
        /// win32api::MonitorFromPoint
        /// </summary>
        /// <param name="pt">param#1</param>
        /// <param name="dwFlags">param#2</param>
        /// <returns>returns</returns>
        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromPoint(POINT pt, uint dwFlags);

        /// <summary>
        /// win32api::GetMonitorInfo
        /// </summary>
        /// <param name="hMonitor">param#1</param>
        /// <param name="lpmi">param#2</param>
        /// <returns>returns</returns>
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);



        /// <summary>
        /// Hook实例1
        /// </summary>
        private IntPtr KeyHook;

        /// <summary>
        /// Hook实例2
        /// </summary>
        private IntPtr WindowLocationHook;

        /// <summary>
        /// Hook实例3
        /// </summary>
        private IntPtr WindowTitleHook;

        /// <summary>
        /// 用于为Hook实例1实现内存固定的全局字段
        /// </summary>
        private HOOKPROC _HOOKPROC;

        /// <summary>
        /// Hook实例1的处理程序
        /// </summary>
        /// <param name="nCode">参数1</param>
        /// <param name="wParam">参数2</param>
        /// <param name="lParam">参数3</param>
        /// <returns>返回值</returns>
        private int Win32CallBack(int nCode, int wParam, IntPtr lParam)
        {
            if (wParam == WM_KEYUP)
            {
                KeyBoardHookStruct keyBoardHookStruct = new KeyBoardHookStruct();
                try
                {
                    keyBoardHookStruct = (KeyBoardHookStruct)Marshal.PtrToStructure(lParam, typeof(KeyBoardHookStruct));
                }
                catch (Exception) { }
                if (keyBoardHookStruct != null && keyBoardHookStruct.vkCode == VK_F12)
                {
                    //
                    IntPtr hwnd = GetForegroundWindow();
                    ValueLock.Wait();
                    bool exist = windowlist.TryGetValue(hwnd, out WindowData value);
                    ValueLock.Release();
                    if (exist)
                    {
                        value.moveflag = !value.moveflag;
                        ValueLock.Wait();
                        windowlist[hwnd] = value;
                        ValueLock.Release();
                        SetMenu(hwnd, value.moveflag);
                        SetWindowTitle(hwnd, value.title, value.moveflag);
                        SetWindowArea(hwnd, value.winsize, value.clientsize, value.moveflag);
                    }
                }
            }
            return CallNextHookEx((int)KeyHook, nCode, wParam, lParam);
        }

        /// <summary>
        /// 用于为Hook实例2实现内存固定的全局字段
        /// </summary>
        private WinEventDelegate EventDelegate_L;

        /// <summary>
        /// Hook实例2的处理程序
        /// </summary>
        /// <param name="hWinEventHook">参数1</param>
        /// <param name="eventType">参数2</param>
        /// <param name="hwnd">参数3</param>
        /// <param name="idObject">参数4</param>
        /// <param name="idChild">参数5</param>
        /// <param name="dwEventThread">参数6</param>
        /// <param name="dwmsEventTime">参数7</param>
        private void WinEventHook_L(
            IntPtr hWinEventHook, uint eventType,
            IntPtr hwnd, int idObject, int idChild,
            uint dwEventThread, uint dwmsEventTime)
        {
            if (idObject == OBJID_WINDOW && idChild == CHILDID_SELF && hwnd != IntPtr.Zero)
            {
                if (IsIconic(hwnd)) return;
                //
                ValueLock.Wait();
                bool exist = windowlist.TryGetValue(hwnd, out WindowData value);
                ValueLock.Release();
                if (exist)
                {
                    if (SetWindowArea(hwnd, value.winsize, value.clientsize, value.moveflag))
                    {
                        long time = Environment.TickCount;
                        long cycle = time - value.timestamp;
                        if (cycle > 3 || cycle < 0)
                        {
                            value.timestamp = time;
                            value.blowcount = 1;
                        }
                        else
                        {
                            value.blowcount++;
                        }
                        if (value.blowcount >= 6)
                        {
                            ValueLock.Wait();
                            windowlist.Remove(hwnd);
                            ValueLock.Release();
                            SetWindowBlow(hwnd, value.title);
                            return;
                        }
                        ValueLock.Wait();
                        windowlist[hwnd] = value;
                        ValueLock.Release();
                    }
                }
            }
        }

        /// <summary>
        /// 用于为Hook实例3实现内存固定的全局字段
        /// </summary>
        private WinEventDelegate EventDelegate_T;

        /// <summary>
        /// Hook实例3的处理程序
        /// </summary>
        /// <param name="hWinEventHook">参数1</param>
        /// <param name="eventType">参数2</param>
        /// <param name="hwnd">参数3</param>
        /// <param name="idObject">参数4</param>
        /// <param name="idChild">参数5</param>
        /// <param name="dwEventThread">参数6</param>
        /// <param name="dwmsEventTime">参数7</param>
        private void WinEventHook_T(
            IntPtr hWinEventHook, uint eventType,
            IntPtr hwnd, int idObject, int idChild,
            uint dwEventThread, uint dwmsEventTime)
        {
            if (idObject == OBJID_WINDOW && idChild == CHILDID_SELF && hwnd != IntPtr.Zero)
            {
                //
                ValueLock.Wait();
                bool exist = windowlist.TryGetValue(hwnd, out WindowData value);
                ValueLock.Release();
                if (exist) SetWindowTitle(hwnd, value.title, value.moveflag);
            }
        }

        /// <summary>
        /// 开始Hook
        /// </summary>
        /// <exception cref="Win32Exception">发生 <see cref="Win32Exception"/> 错误</exception>
        public void BeginEventHook()
        {
            _HOOKPROC = Win32CallBack;
            KeyHook = SetWindowsHookEx(WH_KEYBOARD_LL, _HOOKPROC, IntPtr.Zero, 0);
            if (KeyHook == IntPtr.Zero)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(),
                    $"挂接全局HOOK挂钩过程中出现Win32异常({Marshal.GetLastWin32Error().ToString().PadLeft(4, '0')})。");
            }
            EventDelegate_L = WinEventHook_L;
            WindowLocationHook = SetWinEventHook(
                EVENT_OBJECT_LOCATIONCHANGE,
                EVENT_OBJECT_LOCATIONCHANGE,
                IntPtr.Zero,
                EventDelegate_L,
                0U,
                0U,
                WINEVENT_OUTOFCONTEXT);
            if (WindowLocationHook == IntPtr.Zero)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(),
                    $"挂接全局HOOK挂钩过程中出现Win32异常({Marshal.GetLastWin32Error().ToString().PadLeft(4, '0')})。");
            }
            EventDelegate_T = WinEventHook_T;
            WindowTitleHook = SetWinEventHook(
                EVENT_OBJECT_NAMECHANGE,
                EVENT_OBJECT_NAMECHANGE,
                IntPtr.Zero,
                EventDelegate_T,
                0U,
                0U,
                WINEVENT_OUTOFCONTEXT);
            if (WindowTitleHook == IntPtr.Zero)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(),
                    $"挂接全局HOOK挂钩过程中出现Win32异常({Marshal.GetLastWin32Error().ToString().PadLeft(4, '0')})。");
            }
            IsLoaded = true;
            if (IsLoaded) output($"事件监视器::WinEvent已挂载 ({Environment.OSVersion.Version})", false);
        }

        /// <summary>
        /// 卸载Hook
        /// </summary>
        /// <exception cref="Win32Exception">发生 <see cref="Win32Exception"/> 错误</exception>
        public void EndEventHook()
        {
            if (!UnhookWindowsHookEx((int)KeyHook))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(),
                    $"卸载全局HOOK挂钩过程中出现Win32异常({Marshal.GetLastWin32Error().ToString().PadLeft(4, '0')})。");
            }
            if (!UnhookWinEvent(WindowLocationHook))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(),
                    $"卸载全局HOOK挂钩过程中出现Win32异常({Marshal.GetLastWin32Error().ToString().PadLeft(4, '0')})。");
            }
            if (!UnhookWinEvent(WindowTitleHook))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(),
                    $"卸载全局HOOK挂钩过程中出现Win32异常({Marshal.GetLastWin32Error().ToString().PadLeft(4, '0')})。");
            }
            GC.KeepAlive(_HOOKPROC);
            GC.KeepAlive(EventDelegate_L);
            GC.KeepAlive(EventDelegate_T);
            if (IsLoaded) output("事件监视器::WinEvent已卸载 (Idle)", false);
            IsLoaded = false;
        }



        /// <summary>
        /// 声明 <see href="Win32"/> 结构 <see langword="KeyBoardHookStruct"/>
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private class KeyBoardHookStruct
        {
            /// <summary>
            /// 触发挂钩过程的键盘按键的键值
            /// </summary>
            public int vkCode;
            /// <summary>
            /// 挂钩过程的扫描代码(unchecked)
            /// </summary>
            public int scanCode;
            /// <summary>
            /// 挂钩过程的位标志(unchecked)
            /// </summary>
            public int flags;
            /// <summary>
            /// 挂钩过程触发的时间(unchecked)
            /// </summary>
            public int time;
            /// <summary>
            /// 挂钩过程包含的状态详细信息(unchecked)
            /// </summary>
            public int dwExtraInfo;
        }

        /// <summary>
        /// 键盘按键抬起事件常量
        /// </summary>
        private const int WM_KEYUP = 0x0101;
        /// <summary>
        /// 表示键盘 <see langword="F12"/> 按键
        /// </summary>
        private const int VK_F12 = 0x7B;

        /// <summary>
        /// 用于执行挂钩过程的委托函数
        /// </summary>
        /// <param name="nCode">通知下个挂钩过程如何处理挂钩信息</param>
        /// <param name="wParam">主要挂钩数据</param>
        /// <param name="lParam">挂钩事件参数的相关标志信息的位组合数据</param>
        /// <returns>返回当前挂钩处理结果</returns>
        private delegate int HOOKPROC(int nCode, int wParam, IntPtr lParam);

        /// <summary>
        /// 表示用于监视低级别键盘输入事件的挂钩过程
        /// </summary>
        private const int WH_KEYBOARD_LL = 13;

        /// <summary>
        /// 安装全局HOOK挂钩
        /// <para>【对于低级别全局HOOK挂钩，显式设置dwThreadId参数将会引发Win32Error(1429)_只能全局设置该挂接过程】</para>
        /// </summary>
        /// <param name="idHook">要安装的挂钩过程的类型</param>
        /// <param name="lpfn">指向挂钩过程的指针</param>
        /// <param name="hmod">指向挂钩过程的dll的句柄，若挂接线程由当前进程创建，并且挂钩过程位于与当前进程关联的代码中，则必须为null</param>
        /// <param name="dwThreadId">要挂接的线程的线程标识符</param>
        /// <returns>返回挂接的挂钩句柄</returns>
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, HOOKPROC lpfn, IntPtr hmod, int dwThreadId);

        /// <summary>
        /// 传递挂钩数据并执行下一个挂钩函数
        /// </summary>
        /// <param name="idHook">此参数被忽略</param>
        /// <param name="nCode">确认如何处理挂钩信息</param>
        /// <param name="wParam">主要挂钩数据</param>
        /// <param name="lParam">挂钩事件参数的相关标志信息的位组合数据</param>
        /// <returns>返回值由链中的下一个挂钩过程返回</returns>
        [DllImport("user32.dll")]
        private static extern int CallNextHookEx(int idHook, int nCode, int wParam, IntPtr lParam);

        /// <summary>
        /// 卸载全局HOOK挂钩
        /// </summary>
        /// <param name="idHook">要卸载的挂钩的句柄</param>
        /// <returns>返回操作是否成功执行</returns>
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnhookWindowsHookEx(int idHook);

        /// <summary>
        /// 配置 <see href="Win32"/> 事件挂钩
        /// </summary>
        /// <param name="eventMin">最小事件常量</param>
        /// <param name="eventMax">最大事件常量</param>
        /// <param name="hmodWinEventProc">挂钩函数所在DLL的句柄</param>
        /// <param name="lpfnWinEventProc">指向挂钩函数的指针</param>
        /// <param name="idProcess">产生事件的目标进程</param>
        /// <param name="idThread">产生事件的目标线程</param>
        /// <param name="dwFlags">指定要跳过的挂钩函数和事件的位置</param>
        /// <returns>返回Win32事件挂钩实例</returns>
        [DllImport("user32.dll")]
        private static extern IntPtr SetWinEventHook(
            uint eventMin, uint eventMax, IntPtr hmodWinEventProc,
            WinEventDelegate lpfnWinEventProc, uint idProcess,
            uint idThread, uint dwFlags);

        /// <summary>
        /// 卸载 <see href="Win32"/> 事件挂钩
        /// </summary>
        /// <param name="hWinEventHook">目标Win32事件挂钩实例</param>
        /// <returns>返回操作是否成功</returns>
        [DllImport("user32.dll")]
        private static extern bool UnhookWinEvent(IntPtr hWinEventHook);

        /// <summary>
        /// 表示对象位置/大小发生变化的事件
        /// </summary>
        private const uint EVENT_OBJECT_LOCATIONCHANGE = 0x800B;
        /// <summary>
        /// 表示对象名称发生变化的事件
        /// </summary>
        private const uint EVENT_OBJECT_NAMECHANGE = 0x800C;
        /// <summary>
        /// 表示回调函数不会映射到生成事件的进程的地址空间中
        /// </summary>
        private const uint WINEVENT_OUTOFCONTEXT = 0x0000;
        /// <summary>
        /// 表示目标元素类型为窗口
        /// </summary>
        private const uint OBJID_WINDOW = 0x0000;
        /// <summary>
        /// 表示目标元素为自身
        /// </summary>
        private const uint CHILDID_SELF = 0;

        /// <summary>
        /// <see href="Win32"/> 事件挂钩的委托类型
        /// </summary>
        /// <param name="hWinEventHook">事件挂钩函数的句柄</param>
        /// <param name="eventType">发生的事件</param>
        /// <param name="hwnd">生成事件的窗口的句柄</param>
        /// <param name="idObject">与事件关联的对象</param>
        /// <param name="idChild">事件触发者</param>
        /// <param name="dwEventThread">指定生成事件的时间</param>
        /// <param name="dwmsEventTime">指定生成事件的时间</param>
        private delegate void WinEventDelegate(
            IntPtr hWinEventHook, uint eventType, IntPtr hwnd,
            int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

        /// <summary>
        /// 获取窗口是否处于最小化状态
        /// </summary>
        /// <param name="hWnd">目标窗口句柄</param>
        /// <returns>返回最小化状态</returns>
        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);



        /// <summary>
        /// 窗口信息结构
        /// </summary>
        private struct WindowData
        {
            public RECT winsize;
            public POINT clientsize;
            public string title;
            public bool moveflag;
            public long timestamp;
            public int blowcount;
        }
        /// <summary>
        /// 窗口字典
        /// </summary>
        private readonly Dictionary<IntPtr, WindowData> windowlist = new Dictionary<IntPtr, WindowData>(64);

        /// <summary>
        /// 数据读取同步器
        /// </summary>
        private readonly SemaphoreSlim ValueLock = new SemaphoreSlim(1, 1);

        /// <summary>
        /// 向列表添加数据
        /// </summary>
        /// <param name="window">目标窗口句柄</param>
        /// <param name="pname">目标进程名称</param>
        /// <param name="width">目标窗口宽度</param>
        /// <param name="height">目标窗口高度</param>
        /// <returns>返回异步方法实例</returns>
        /// <exception cref="Win32Exception">出现 <see cref="Win32Exception"/> 错误</exception>
        internal async Task<bool> Add(IntPtr window, string pname, int width, int height)
        {
            TaskCount.Add(out long tidvalue);
            // 计算数据
            RECT Screen = GetScreen();
            bool report = true;
            report &= GetWindowRect(window, out RECT window_rect);
            report &= GetClientRect(window, out RECT client_rect);
            POINT client_location = new POINT() { X = 0, Y = 0 };
            report &= ClientToScreen(window, ref client_location);
            if (!report)
            {
                TaskCount.Release(tidvalue);
                throw new Win32Exception(Marshal.GetLastWin32Error(),
                    $"获取窗口属性过程中出现Win32异常({Marshal.GetLastWin32Error().ToString().PadLeft(4, '0')})。");
            }
            RECT border = new RECT()
            {
                Left = client_location.X - window_rect.X,
                Top = client_location.Y - window_rect.Y,
                Right = window_rect.Width - client_rect.Width - (client_location.X - window_rect.X),
                Bottom = window_rect.Height - client_rect.Height - (client_location.Y - window_rect.Y)
            };
            width += border.Left + border.Right;
            height += border.Top + border.Bottom;
            if (width > Screen.Width) width = Screen.Width;
            if (height > Screen.Height) height = Screen.Height;
            int offsetx = Screen.X + (Screen.Width - width) / 2;
            int offsety = Screen.Y + (Screen.Height - height) * 2 / 3;
            RECT rect = new RECT()
            {
                Left = offsetx,
                Top = offsety,
                Right = offsetx + width,
                Bottom = offsety + height,
            };
            POINT _rect = new POINT()
            {
                X = rect.Width - border.Left - border.Right,
                Y = rect.Height - border.Top - border.Bottom
            };
            StringBuilder title = new StringBuilder(GetWindowTextLength(window) + 1);
            GetWindowText(window, title, title.Capacity);
            string _title = title.ToString().Trim().Replace(title1, string.Empty).Replace(title2, string.Empty);
            if (_title.Length > 0) pname = _title;
            // 开始配置
            for (int retry = 0; retry < 3; retry++)
            {
                if (TryWindowArea(window, rect, _rect)) break;
                await Task.Delay(1000);
                if (retry == 2) throw new Win32Exception(Marshal.GetLastWin32Error(),
                    $"配置窗口属性过程中出现Win32异常({short.MinValue.ToString().PadLeft(4, '0')})。");
            }
            bool result = false;
            await ValueLock.WaitAsync();
            if (!windowlist.ContainsKey(window))
            {
                windowlist.Add(window, new WindowData()
                {
                    winsize = rect,
                    clientsize = _rect,
                    title = pname,
                    moveflag = false,
                    timestamp = Environment.TickCount,
                    blowcount = 0
                });
                result = true;
            }
            ValueLock.Release();
            if (result)
            {
                SetWindow(window);
                SetMenu(window, false);
                SetWindowArea(window, rect, _rect, false);
                SetWindowTitle(window, pname, false);
            }
            TaskCount.Release(tidvalue);
            return result;
        }

        /// <summary>
        /// 从列表移除数据
        /// </summary>
        /// <param name="handle">目标窗口句柄</param>
        /// <returns>返回异步方法实例</returns>
        internal async Task Remove(IntPtr handle)
        {
            TaskCount.Add(out long tidvalue);
            await ValueLock.WaitAsync();
            windowlist.Remove(handle);
            ValueLock.Release();
            TaskCount.Release(tidvalue);
        }

        /// <summary>
        /// 对目标窗口样式进行配置
        /// </summary>
        /// <param name="window">目标窗口句柄</param>
        private void SetWindow(IntPtr window)
        {
            long style = GetWindowLongPtr(window, GWL_STYLE);
            if (style != 0L)
            {
                SetWindowLongPtr(window, GWL_STYLE, style & ~WS_MAXIMIZEBOX & ~WS_THICKFRAME);
                SetWindowPos(window, IntPtr.Zero, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE | SWP_NOZORDER | SWP_FRAMECHANGED);
            }
            if (IsLoaded) output($"窗口样式已更改 (0x{window.ToInt32():x8})", false);
        }

        /// <summary>
        /// 对目标窗口菜单进行配置
        /// </summary>
        /// <param name="window">目标窗口句柄</param>
        /// <param name="moveflag">移动控制标志 (是否允许窗口移动)</param>
        private void SetMenu(IntPtr window, bool moveflag)
        {
            if (moveflag)
            {
                _ = GetSystemMenu(window, true);
                if (IsLoaded) output($"位置锁定已停止 (0x{window.ToInt32():x8})", false);
            }
            else
            {
                IntPtr menuhwnd = GetSystemMenu(window, false);
                if (menuhwnd != IntPtr.Zero)
                {
                    RemoveMenu(menuhwnd, SC_MOVE, MF_BYCOMMAND);
                    if (IsLoaded) output($"位置锁定已激活 (0x{window.ToInt32():x8})", false);
                }
            }
        }

        /// <summary>
        /// 对目标窗口位置及大小进行配置
        /// </summary>
        /// <param name="window">目标窗口句柄</param>
        /// <param name="area">窗口显示范围</param>
        /// <param name="_area">窗口客户区大小</param>
        /// <param name="moveflag">移动控制标志 (是否允许窗口移动)</param>
        /// <returns>返回是否执行了有效操作</returns>
        private bool SetWindowArea(IntPtr window, RECT area, POINT _area, bool moveflag)
        {
            _ = SWA_Get(window, area, moveflag, out bool move, out bool size);
            if (SWA_Set(window, area, _area, move, size))
            {
                if (IsLoaded) output($"窗口位置&大小已重置 (0x{window.ToInt32():x8})", false);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 对目标窗口适应性进行测试
        /// </summary>
        /// <param name="window">目标窗口句柄</param>
        /// <param name="area">窗口显示范围</param>
        /// <param name="_area">窗口客户区大小</param>
        /// <returns>返回适应性测试是否通过</returns>
        private bool TryWindowArea(IntPtr window, RECT area, POINT _area)
        {
            _ = SWA_Get(window, area, false, out bool move, out bool size);
            _ = SWA_Set(window, area, _area, move, size);
            return !SWA_Get(window, area, false, out _, out _);
        }

        /// <summary>
        /// 获取窗口属性
        /// </summary>
        /// <param name="window">目标窗口句柄</param>
        /// <param name="area">窗口显示范围</param>
        /// <param name="moveflag">是否允许移动</param>
        /// <param name="move">[<see langword="out"/>] 当前状态是否需要移动</param>
        /// <param name="size">[<see langword="out"/>] 当前状态是否需要调整大小</param>
        /// <returns>返回是否需要设置属性</returns>
        private bool SWA_Get(IntPtr window, RECT area, bool moveflag, out bool move, out bool size)
        {
            move = true;
            size = true;
            if (GetWindowRect(window, out RECT checkout))
            {
                move = checkout.X != area.X || checkout.Y != area.Y;
                size = checkout.Width != area.Width || checkout.Height != area.Height;
            }
            move &= !moveflag;
            return move || size;
        }

        /// <summary>
        /// 配置窗口属性
        /// </summary>
        /// <param name="window">目标窗口句柄</param>
        /// <param name="area">窗口显示范围</param>
        /// <param name="_area">窗口客户区大小</param>
        /// <param name="move">是否需要移动</param>
        /// <param name="size">是否需要调整大小</param>
        /// <returns></returns>
        private bool SWA_Set(IntPtr window, RECT area, POINT _area, bool move, bool size)
        {
            if (move || size)
            {
                SetWindowPos(window, IntPtr.Zero,
                    area.X, area.Y, area.Width, area.Height,
                    SWP_DEFERERASE | SWP_NOCOPYBITS | SWP_NOZORDER | (move ? 0 : SWP_NOMOVE) | (size ? 0 : SWP_NOSIZE));
                if (size)
                {
                    long style = GetWindowLongPtr(window, GWL_STYLE);
                    if (style != 0L)
                    {
                        SetWindowLongPtr(window, GWL_STYLE, style & ~WS_CAPTION);
                        SetWindowLongPtr(window, GWL_STYLE, style);
                        SetWindowPos(window, IntPtr.Zero, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE | SWP_NOZORDER | SWP_FRAMECHANGED);
                    }
                    SendMessage(window, WM_SIZE, IntPtr.Zero, (IntPtr)(_area.Y << 16 | _area.X));
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// 字符串常量(窗口标题替换值)
        /// </summary>
        private const string title1 = " │ GWCT: 已加载 │ F12: 位置锁定已激活";
        /// <summary>
        /// 字符串常量(窗口标题替换值)
        /// </summary>
        private const string title2 = " │ GWCT: 已加载 │ F12: 位置锁定未激活";

        /// <summary>
        /// 对目标窗口标题进行配置
        /// </summary>
        /// <param name="window">目标窗口句柄</param>
        /// <param name="title">窗口标题</param>
        /// <param name="moveflag">移动控制标志 (是否允许窗口移动)</param>
        private void SetWindowTitle(IntPtr window, string title, bool moveflag)
        {
            title = $"{title}{(moveflag ? title2 : title1)}";
            StringBuilder _title = new StringBuilder(GetWindowTextLength(window) + 1);
            GetWindowText(window, _title, _title.Capacity);
            if (_title.ToString().Trim() != title)
            {
                SendMessage(window, WM_SETTEXT, IntPtr.Zero, title);
                if (IsLoaded) output($"窗口标题已重置 (0x{window.ToInt32():x8})", false);
            }
        }

        /// <summary>
        /// 配置熔断
        /// </summary>
        /// <param name="window">目标窗口</param>
        /// <param name="title">窗口标题</param>
        private void SetWindowBlow(IntPtr window, string title)
        {
            SendMessage(window, WM_SETTEXT, IntPtr.Zero, $"{title} │ GWCT: 已熔断");
            if (IsLoaded) output($"窗口已触发熔断，监控已暂停 (0x{window.ToInt32():x8})", null);
        }

        /// <summary>
        /// 获取窗口矩形
        /// </summary>
        /// <param name="hWnd">目标窗口句柄</param>
        /// <param name="lpRect">[<see langword="out"/>] 目标窗口的窗口矩形</param>
        /// <returns>返回操作是否成功</returns>
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        /// <summary>
        /// 获取客户矩形
        /// </summary>
        /// <param name="hWnd">目标窗口句柄</param>
        /// <param name="lpRect">[<see langword="out"/>] 目标窗口的客户矩形</param>
        /// <returns>返回操作是否成功</returns>
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

        /// <summary>
        /// 将客户区坐标转换为屏幕坐标
        /// </summary>
        /// <param name="hWnd">目标窗口句柄</param>
        /// <param name="lpPoint">[<see langword="ref"/>] 要转换的客户区坐标(标准值为0,0)</param>
        /// <returns>返回操作是否成功</returns>
        [DllImport("user32.dll")]
        private static extern bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint);

        /// <summary>
        /// 阻止生成 <see langword="WM_SYNCPAINT"/> 消息
        /// </summary>
        private const int SWP_DEFERERASE = 0x2000;
        /// <summary>
        /// 丢弃工作区的整个内容
        /// </summary>
        private const int SWP_NOCOPYBITS = 0x0100;
        /// <summary>
        /// 指示不改变窗口Z序
        /// </summary>
        private const int SWP_NOZORDER = 0x0004;
        /// <summary>
        /// 指示不改变窗口位置
        /// </summary>
        private const int SWP_NOMOVE = 0x0002;
        /// <summary>
        /// 指示不改变窗口大小
        /// </summary>
        private const int SWP_NOSIZE = 0x0001;
        /// <summary>
        /// 指示重新计算窗口框架
        /// </summary>
        private const int SWP_FRAMECHANGED = 0x0020;

        /// <summary>
        /// 调整窗口空间信息
        /// </summary>
        /// <param name="hwnd">目标窗口坐标</param>
        /// <param name="hWndInsertAfter">指示窗口Z序如何变化</param>
        /// <param name="x">窗口左上角横坐标</param>
        /// <param name="y">窗口左上角纵坐标</param>
        /// <param name="cx">窗口宽度</param>
        /// <param name="cy">窗口高度</param>
        /// <param name="wFlags">窗口空间信息修改规则</param>
        /// <returns>返回操作是否成功</returns>
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hwnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, int wFlags);

        /// <summary>
        /// 需要设置窗口的标题文本
        /// </summary>
        private const int WM_SETTEXT = 0x000C;
        /// <summary>
        /// 需要设置窗口的大小
        /// </summary>
        private const int WM_SIZE = 0x0005;

        /// <summary>
        /// 向指定窗口发送指定的 <see href="Win32"/> 消息
        /// </summary>
        /// <param name="hWnd">窗口句柄</param>
        /// <param name="Msg">消息常量</param>
        /// <param name="wParam">消息参数</param>
        /// <param name="lParam">消息参数</param>
        /// <returns>返回消息的处理结果</returns>
        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
        /// <summary>
        /// 向指定窗口发送指定的 <see href="Win32"/> 消息(仅用于发送字符串消息)
        /// </summary>
        /// <param name="hWnd">窗口句柄</param>
        /// <param name="Msg">消息常量</param>
        /// <param name="wParam">消息参数</param>
        /// <param name="lParam">消息参数(仅传递字符串对象)</param>
        /// <returns>返回消息的处理结果</returns>
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, string lParam);

        /// <summary>
        /// 获取窗口标题字符串长度
        /// </summary>
        /// <param name="hwnd">目标窗口句柄</param>
        /// <returns>返回窗口标题字符串的长度</returns>
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowTextLength(IntPtr hwnd);

        /// <summary>
        /// 获取窗口标题字符串
        /// </summary>
        /// <param name="hwnd">目标窗口句柄</param>
        /// <param name="lpString">存储字符串的对象</param>
        /// <param name="nMaxCount">获取字符的最大长度</param>
        /// <returns>返回获取到的字符串长度</returns>
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowText(IntPtr hwnd, StringBuilder lpString, int nMaxCount);

        /// <summary>
        /// 获取窗口菜单句柄
        /// </summary>
        /// <param name="hWnd">窗口句柄</param>
        /// <param name="bRevert">是否恢复到保存的窗口菜单副本</param>
        /// <returns>窗口菜单句柄</returns>
        [DllImport("user32.dll")]
        public static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

        /// <summary>
        /// 表示窗口移动菜单项
        /// </summary>
        private const int SC_MOVE = 0xF010;
        /// <summary>
        /// 表示通过命令常量查找菜单项
        /// </summary>
        private const int MF_BYCOMMAND = 0;

        /// <summary>
        /// 移除窗口菜单的菜单项
        /// </summary>
        /// <param name="hMenu">窗口菜单句柄</param>
        /// <param name="nPos">窗口菜单项的索引/常量</param>
        /// <param name="flags">指示查找菜单项的方式</param>
        /// <returns>返回操作是否成功</returns>
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RemoveMenu(IntPtr hMenu, int nPos, int flags);

        /// <summary>
        /// 获取当前正在前台显示的窗口句柄
        /// </summary>
        /// <returns>返回当前正在前台显示的窗口句柄</returns>
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        /// <summary>
        /// 表示窗口样式
        /// </summary>
        private const int GWL_STYLE = -16;
        /// <summary>
        /// 表示窗口具有最大化窗口
        /// </summary>
        private const long WS_MAXIMIZEBOX = 0x00010000L;
        /// <summary>
        /// 表示窗口具有标题栏
        /// </summary>
        private const long WS_CAPTION = 0x00C00000L;
        /// <summary>
        /// 表示窗口具有大小调整边框
        /// </summary>
        private const long WS_THICKFRAME = 0x00040000L;

        /// <summary>
        /// 获取窗口属性
        /// </summary>
        /// <param name="hWnd">窗口句柄</param>
        /// <param name="nIndex">属性类型索引</param>
        /// <returns>返回当前窗口属性</returns>
        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern long GetWindowLong64(IntPtr hWnd, long nIndex);
        /// <summary>
        /// 获取窗口属性
        /// </summary>
        /// <param name="hWnd">窗口句柄</param>
        /// <param name="nIndex">属性类型索引</param>
        /// <returns>返回当前窗口属性</returns>
        [DllImport("user32.dll", EntryPoint = "GetWindowLong", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern long GetWindowLong32(IntPtr hWnd, long nIndex);

        /// <summary>
        /// 设置窗口属性
        /// </summary>
        /// <param name="hWnd">窗口句柄</param>
        /// <param name="nIndex">属性类型索引</param>
        /// <param name="dwNewLong">新的窗口属性</param>
        /// <returns>返回之前的窗口属性</returns>
        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern long SetWindowLong64(IntPtr hWnd, int nIndex, long dwNewLong);
        /// <summary>
        /// 设置窗口属性
        /// </summary>
        /// <param name="hWnd">窗口句柄</param>
        /// <param name="nIndex">属性类型索引</param>
        /// <param name="dwNewLong">新的窗口属性</param>
        /// <returns>返回之前的窗口属性</returns>
        [DllImport("user32.dll", EntryPoint = "SetWindowLong", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern long SetWindowLong32(IntPtr hWnd, int nIndex, long dwNewLong);

        /// <summary>
        /// 获取窗口属性
        /// </summary>
        /// <param name="hWnd">窗口句柄</param>
        /// <param name="nIndex">属性类型索引</param>
        /// <returns>返回当前窗口属性</returns>
        private static long GetWindowLongPtr(IntPtr hWnd, int nIndex)
        {
            if (Environment.Is64BitProcess)
            {
                return GetWindowLong64(hWnd, nIndex);
            }
            else
            {
                return GetWindowLong32(hWnd, nIndex);
            }
        }
        /// <summary>
        /// 设置窗口属性
        /// </summary>
        /// <param name="hWnd">窗口句柄</param>
        /// <param name="nIndex">属性类型索引</param>
        /// <param name="dwNewLong">新的窗口属性</param>
        /// <returns>返回之前的窗口属性</returns>
        private static long SetWindowLongPtr(IntPtr hWnd, int nIndex, long dwNewLong)
        {
            if (Environment.Is64BitProcess)
            {
                return SetWindowLong64(hWnd, nIndex, dwNewLong);
            }
            else
            {
                return SetWindowLong32(hWnd, nIndex, dwNewLong);
            }
        }
    }
}
