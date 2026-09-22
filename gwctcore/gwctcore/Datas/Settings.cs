using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace GWCT.Datas
{
    /// <summary>
    /// 提供向 <see langword="UI"/> 输出配置数据功能的公开类
    /// <para>
    /// 实现 <see cref="INotifyPropertyChanged"/> 接口
    /// </para>
    /// </summary>
    public class Settings : INotifyPropertyChanged
    {
        /// <summary>
        /// 配置数据缓冲区
        /// </summary>
        private readonly ObservableCollection<SettingEntry> Buffer;

        /// <summary>
        /// 初始化 <see cref="Settings"/> 类实例
        /// </summary>
        /// <param name="maxCapacity">最大可缓存数据数量(可自动扩容)</param>
        public Settings(int maxCapacity = 64)
        {
            Buffer = new ObservableCollection<SettingEntry>(new List<SettingEntry>(maxCapacity));
        }

        /// <summary>
        /// 向缓冲区中添加数据条目
        /// </summary>
        /// <param name="p">数据内容</param>
        /// <param name="plus">是否进行额外查询操作</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(string p, bool plus = false)
        {
            string plusstd = null;
            if (plus)
            {
                StringBuilder sbuilder = new StringBuilder(260);
                FileVersionInfo info = FileVersionInfo.GetVersionInfo(p);
                bool pname =
                    !string.IsNullOrEmpty(info.ProductName) &&
                    info.ProductName != Path.GetFileNameWithoutExtension(p) &&
                    info.ProductName != info.OriginalFilename;
                bool description =
                    !pname && !string.IsNullOrEmpty(info.FileDescription);
                bool originname =
                    !pname && !description;
                bool pversion = 
                    !string.IsNullOrEmpty(info.ProductVersion);
                bool fversion =
                    !pversion && !string.IsNullOrEmpty(info.FileVersion);
                if (pname) sbuilder.Append(info.ProductName);
                if (description) sbuilder.Append(info.FileDescription);
                if (originname) sbuilder.Append(Path.GetFileName(p));
                if (pversion)
                {
                    sbuilder.Append(" (");
                    sbuilder.Append(info.ProductVersion);
                    sbuilder.Append(")");
                }
                if (fversion)
                {
                    sbuilder.Append(" (");
                    sbuilder.Append(info.FileVersion);
                    sbuilder.Append(")");
                }
                plusstd = sbuilder.ToString();
            }
            var Entry = new SettingEntry
            {
                P = p,
                N = plus ? plusstd : Path.GetFileName(p)
            };
            Buffer.Add(Entry);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SettingEntries)));
        }

        /// <summary>
        /// 从缓冲区移除指定索引位置的数据条目
        /// </summary>
        /// <param name="index">目标数据条目的索引</param>
        public void Remove(int index)
        {
            Buffer.RemoveAt(index);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SettingEntries)));
        }

        /// <summary>
        /// 从缓冲区中读取数据
        /// </summary>
        /// <param name="index">目标数据条目的索引</param>
        public string Get(int index)
        {
            return Buffer[index].P;
        }

        /// <summary>
        /// 清空缓冲区
        /// </summary>
        public void Clear() => Buffer.Clear();

        /// <summary>
        /// 获取缓冲区是否为空
        /// </summary>
        /// <returns>返回缓冲区是否为空</returns>
        public bool IsEmpty() => Buffer.Count == 0;

        /// <summary>
        /// 用于向外暴露缓冲区的 <see cref="IEnumerable{SettingEntry}"/> 通用接口
        /// </summary>
        public IEnumerable<SettingEntry> SettingEntries => (IEnumerable<SettingEntry>)Buffer;

        /// <summary>
        /// 用于在数据更改时向外部提供事件通知
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
    }

    /// <summary>
    /// 承载数据信息的公开类
    /// </summary>
    public class SettingEntry
    {
        /// <summary>
        /// 路径信息
        /// </summary>
        public string P { get; set; }
        /// <summary>
        /// 路径信息(短名称)
        /// </summary>
        public string N { get; set; }
    }
}
