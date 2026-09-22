using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using static GWCT.CoreCtrl.API;

namespace GWCT.CoreCtrl
{
    /// <summary>
    /// 提供 <see href="CPU"/> 核心信息读取功能
    /// </summary>
    public static class Core
    {
        /// <summary>
        /// 全局标志，指示组件是否准备就绪
        /// </summary>
        public static volatile bool IsReady = false;

        /// <summary>
        /// 内部字段，存储核心掩码
        /// </summary>
        private static ulong _PMap = 0UL;

        /// <summary>
        /// 获取核心掩码信息 (P核，如果可能)
        /// <para>
        /// 已通过 <see cref="Volatile"/> 确保线程安全
        /// </para>
        /// </summary>
        public static ulong PMap
        {
            get { return Volatile.Read(ref _PMap); }
        }

        /// <summary>
        /// 获取核心亲和性掩码数据
        /// </summary>
        /// <param name="hProcess">目标进程句柄</param>
        /// <param name="mask">可用最大掩码</param>
        /// <returns>返回当前核心掩码</returns>
        private static ulong GetBasicMap(IntPtr hProcess, out ulong mask)
        {
            if (GetProcessAffinityMask(hProcess, out UIntPtr map, out UIntPtr _mask))
            {
                mask = _mask.ToUInt64();
                return map.ToUInt64();
            }
            mask = 0UL;
            return 0UL;
        }

        /// <summary>
        /// 读取核心掩码 (P核，如果可能)
        /// <para>
        /// 该方法会设置 <see cref="IsReady"/> 标志
        /// </para>
        /// </summary>
        /// <param name="is_override">[<see langword="out"/>] 指示是否被本地配置覆写</param>
        /// <returns>返回执行结果</returns>
        public static short GetCoreMap(out bool is_override)
        {
            is_override = false;
            try
            {
                ulong basic = 0UL;
                using (Process self = Process.GetCurrentProcess())
                {
                    basic = (ulong)self.ProcessorAffinity.ToInt64();
                }
                short report = CPUID.GetPCoreMaps(out ulong map);
                if (report == 0 && map > 0 && map < basic)
                {
                    Volatile.Write(ref _PMap, map);
                    IsReady = true;
                }
                else
                {
                    Volatile.Write(ref _PMap, basic);
                    IsReady = true;
                    is_override = true;
                }
                return report;
            }
            catch (Exception)
            {
                Volatile.Write(ref _PMap, 0UL);
                IsReady = false;
                return -2;
            }
        }

        /// <summary>
        /// 设置指定进程的 <see href="CPU"/> 亲和性
        /// <para>
        /// 警告：自身进程应使用 <see cref="System.Diagnostics.Process.ProcessorAffinity"/> 进行配置，以避免权限问题
        /// </para>
        /// </summary>
        /// <param name="pid">目标进程ID</param>
        /// <param name="map">目标核心掩码</param>
        /// <param name="skip">指示是否自动跳过了配置过程</param>
        /// <returns>返回执行结果<br/>
        /// <see cref="short.MinValue"/>表示未就绪<br/>
        /// <see langword="-1"/>表示访问目标进程失败<br/>
        /// <see langword="0"/>表示成功<br/>
        /// 其他非零值表示 <see href="Win32"/> 错误代码</returns>
        internal static short SetProcess(int pid, ulong map, out bool skip)
        {
            skip = false;
            if (!IsReady) return short.MinValue;
            IntPtr hProcess = OpenProcess(
                ProcessAccessFlags.PROCESS_SET_INFORMATION | ProcessAccessFlags.PROCESS_QUERY_INFORMATION,
                false,
                pid);
            if (hProcess != IntPtr.Zero)
            {
                try
                {
                    ulong procmap = GetBasicMap(hProcess, out _);
                    if (procmap == map)
                    {
                        skip = true;
                        return 0;
                    }
                    else
                    {
                        if (SetProcessAffinityMask(hProcess, new UIntPtr(map)))
                        {
                            return 0;
                        }
                        else
                        {
                            return (short)Marshal.GetLastWin32Error();
                        }
                    }
                }
                finally
                {
                    CloseHandle(hProcess);
                }
            }
            return -1;
        }
    }
}
