using System;
using System.Runtime.InteropServices;
using System.Windows.Media;

namespace GWCT.Tool
{
    /// <summary>
    /// 提供 <see href="DWM"/> 互操作
    /// </summary>
    public static class DWM
    {
        /// <summary>
        /// 配置窗口 <see href="DWM"/> 参数
        /// </summary>
        /// <param name="window">目标窗口句柄</param>
        /// <param name="C2NC">是否允许工作区绘制</param>
        /// <param name="DARK">是否允许同步暗色模式配置</param>
        /// <param name="B_COLORREF">标题栏颜色</param>
        /// <param name="T_COLORREF">标题文本颜色</param>
        /// <param name="CORNER">是否启用圆角 (<see langword="true"/>：标准圆角，<see langword="false"/>：较小圆角，<see langword="null"/>：无圆角)</param>
        /// <returns>返回操作是否成功执行</returns>
        public static bool Set(this IntPtr window, bool C2NC, bool DARK, int B_COLORREF, int T_COLORREF, bool? CORNER)
        {
            if (!DwmIsCompositionEnabled()) return false;
            // 系统版本检测：
            // Windows Vista -> 6.0.6000 (RTM)
            //                  6.0.6001 (SP1)
            //                  6.0.6002 (SP2)
            // Windows 7     -> 6.1.7600 (RTM)
            //                  6.1.7601 (SP1)
            // Windows 8     -> 6.2.9200
            // Windows 8.1   -> 6.3.9600
            // Windows 10    -> 10.0.10240
            // Windows 11    -> 10.0.22000
            Version sysver = Environment.OSVersion.Version;
            if (sysver >= new Version(6, 0, 6000))
            {
                bool dwm_result = true;
                bool isDWMenable = false;
                DwmGetWindowAttribute(
                    window,
                    DWMWINDOWATTRIBUTE.DWMWA_NCRENDERING_ENABLED,
                    ref isDWMenable,
                    sizeof(int));
                uint flag_0 = (uint)DWMNCRENDERINGPOLICY.DWMNCRP_ENABLED;
                if (isDWMenable || DwmSetWindowAttribute(
                    window,
                    DWMWINDOWATTRIBUTE.DWMWA_NCRENDERING_POLICY,
                    ref flag_0,
                    sizeof(uint)) == S_OK)
                {
                    bool flag_1 = true;
                    if (C2NC) DwmSetWindowAttribute(
                        window,
                        DWMWINDOWATTRIBUTE.DWMWA_ALLOW_NCPAINT,
                        ref flag_1,
                        sizeof(int));
                    if (sysver >= new Version(10, 0, 17763))
                    {
                        bool flag_2 = true;
                        DWMWINDOWATTRIBUTE ENUM = DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE_20H1;
                        if (sysver.Build >= 18985)
                        {
                            ENUM = DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE;
                        }
                        if (DARK) DwmSetWindowAttribute(
                            window,
                            ENUM,
                            ref flag_2,
                            sizeof(int));
                        if (sysver.Build >= 19041)
                        {
                            dwm_result &= DwmSetWindowAttribute(
                                window,
                                DWMWINDOWATTRIBUTE.DWMWA_CAPTION_COLOR,
                                ref B_COLORREF,
                                sizeof(uint)) == S_OK;
                            dwm_result &= DwmSetWindowAttribute(
                                window,
                                DWMWINDOWATTRIBUTE.DWMWA_TEXT_COLOR,
                                ref T_COLORREF,
                                sizeof(uint)) == S_OK;
                        }
                    }
                    if (sysver >= new Version(10, 0, 22000))
                    {
                        uint flag_3 = CORNER.HasValue
                            ? (CORNER.Value
                                ? (uint)DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_ROUND
                                : (uint)DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_ROUNDSMALL)
                            : (uint)DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_SQUARE;
                        DwmSetWindowAttribute(
                            window,
                            DWMWINDOWATTRIBUTE.DWMWA_WINDOW_CORNER_PREFERENCE,
                            ref flag_3,
                            sizeof(uint));
                    }
                    MARGINS margins = new MARGINS() { Left = 0, Top = 0, Right = 0, Bottom = 0 };
                    if (C2NC) DwmExtendFrameIntoClientArea(window, ref margins);
                    return dwm_result;
                }
            }
            return false;
        }

        /// <summary>
        /// 强制应用程序使用深色原生控件
        /// </summary>
        /// <param name="flush">是否强制刷新</param>
        public static void EnableDarkModeForNativeControls(bool flush)
        {
            try
            {
                // 检查操作系统版本，仅 Windows 10 1903 (Build 18362) 及以上支持
                if (Environment.OSVersion.Version >= new Version(10, 0, 18362))
                {
                    SetPreferredAppMode(APP_MODE_FORCE_DARK);
                    if (flush) FlushMenuThemes();
                }
                // 检查操作系统版本，仅 Windows 10 1809 (Build 17763) 及以上支持
                else if (Environment.OSVersion.Version >= new Version(10, 0, 17763))
                {
                    AllowDarkModeForApp(true);
                    if (flush) FlushMenuThemes();
                }
            }
            catch (EntryPointNotFoundException) { }
            catch (DllNotFoundException) { }
            catch (Exception) { throw; }
        }

        /// <summary>
        /// 用于获取 <see href="Win32"/> 数据类型 <see href="COLORREF"/> 的扩展方法
        /// </summary>
        /// <param name="color">目标颜色值</param>
        /// <returns>返回目标颜色值的 <see href="COLORREF"/> 形式数据</returns>
        public static int TO_COLORREF(this Color color)
        {
            return (color.B << 16) | (color.G << 8) | color.R;
        }

        /// <summary>
        /// 用于将 <see href="Win32"/> 数据类型 <see href="COLORREF"/> 转换为 <see cref="Color"/> 类型实例的扩展方法
        /// </summary>
        /// <param name="COLORREF">目标颜色的 <see href="COLORREF"/> 值</param>
        /// <returns>返回目标颜色的 <see cref="Color"/> 实例</returns>
        public static Color FROM_COLORREF(this int COLORREF)
        {
            byte blue = (byte)(COLORREF & 0xFF);
            byte green = (byte)((COLORREF >> 8) & 0xFF);
            byte red = (byte)((COLORREF >> 16) & 0xFF);
            return Color.FromArgb(0xFF, red, green, blue);
        }



        /// <summary>
        /// 获取 <see href="DWM"/> 是否启用
        /// </summary>
        /// <returns>返回当前设备中 <see href="DWM"/> 的启用情况</returns>
        [DllImport("Dwmapi.dll", ExactSpelling = true, PreserveSig = false)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DwmIsCompositionEnabled();

        /// <summary>
        /// 声明 <see href="Win32"/> 枚举DWMWINDOWATTRIBUTE
        /// </summary>
        private enum DWMWINDOWATTRIBUTE : uint
        {
            /// <summary>
            /// 获取当前 <see href="DWM"/> 状态
            /// </summary>
            DWMWA_NCRENDERING_ENABLED = 1,
            /// <summary>
            /// 配置 <see href="DWM"/> 状态
            /// </summary>
            DWMWA_NCRENDERING_POLICY = 2,
            /// <summary>
            /// 配置是否允许渲染工作区
            /// </summary>
            DWMWA_ALLOW_NCPAINT = 3,
            /// <summary>
            /// 配置是否同步系统暗色模式配置(17763~18985)
            /// </summary>
            DWMWA_USE_IMMERSIVE_DARK_MODE_20H1 = 19,
            /// <summary>
            /// 配置是否同步系统暗色模式配置
            /// </summary>
            DWMWA_USE_IMMERSIVE_DARK_MODE = 20,
            /// <summary>
            /// 配置窗口圆角参数
            /// </summary>
            DWMWA_WINDOW_CORNER_PREFERENCE = 33,
            /// <summary>
            /// 配置窗口标题栏颜色
            /// </summary>
            DWMWA_CAPTION_COLOR = 35,
            /// <summary>
            /// 配置窗口标题文本颜色
            /// </summary>
            DWMWA_TEXT_COLOR = 36
        }

        /// <summary>
        /// 声明 <see href="Win32"/> 枚举 <see href="DWMNCRENDERINGPOLICY"/> 
        /// </summary>
        private enum DWMNCRENDERINGPOLICY : uint
        {
            /// <summary>
            /// 使用系统 <see href="DWM"/> 配置
            /// </summary>
            DWMNCRP_USEWINDOWSTYLE = 0,
            /// <summary>
            /// 禁用 <see href="DWM"/>
            /// </summary>
            DWMNCRP_DISABLED = 1,
            /// <summary>
            /// 启用 <see href="DWM"/>
            /// </summary>
            DWMNCRP_ENABLED = 2
        }

        /// <summary>
        /// 声明 <see href="Win32"/> 枚举 <see href="DWM_WINDOW_CORNER_PREFERENCE"/>
        /// </summary>
        private enum DWM_WINDOW_CORNER_PREFERENCE : uint
        {
            /// <summary>
            /// 默认圆角风格
            /// </summary>
            DWMWCP_DEFAULT = 0,
            /// <summary>
            /// 无圆角
            /// </summary>
            DWMWCP_SQUARE = 1,
            /// <summary>
            /// 标准圆角
            /// </summary>
            DWMWCP_ROUND = 2,
            /// <summary>
            /// 较小圆角
            /// </summary>
            DWMWCP_ROUNDSMALL = 3
        }

        /// <summary>
        /// 声明 <see href="Win32"/> 结构MARGINS
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct MARGINS
        {
            /// <summary>
            /// 左边距
            /// </summary>
            public int Left;
            /// <summary>
            /// 右边距
            /// </summary>
            public int Right;
            /// <summary>
            /// 上边距
            /// </summary>
            public int Top;
            /// <summary>
            /// 下边距
            /// </summary>
            public int Bottom;
        }

        /// <summary>
        /// 获取/设置指定窗口的 <see href="DWM"/> 配置
        /// </summary>
        /// <param name="hwnd">目标窗口句柄</param>
        /// <param name="dwAttribute">要获取/设置的 <see href="DWM"/> 属性枚举</param>
        /// <param name="pvAttribute">承载 <see href="DWM"/> 属性值的特定数据类型实例</param>
        /// <param name="cbAttribute">属性值的内存大小</param>
        /// <returns>返回操作是否成功(S_OK为成功，否则失败)</returns>
        [DllImport("dwmapi.dll", EntryPoint = "DwmSetWindowAttribute", PreserveSig = true)]
        private static extern int DwmGetWindowAttribute(
            IntPtr hwnd,
            DWMWINDOWATTRIBUTE dwAttribute,
            ref bool pvAttribute,
            uint cbAttribute);

        /// <summary>
        /// 获取/设置指定窗口的 <see href="DWM"/> 配置
        /// </summary>
        /// <param name="hwnd">目标窗口句柄</param>
        /// <param name="dwAttribute">要获取/设置的 <see href="DWM"/> 属性枚举</param>
        /// <param name="pvAttribute">承载 <see href="DWM"/> 属性值的特定数据类型实例</param>
        /// <param name="cbAttribute">属性值的内存大小</param>
        /// <returns>返回操作是否成功 (<see langword="S_OK"/> 为成功，否则失败)</returns>
        [DllImport("dwmapi.dll", EntryPoint = "DwmSetWindowAttribute", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(
            IntPtr hwnd,
            DWMWINDOWATTRIBUTE dwAttribute,
            ref bool pvAttribute,
            uint cbAttribute);

        /// <summary>
        /// 获取/设置指定窗口的 <see href="DWM"/> 配置
        /// </summary>
        /// <param name="hwnd">目标窗口句柄</param>
        /// <param name="dwAttribute">要获取/设置的 <see href="DWM"/> 属性枚举</param>
        /// <param name="pvAttribute">承载 <see href="DWM"/> 属性值的特定数据类型实例</param>
        /// <param name="cbAttribute">属性值的内存大小</param>
        /// <returns>返回操作是否成功 (<see langword="S_OK"/> 为成功，否则失败)</returns>
        [DllImport("dwmapi.dll", EntryPoint = "DwmSetWindowAttribute", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(
            IntPtr hwnd,
            DWMWINDOWATTRIBUTE dwAttribute,
            ref uint pvAttribute,
            uint cbAttribute);

        /// <summary>
        /// 获取/设置指定窗口的 <see href="DWM"/> 配置
        /// </summary>
        /// <param name="hwnd">目标窗口句柄</param>
        /// <param name="dwAttribute">要获取/设置的 <see href="DWM"/> 属性枚举</param>
        /// <param name="pvAttribute">承载 <see href="DWM"/> 属性值的特定数据类型实例</param>
        /// <param name="cbAttribute">属性值的内存大小</param>
        /// <returns>返回操作是否成功 (<see langword="S_OK"/> 为成功，否则失败)</returns>
        [DllImport("dwmapi.dll", EntryPoint = "DwmSetWindowAttribute", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(
            IntPtr hwnd,
            DWMWINDOWATTRIBUTE dwAttribute,
            ref int pvAttribute,
            uint cbAttribute);

        /// <summary>
        /// 设置窗口工作区包含的窗口边框
        /// </summary>
        /// <param name="hwnd">目标窗口句柄</param>
        /// <param name="margins">指示窗口边框的 <see href="Win32"/> 结构 <see href="MARGINS"/> </param>
        /// <returns>返回操作是否成功 (<see cref="S_OK"/> 为成功，否则失败)</returns>
        [DllImport("dwmapi.dll", EntryPoint = "DwmExtendFrameIntoClientArea", PreserveSig = true)]
        private static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref MARGINS margins);

        /// <summary>
        /// 指示 <see href="DWM"/> 函数操作成功的值
        /// </summary>
        private const int S_OK = 0;

        /// <summary>
        /// [内部函数#135] 设置应用主题模式
        /// </summary>
        /// <param name="allow">是否使用深色模式</param>
        [DllImport("uxtheme.dll", EntryPoint = "#135")]
        public static extern void AllowDarkModeForApp(bool allow);
        /// <summary>
        /// 主题枚举，强制使用暗色模式
        /// </summary>
        private const int APP_MODE_FORCE_DARK = 2;
        /// <summary>
        /// [内部函数#135] 设置应用主题模式
        /// </summary>
        /// <param name="preferredAppMode">目标主题枚举</param>
        /// <returns>返回执行结果 (<see cref="S_OK"/> 表示成功)</returns>
        [DllImport("uxtheme.dll", EntryPoint = "#135")]
        private static extern int SetPreferredAppMode(int preferredAppMode);
        /// <summary>
        /// 立即刷新主题模式
        /// </summary>
        [DllImport("uxtheme.dll", EntryPoint = "#136")]
        private static extern void FlushMenuThemes();
    }
}
