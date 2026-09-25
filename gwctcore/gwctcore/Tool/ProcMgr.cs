using GWCT.Cfg;
using GWCT.CoreCtrl;
using GWCT.Datas;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using static GWCT.CoreCtrl.API;

namespace GWCT.Tool
{
    /// <summary>
    /// 核心组件，进程管理器
    /// </summary>
    public class ProcMgr
    {
        /// <summary>
        /// 配置文件模块实例
        /// </summary>
        private readonly Bin bin;
        /// <summary>
        /// 窗口管理器实例
        /// </summary>
        private readonly WinCtrl winctrl;

        /// <summary>
        /// 默认构造函数
        /// </summary>
        /// <param name="bin">传递配置文件模块实例</param>
        /// <param name="winctrl">传递窗口管理器实例</param>
        public ProcMgr(Bin bin, WinCtrl winctrl)
        {
            this.bin = bin;
            this.winctrl = winctrl;
            Paths = new string[0];
            Width = 1920;
            Height = 1080;
            CoreFlag = false;
        }

        /// <summary>
        /// 保存路径信息
        /// </summary>
        private string[] Paths;
        /// <summary>
        /// 保存窗口宽度信息
        /// </summary>
        private ushort Width;
        /// <summary>
        /// 保存窗口高度信息
        /// </summary>
        private ushort Height;
        /// <summary>
        /// 保存核心亲和性标志
        /// </summary>
        private bool CoreFlag;

        /// <summary>
        /// 全局暂停列表
        /// </summary>
        private readonly HashSet<string> Pause = new HashSet<string>(64);

        /// <summary>
        /// 数据读写同步组件
        /// </summary>
        private readonly SemaphoreSlim ValueLock = new SemaphoreSlim(1, 1);
        /// <summary>
        /// 向内部字段中写入数据
        /// </summary>
        /// <param name="paths">路径信息</param>
        /// <param name="width">窗口宽度信息</param>
        /// <param name="height">窗口高度信息</param>
        /// <param name="coreflag">核心亲和性标志</param>
        /// <returns>返回异步方法实例</returns>
        public void WriteValues(string[] paths, ushort width, ushort height, bool coreflag)
        {
            TaskCount.Add(out long tidvalue);
            ValueLock.Wait();
            Paths = paths;
            Width = width;
            Height = height;
            CoreFlag = coreflag;
            ValueLock.Release();
            TaskCount.Release(tidvalue);
        }
        /// <summary>
        /// 向内部字段中写入暂停标志
        /// </summary>
        /// <param name="path">目标路径</param>
        /// <param name="add">是否为添加</param>
        /// <returns>返回是否执行了实际操作</returns>
        public bool WritePause(string path, bool add)
        {
            ValueLock.Wait();
            try
            {
                return add ? Pause.Add(path) : Pause.Remove(path);
            }
            finally { ValueLock.Release(); }
        }

        /// <summary>
        /// 主要工作线程
        /// </summary>
        /// <returns>返回异步方法实例</returns>
        private async Task Work()
        {
            TaskCount.Add(out long tidvalue);
            winctrl.IsWorking = true;
            if (winctrl.IsWorking) winctrl.output($"ProcMgr组件已启动 (#{Task.CurrentId})", false);
            HashSet<string> ProcFlag = new HashSet<string>(64);
            SemaphoreSlim ProcFlagLock = new SemaphoreSlim(1, 1);
            List<int> indexpool = new List<int>(64);
            while (winctrl.IsWorking)
            {
                await ValueLock.WaitAsync();
                string[] paths = Paths;
                ushort width = Width;
                ushort height = Height;
                bool coreflag = CoreFlag;
                ValueLock.Release();
                if (paths.Length != 0)
                {
                    indexpool.Clear();
                    await ProcFlagLock.WaitAsync();
                    for (int i = 0; i < paths.Length; i++)
                    {
                        await ValueLock.WaitAsync();
                        try
                        {
                            if (Pause.Contains(paths[i])) continue;
                        }
                        finally { ValueLock.Release(); }
                        if (!ProcFlag.Contains(paths[i])) indexpool.Add(i);
                    }
                    ProcFlagLock.Release();
                    if (indexpool.Count > 0)
                    {
                        HashSet<string> cache = new HashSet<string>(indexpool.Count, StringComparer.OrdinalIgnoreCase);
                        foreach (int index in indexpool)
                        {
                            cache.Add(Path.GetFileName(paths[index]));
                        }
                        ILookup<string, int> processcache = null;
                        List<PROCESSENTRY32> processes = new List<PROCESSENTRY32>(indexpool.Count * 2);
                        IntPtr hSnapshot = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);
                        if (hSnapshot == IntPtr.Zero || hSnapshot == new IntPtr(-1)/*INVALID_HANDLE_VALUE*/)
                        {
                            throw new Win32Exception(Marshal.GetLastWin32Error(), "生成进程快照失败。");
                        }
                        bool th32err = false;
                        try
                        {
                            PROCESSENTRY32 pe32 = new PROCESSENTRY32
                            {
                                dwSize = (uint)Marshal.SizeOf(typeof(PROCESSENTRY32))
                            };
                            if (Process32First(hSnapshot, ref pe32))
                            {
                                do
                                {
                                    if (cache.Contains(pe32.szExeFile))
                                    {
                                        processes.Add(pe32);
                                    }
                                } while (Process32Next(hSnapshot, ref pe32));
                                processcache = processes
                                    .ToLookup(
                                        keySelector: p => p.szExeFile,
                                        elementSelector: p => (int)p.th32ProcessID,
                                        comparer: StringComparer.OrdinalIgnoreCase
                                    );
                                processes.Clear();
                                cache.Clear();
                            }
                        }
                        catch (Exception) { th32err = true; }
                        finally
                        {
                            CloseHandle(hSnapshot);
                            if (th32err || processcache == null)
                            {
                                throw new Win32Exception(-1, "读取进程快照失败。");
                            }
                        }
                        if (processcache.Count > 0)
                        {
                            List<(int, string)> ProcessList = new List<(int, string)>(indexpool.Count);
                            foreach (int index in indexpool)
                            {
                                IEnumerable<int> pids = processcache[Path.GetFileName(paths[index])];
                                foreach (int pid in pids)
                                {
                                    try
                                    {
                                        // 由于游戏进程受到保护，process.MainModule.FileName会产生拒绝访问的win32异常
                                        IntPtr phandle = OpenProcess(
                                            ProcessAccessFlags.PROCESS_QUERY_INFORMATION | ProcessAccessFlags.PROCESS_VM_READ,
                                            false, pid);
                                        if (phandle != IntPtr.Zero)
                                        {
                                            StringBuilder path = new StringBuilder(32768);
                                            uint scount = (uint)path.Capacity;
                                            QueryFullProcessImageName(phandle, 0, path, ref scount);
                                            string p = path.ToString().Trim();
                                            path.Clear();
                                            if (p == paths[index])
                                            {
                                                await ProcFlagLock.WaitAsync();
                                                ProcFlag.Add(paths[index]);
                                                ProcFlagLock.Release();
                                                ProcessList.Add((pid, paths[index]));
                                                CloseHandle(phandle);
                                                break;
                                            }
                                            CloseHandle(phandle);
                                        }
                                    }
                                    catch (Exception)
                                    {
                                        continue;
                                    }
                                }
                            }
                            foreach ((int pid, string path) proc in ProcessList)
                            {
                                int pid = proc.pid;
                                string path = proc.path;
                                ushort w = width;
                                ushort h = height;
                                bool c = coreflag;
                                async void innertask()
                                {
                                    TaskCount.Add(out long _tidvalue);
                                    int timeout = 0;
                                    Process game = Process.GetProcessById(pid);
                                    IntPtr hwnd = IntPtr.Zero;
                                    do
                                    {
                                        if (game.HasExited || timeout == 300)
                                        {
                                            game.Dispose();
                                            await ProcFlagLock.WaitAsync();
                                            ProcFlag.Remove(path);
                                            ProcFlagLock.Release();
                                            TaskCount.Release(_tidvalue);
                                            return;
                                        }
                                        await Task.Delay(100);
                                        timeout++;
                                        try
                                        {
                                            hwnd = game.MainWindowHandle;
                                        }
                                        catch (Exception) { }
                                    } while (hwnd == IntPtr.Zero);
                                    await Task.Delay(1000); // 为了防止游戏窗口已初始化但未呈现 OR 呈现后极短时间内被销毁，以下为冗余代码
                                    if (game.HasExited || game.MainWindowHandle == IntPtr.Zero)
                                    {
                                        game.Dispose();
                                        await ProcFlagLock.WaitAsync();
                                        ProcFlag.Remove(path);
                                        ProcFlagLock.Release();
                                        TaskCount.Release(_tidvalue);
                                        return;
                                    }
                                    using (game)
                                    {
                                        if (!game.HasExited)
                                        {
                                            string pname = game.ProcessName;
                                            int id = game.Id;
                                            IntPtr window = game.MainWindowHandle;
                                            if (winctrl.IsWorking) winctrl.output($"进程已启动-{pname}.exe ({id}, 0x{window.ToInt32():x8})", false);
                                            // CPU
                                            if (c)
                                            {
                                                short corereport = Core.SetProcess(pid, Core.PMap, out bool skip);
                                                if (winctrl.IsWorking)
                                                {
                                                    if (skip)
                                                    {
                                                        winctrl.output($"已跳过CPU亲和性配置 ({id})", null);
                                                    }
                                                    else if (corereport == 0)
                                                    {
                                                        winctrl.output($"CPU亲和性配置成功 ({id})", false);
                                                    }
                                                    else if (corereport < 0 || corereport == short.MinValue)
                                                    {
                                                        winctrl.output("不支持配置CPU亲和性 (Idle)", null);
                                                    }
                                                    else
                                                    {
                                                        winctrl.output($"CPU亲和性配置失败 (0x{corereport:x4})", true);
                                                    }
                                                }
                                            }
                                            // DWM
                                            bool dwmresult = DWM.Set(
                                                window,
                                                false,
                                                true,
                                                Color.FromArgb(0xFF, 0x20, 0x20, 0x20).TO_COLORREF(),
                                                Color.FromArgb(0xFF, 0xF0, 0xF0, 0xF0).TO_COLORREF(),
                                                true);
                                            if (winctrl.IsWorking)
                                            {
                                                winctrl.output(dwmresult ? $"DWM配置成功 (0x{window.ToInt32():x8})" : $"DWM配置失败 (false)", !dwmresult);
                                            }
                                            // MAIN
                                            bool winctrlreport = false;
                                            try
                                            {
                                                winctrlreport = await winctrl.Add(window, pname, w, h);
                                            }
                                            catch (Win32Exception)
                                            {
                                                if (winctrl.IsWorking) winctrl.output($"目标-{pname}.exe窗口不受支持，已停止捕获 (?)", null);
                                                TaskCount.Release(_tidvalue);
                                                return;
                                            }
                                            catch (Exception)
                                            {
                                                if (!game.HasExited && game.MainWindowHandle != IntPtr.Zero)
                                                {
                                                    await winctrl.Remove(window); // 高风险操作，强制移除
                                                    if (winctrl.IsWorking) winctrl.output($"目标-{pname}.exe发生严重异常，已停止捕获 (?)", true);
                                                    TaskCount.Release(_tidvalue);
                                                    return;
                                                }
                                            }
                                            await Task.Factory.StartNew(() =>
                                            {
                                                TaskCount.Add(out long __tidvalue);
                                                DateTime starttime = game.StartTime;
                                                game.WaitForExit();
                                                TimeSpan runtime = DateTime.Now - starttime;
                                                bin.SetCount();
                                                bin.AddTime(ref bin.GameTime, runtime);
                                                TaskCount.Release(__tidvalue);
                                            }, TaskCreationOptions.LongRunning);
                                            if (winctrlreport) await winctrl.Remove(window);
                                            if (winctrl.IsWorking) winctrl.output($"进程已退出-{pname}.exe (Idle)", false);
                                        }
                                    }
                                    await ProcFlagLock.WaitAsync();
                                    ProcFlag.Remove(path);
                                    ProcFlagLock.Release();
                                    TaskCount.Release(_tidvalue);
                                    return;
                                }
                                innertask();
                            }
                        }
                    }
                }
                await Task.Delay(1000);
            }
            TaskCount.Release(tidvalue);
        }

        /// <summary>
        /// 存储主要工作线程的异步实例
        /// </summary>
        private Task WorkingTask = null;
        /// <summary>
        /// 启动主要工作线程
        /// </summary>
        public void Start()
        {
            if (!winctrl.IsWorking)
            {
                WorkingTask = Task.Run(Work);
                WorkingTask.ContinueWith(task =>
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        throw task.Exception;
                    });
                }, TaskContinuationOptions.OnlyOnFaulted);
            }
        }
        /// <summary>
        /// 停止主要工作线程 (仅发送终止信号，需自行调用 <see cref="WaitForExit"/> 进行同步等待)
        /// </summary>
        public void Stop()
        {
            if (winctrl.IsWorking)
            {
                winctrl.IsWorking = false;
                if (winctrl.IsWorking) winctrl.output($"已向ProcMgr组件发送终止信号 (async)", false);
            }
        }
        /// <summary>
        /// 等待终止
        /// </summary>
        public void WaitForExit(int timeout)
        {
            WorkingTask?.Wait(timeout);
        }
    }
}
