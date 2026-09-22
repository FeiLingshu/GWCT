using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using static GWCT.Datas.TaskCount;

namespace GWCT.Datas
{
    /// <summary>
    /// 提供 <see cref="System.Threading.Tasks.Task"/> 计数功能
    /// <para>
    /// 警告：需要执行 <see cref="Input"/> 进行初始化操作<br/>
    /// 警告：所有外部读取行为需调用 <see cref="Lock"/> 进行同步<br/><br/>
    /// 使用 <see cref="PropertyChanged"/> 事件向外通知
    /// </para>
    /// </summary>
    public static class TaskCount
    {
        /// <summary>
        /// 主工作线程
        /// </summary>
        private static readonly Dispatcher UI = Application.Current.Dispatcher;

        /// <summary>
        /// 提供数据信息读取的同步功能
        /// </summary>
        public static readonly SemaphoreSlim Lock = new SemaphoreSlim(1, 1);

        /// <summary>
        /// 用于在数据更改时向外部提供事件通知
        /// </summary>
        public static event EventHandler<TaskEventArgs> PropertyChanged;

        /// <summary>
        /// 内部字段，存储计数器数据
        /// </summary>
        private volatile static int Counter = 0;

        /// <summary>
        /// 内部字段，存储任务ID
        /// </summary>
        private static readonly List<long> IDs = new List<long>(Environment.ProcessorCount);

        /// <summary>
        /// 内部字段，存储可用任务数
        /// </summary>
        private volatile static int Available = Environment.ProcessorCount;

        /// <summary>
        /// 使计数器计数 <see langword="+1"/>
        /// </summary>
        /// <param name="value">[<see langword="out"/>] 返回当前缓存特征值</param>
        public static void Add(out long value)
        {
            // 初始化信息
            TaskData td = new TaskData();
            value = -1;
            // 开始执行
            Lock.Wait();
            // ++ 更新计数器
            Counter++;
            td.Count = Counter;
            // ++ 更新ID表
            int id = Task.CurrentId ?? -1;
            if (id != -1)
            {
                HashSet<int> mask = null;
                bool maskflag = false;
                int index = 0;
                for (int i = 0; i < IDs.Count; i++)
                {
                    long v = IDs[i];
                    if ((int)v == id)
                    {
                        if (!maskflag)
                        {
                            maskflag = true;
                            mask = new HashSet<int>();
                        }
                        mask.Add((int)(v >> 32));
                    }
                }
                if (maskflag)
                {
                    while (mask.Contains(index)) { index++; }
                }
                value = ((long)index << 32) | (uint)id;
                IDs.Add(value);
            }
            td.ID = IDs.Count == 0 ? 0 : (int)IDs.Last();
            Lock.Release();
            // 发送通知
            _ = UI.InvokeAsync(() => PropertyChanged?.Invoke(null, new TaskEventArgs(false, td)));
        }

        /// <summary>
        /// 使计数器计数 <see langword="-1"/>
        /// </summary>
        /// <param name="value">目标缓存特征值</param>
        public static void Release(long value)
        {
            // 初始化信息
            TaskData td = new TaskData();
            // 开始执行
            Lock.Wait();
            // ++ 更新计数器
            Counter--;
            td.Count = Counter;
            // ++ 更新ID表
            if (value != -1)
            {
                IDs.Remove(value);
            }
            td.ID = IDs.Count == 0 ? 0 : (int)IDs.Last();
            Lock.Release();
            // 发送通知
            _ = UI.InvokeAsync(() => PropertyChanged?.Invoke(null, new TaskEventArgs(false, td)));
        }

        /// <summary>
        /// 内部方法，获取颜色值
        /// </summary>
        private static SolidColorBrush GetColor(int value, int basevalue) => value > (basevalue / 2) ? Green : (value > 2 ? Yellow : Red);

        /// <summary>
        /// 控制内部循环是否运行
        /// </summary>
        public static volatile bool Watcher = false;

        /// <summary>
        /// 读取线程池信息
        /// </summary>
        /// <param name="exsize">线程池可扩展大小</param>
        public static async Task StartThreadPoolWatch(int exsize)
        {
            // 记录自身
            Add(out long value);
            // 初始化默认数据
            await Lock.WaitAsync();
            TaskData td = new TaskData
            {
                Count = Counter,
                ID = IDs.Count == 0 ? 0 : (int)IDs.Last(),
                Color = Green,
                Free = Available
            };
            Lock.Release();
            _ = UI.InvokeAsync(() => PropertyChanged?.Invoke(null, new TaskEventArgs(false, td)));
            _ = UI.InvokeAsync(() => PropertyChanged?.Invoke(null, new TaskEventArgs(true, td)));
            // 启动循环
            Watcher = true;
            while (Watcher)
            {
                ThreadPool.GetAvailableThreads(out int available, out _);
                int free = available - exsize;
                free = (free < 0) ? 0 : free;
                await Lock.WaitAsync();
                if (Available != free)
                {
                    Available = free;
                    td.Color = GetColor(Available, Max);
                    td.Free = Available;
                    Lock.Release();
                    _ = UI.InvokeAsync(() => PropertyChanged?.Invoke(null, new TaskEventArgs(true, td)));
                }
                else
                {
                    Lock.Release();
                }
                await Task.Delay(1000);
            }
            // 移除自身
            Release(value);
        }

        /// <summary>
        /// 内部字段，存储最大安全值
        /// </summary>
        private static readonly int Max = Environment.ProcessorCount;

        /// <summary>
        /// 静态字段，存储绿色画刷
        /// </summary>
        private static SolidColorBrush Green;
        /// <summary>
        /// 静态字段，存储黄色画刷
        /// </summary>
        private static SolidColorBrush Yellow;
        /// <summary>
        /// 静态字段，存储红色画刷
        /// </summary>
        private static SolidColorBrush Red;

        /// <summary>
        /// 加载初始值
        /// </summary>
        /// <param name="green">绿色画刷</param>
        /// <param name="yellow">黄色画刷</param>
        /// <param name="red">红色画刷</param>
        public static void Input(SolidColorBrush green, SolidColorBrush yellow, SolidColorBrush red)
        {
            Green = green;
            Yellow = yellow;
            Red = red;
        }

        /// <summary>
        /// 存储组件缓存数据的结构体
        /// </summary>
        public struct TaskData
        {
            /// <summary>
            /// 计数
            /// </summary>
            public int Count;
            /// <summary>
            /// 可用
            /// </summary>
            public int Free;
            /// <summary>
            /// 颜色
            /// </summary>
            public SolidColorBrush Color;
            /// <summary>
            /// ID
            /// </summary>
            public int ID;
        }
    }

    /// <summary>
    /// 用于事件通知的数据类型
    /// <para>
    /// 继承自 <see cref="EventArgs"/>
    /// </para>
    /// </summary>
    public class TaskEventArgs : EventArgs
    {
        /// <summary>
        /// 事件类型 (<see langword="false"/>表示基础计数类型，<see langword="true"/>表示可用数量类型)
        /// </summary>
        public bool Type { get; }
        /// <summary>
        /// 包含事件数据的结构体 (类型：<see cref="TaskData"/>)
        /// </summary>
        public TaskData Data { get; }

        /// <summary>
        /// 初始化事件数据
        /// </summary>
        /// <param name="type">事件类型</param>
        /// <param name="data">事件数据</param>
        public TaskEventArgs(bool type, TaskData data)
        {
            Type = type;
            Data = data;
        }
    }
}
