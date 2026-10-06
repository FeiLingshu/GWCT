using System;
using System.Runtime.InteropServices;
using System.Text;

namespace GWCT.CoreCtrl
{
    /// <summary>
    /// 提供相关 <see href="Win32"/> 函数调用
    /// </summary>
    public static class API
    {
        /// <summary>
        /// win32enum::ProcessAccessFlags
        /// </summary>
        [Flags]
        public enum ProcessAccessFlags : uint
        {
            /// <summary>
            /// ::PROCESS_SET_INFORMATION
            /// </summary>
            PROCESS_SET_INFORMATION = 0x00000200,
            /// <summary>
            /// ::PROCESS_QUERY_INFORMATION
            /// </summary>
            PROCESS_QUERY_INFORMATION = 0x00000400,
            /// <summary>
            /// ::PROCESS_VM_READ
            /// </summary>
            PROCESS_VM_READ = 0x00000010
        }

        /// <summary>
        /// win32api::OpenProcess
        /// </summary>
        /// <param name="processAccess">param#1</param>
        /// <param name="bInheritHandle">param#2</param>
        /// <param name="processId">param#3</param>
        /// <returns>returns</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenProcess(
            ProcessAccessFlags processAccess,
            bool bInheritHandle,
            int processId);

        /// <summary>
        /// win32api::CloseHandle
        /// </summary>
        /// <param name="hObject">param#1</param>
        /// <returns>returns</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr hObject);

        /// <summary>
        /// win32enum::PROCESS_INFORMATION_CLASS
        /// </summary>
        public enum PROCESS_INFORMATION_CLASS
        {
            /// <summary>
            /// ::ProcessMemoryPriority
            /// </summary>
            ProcessMemoryPriority,
            /// <summary>
            /// ::ProcessMemoryExhaustionInfo
            /// </summary>
            ProcessMemoryExhaustionInfo,
            /// <summary>
            /// ::ProcessAppMemoryInfo
            /// </summary>
            ProcessAppMemoryInfo,
            /// <summary>
            /// ::ProcessInPrivateInfo
            /// </summary>
            ProcessInPrivateInfo,
            /// <summary>
            /// ::ProcessPowerThrottling
            /// </summary>
            ProcessPowerThrottling,
            /// <summary>
            /// ::ProcessReservedValue1
            /// </summary>
            ProcessReservedValue1,
            /// <summary>
            /// ::ProcessTelemetryCoverageInfo
            /// </summary>
            ProcessTelemetryCoverageInfo,
            /// <summary>
            /// ::ProcessProtectionLevelInfo
            /// </summary>
            ProcessProtectionLevelInfo,
            /// <summary>
            /// ::ProcessLeapSecondInfo
            /// </summary>
            ProcessLeapSecondInfo,
            /// <summary>
            /// ::ProcessInformationClassMax
            /// </summary>
            ProcessInformationClassMax
        }

        /// <summary>
        /// win32api::SetProcessInformation
        /// </summary>
        /// <param name="hProcess">param#1</param>
        /// <param name="ProcessInformationClass">param#2</param>
        /// <param name="ProcessInformation">param#3</param>
        /// <param name="ProcessInformationSize">param#4</param>
        /// <returns>return value</returns>
        [DllImport("kernel32.dll")]
        public static extern bool SetProcessInformation([In] IntPtr hProcess,
            [In] PROCESS_INFORMATION_CLASS ProcessInformationClass, IntPtr ProcessInformation, uint ProcessInformationSize);

        /// <summary>
        /// win32api::SetPriorityClass
        /// </summary>
        /// <param name="handle">param#1</param>
        /// <param name="priorityClass">param#2</param>
        /// <returns>return value</returns>
        [DllImport("kernel32.dll")]
        public static extern bool SetPriorityClass(IntPtr handle, uint priorityClass);

        /// <summary>
        /// win32api::GetProcessAffinityMask
        /// </summary>
        /// <param name="hProcess">param#1</param>
        /// <param name="lpProcessAffinityMask">param#2</param>
        /// <param name="lpSystemAffinityMask">param#3</param>
        /// <returns></returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool GetProcessAffinityMask(
            IntPtr hProcess,
            out UIntPtr lpProcessAffinityMask,
            out UIntPtr lpSystemAffinityMask);

        /// <summary>
        /// win32api::SetProcessAffinityMask
        /// </summary>
        /// <param name="hProcess">param#1</param>
        /// <param name="dwProcessAffinityMask">param#2</param>
        /// <returns>returns</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetProcessAffinityMask(IntPtr hProcess, UIntPtr dwProcessAffinityMask);

        /// <summary>
        /// win32api::QueryFullProcessImageName
        /// </summary>
        /// <param name="hProcess">param#1</param>
        /// <param name="dwFlags">param#2</param>
        /// <param name="lpExeName">param#3</param>
        /// <param name="lpdwSize">param#4</param>
        /// <returns>returns</returns>
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool QueryFullProcessImageName(
            IntPtr hProcess,
            uint dwFlags,
            StringBuilder lpExeName,
            ref uint lpdwSize);
    }
}
