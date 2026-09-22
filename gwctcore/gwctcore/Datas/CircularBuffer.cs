using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Runtime.CompilerServices;

namespace GWCT.Datas
{
    /// <summary>
    /// 实现自定义环形数组的公开类
    /// <para>
    /// 实现 <see cref="IEnumerable"/> 接口<br/>
    /// 实现 <see cref="IEnumerable{LogEntry}"/> 接口<br/>
    /// 实现 <see cref="INotifyCollectionChanged"/> 接口
    /// </para>
    /// </summary>
    public class CircularBuffer : IEnumerable, IEnumerable<LogEntry>, INotifyCollectionChanged
    {
        /// <summary>
        /// 数组缓冲区
        /// </summary>
        private readonly LogEntry[] _buffer;
        /// <summary>
        /// 写入位置(索引)
        /// </summary>
        private int _writeIndex;
        /// <summary>
        /// 缓冲区中有效元素数量
        /// </summary>
        private int _count;
        /// <summary>
        /// 缓冲区最大可承载元素数量
        /// </summary>
        private readonly int _capacity;

        /// <summary>
        /// 初始化 <see cref="CircularBuffer"/> 类实例
        /// <para>
        /// 实现环形数组的可交互类实例
        /// </para>
        /// </summary>
        /// <param name="capacity">环形数组最大容量</param>
        public CircularBuffer(int capacity)
        {
            _capacity = capacity;
            _buffer = new LogEntry[capacity];
            _writeIndex = 0;
            _count = 0;
            InitializeCCS();
        }

        /// <summary>
        /// 向环形数组追加数据
        /// </summary>
        /// <param name="item">目标数据</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(LogEntry item)
        {
            CCS[_count >= _capacity ? 1 : 0](ref _count);
            _buffer[_writeIndex] = item;
            _writeIndex = (_writeIndex + 1) % _capacity;
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item));
        }

        /// <summary>
        /// <see cref="CCS"/> 使用的委托类型
        /// </summary>
        /// <param name="count">[<see langword="ref"/>] 数据2</param>
        public delegate void CountCheck(ref int count);

        /// <summary>
        /// 保存获取数据的委托实例
        /// <para>
        /// [<see langword="警告"/>] 使用前务必调用 <see cref="InitializeCCS"/> 进行初始化
        /// </para>
        /// </summary>
        private readonly CountCheck[] CCS = new CountCheck[2];

        /// <summary>
        /// 初始化 <see cref="CCS"/> 数组
        /// </summary>
        private void InitializeCCS()
        {
            CCS[0] = (ref int count) =>
            {
                count++;
            };
            CCS[1] = (ref int count) =>
            {
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, _buffer[_writeIndex], 0));
            };
        }

        /// <summary>
        /// 移除环形数组内的所有数据
        /// </summary>
        public void Clear()
        {
            _count = 0;
            _writeIndex = 0;
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        /// <summary>
        /// 向外暴露重映射后的数组实例(从索引 <see langword="0"/> 开始按插入顺序排布的数组)
        /// </summary>
        /// <param name="index">目标索引位置</param>
        /// <returns>返回目标索引位置处的重映射数据</returns>
        public LogEntry this[int index] => _buffer[(_writeIndex - _count + index + _capacity) % _capacity];

        /// <summary>
        /// 获取数组中有效数据的数量
        /// </summary>
        public int Count => _count;

        /// <summary>
        /// 实现 <see cref="IEnumerator{T}"/> 接口
        /// </summary>
        /// <returns>返回 <see cref="IEnumerator{T}"/> 接口类型数据</returns>
        public IEnumerator<LogEntry> GetEnumerator()
        {
            for (int i = 0; i < _count; i++)
                yield return this[i];
        }

        /// <summary>
        /// 实现 <see cref="IEnumerator"/> 接口
        /// </summary>
        /// <returns>返回 <see cref="IEnumerator"/> 接口类型数据</returns>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// 实现 <see cref="INotifyCollectionChanged"/> 接口
        /// <para>
        /// 向 <see langword="UI"/> 层发送数据更改通知
        /// </para>
        /// </summary>
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        /// <summary>
        /// 触发数据更改通知消息
        /// </summary>
        /// <param name="e">事件数据</param>
        protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            CollectionChanged?.Invoke(this, e);
        }
    }
}
