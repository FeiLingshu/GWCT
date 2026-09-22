using System;
using System.Runtime.InteropServices;
using static GWCT.CoreCtrl.API;

namespace GWCT.CoreCtrl
{
    /// <summary>
    /// 效能模式专用模块
    /// </summary>
    public static class EcoQoS
    {
        /// <summary>
        /// 尝试配置效能模式
        /// <para>
        /// 警告：仅在 <see href="Windows"/> 版本 <see langword="10,0,16299"/> 及更新版本中受支持
        /// </para>
        /// </summary>
        /// <param name="PID">目标进程ID</param>
        /// <param name="bFlag">开启标志</param>
        /// <returns>返回操作是否成功</returns>
        public static bool Set(int PID, bool bFlag)
        {
            if (Environment.OSVersion.Version < new Version(10, 0, 16299)) return false;
            IntPtr hProcess = IntPtr.Zero;
            try
            {
                hProcess = OpenProcess(
                    ProcessAccessFlags.PROCESS_SET_INFORMATION | ProcessAccessFlags.PROCESS_QUERY_INFORMATION,
                    false,
                    PID);
                // 此结构有三个字段Version，ControlMask 和 StateMask
                uint version = 1;
                uint controlMask = 0x1;
                uint stateMask = (uint)(bFlag ? 0x1 : 0x0);
                int szControlBlock = 12; // 三个uint的大小
                IntPtr homo = Marshal.AllocHGlobal(szControlBlock); //homo 指向内存块开头
                Marshal.WriteInt32(homo, (int)version);
                Marshal.WriteInt32(homo + 4, (int)controlMask);
                Marshal.WriteInt32(homo + 8, (int)stateMask);
                bool result = true;
                result &= SetProcessInformation(hProcess, PROCESS_INFORMATION_CLASS.ProcessPowerThrottling, homo, (uint)szControlBlock);
                result &= SetPriorityClass(hProcess, (uint)(bFlag ? 0x40 : 0x20));
                Marshal.FreeHGlobal(homo);
                return result;
            }
            finally
            {
                if (hProcess != IntPtr.Zero) CloseHandle(hProcess);
            }
        }
    }
}
