using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GWCT.Datas
{
    /// <summary>
    /// 提供向 <see langword="UI"/> 输出日志功能的公开类
    /// <para>
    /// 实现 <see cref="INotifyPropertyChanged"/> 接口
    /// </para>
    /// </summary>
    public class Logs : INotifyPropertyChanged
    {
        /// <summary>
        /// 日志快照缓冲区
        /// <para>
        /// 实现自私有类 <see cref="CircularBuffer"/>
        /// </para>
        /// </summary>
        private readonly CircularBuffer Buffer;
        /// <summary>
        /// 最大缓冲区大小
        /// </summary>
        private readonly int MaxCapacity;

        /// <summary>
        /// 初始化 <see cref="Logs"/> 类实例
        /// </summary>
        /// <param name="maxCapacity">最大可缓存日志数量</param>
        public Logs(int maxCapacity = 64)
        {
            MaxCapacity = maxCapacity;
            Buffer = new CircularBuffer(maxCapacity);
        }

        /// <summary>
        /// 向缓冲区中添加日志条目
        /// </summary>
        /// <param name="type">日志类别</param>
        /// <param name="message">日志信息</param>
        /// <param name="error">错误标志 (<see langword="true"/>为错误，<see langword="false"/>为正常，<see langword="null"/>为警告)</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(string type, string message, bool? error)
        {
            var logEntry = new LogEntry
            {
                Type = type,
                Message = message,
                Warning = error.HasValue ? string.Empty : "<警告> ",
                Error = error.HasValue && error.Value ? "<错误> " : string.Empty
            };
            Buffer.Add(logEntry);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LogEntries)));
        }

        /// <summary>
        /// 从缓冲区移除指定索引位置的日志条目
        /// <paara>
        /// 由于已通过私有类 <see cref="CircularBuffer"/> 实现环形数组，可实现自覆盖，该方法无调用需求
        /// </paara>
        /// </summary>
        /// <param name="index">目标日志条目的索引</param>
        [Obsolete("该方法未实现", true)]
        public void Remove(int index) { }

        /// <summary>
        /// 清空缓冲区
        /// </summary>
        public void Clear() => Buffer.Clear();

        /// <summary>
        /// 用于向外暴露缓冲区的 <see cref="IEnumerable{LogEntry}"/> 通用接口
        /// </summary>
        public IEnumerable<LogEntry> LogEntries => (IEnumerable<LogEntry>)Buffer;

        /// <summary>
        /// 用于在数据更改时向外部提供事件通知
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
    }

    /// <summary>
    /// 承载日志数据的公开类
    /// </summary>
    public class LogEntry
    {
        /// <summary>
        /// 日志类别
        /// </summary>
        public string Type { get; set; }
        /// <summary>
        /// 日志数据
        /// </summary>
        public string Message { get; set; }
        /// <summary>
        /// 错误标志
        /// </summary>
        public string Warning { get; set; }
        /// <summary>
        /// 警告标志
        /// </summary>
        public string Error { get; set; }
    }
}
