using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace GWCT.Cfg
{
    /// <summary>
    /// 提供配置文件读写功能
    /// </summary>
    public class Bin
    {
        /// <summary>
        /// 全局 <see href="UTF8"/> 编码实例
        /// </summary>
        public static readonly Encoding UTF8 = new UTF8Encoding(false);

        /// <summary>
        /// 保存配置参数的结构体
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct DATA
        {
            /// <summary>
            /// 配置参数1
            /// </summary>
            public ushort SET_1;
            /// <summary>
            /// 配置参数2
            /// </summary>
            public ushort SET_2;
            /// <summary>
            /// 配置参数3
            /// </summary>
            public bool SET_3;
            /// <summary>
            /// 路径数据的字节长度
            /// </summary>
            public int PLENGTH;
            /// <summary>
            /// 路径数据的UTF8编码字节
            /// </summary>
            public byte[] PATH;
            /// <summary>
            /// 路径数据的字节长度(启动器附加)
            /// </summary>
            public int _PLENGTH;
            /// <summary>
            /// 路径数据的UTF8编码字节(启动器附加)
            /// </summary>
            public byte[] _PATH;

            /// <summary>
            /// 初始化配置实例
            /// </summary>
            /// <param name="SET">配置参数</param>
            /// <param name="FLAG">配置选项</param>
            /// <param name="PATH">程序路径</param>
            /// <param name="_PATH">程序路径(启动器附加)</param>
            public DATA((ushort, ushort) SET, bool FLAG, string[] PATH, string[] _PATH)
            {
                this.SET_1 = SET.Item1;
                this.SET_2 = SET.Item2;
                this.SET_3 = FLAG;
                this.PATH = UTF8.GetBytes(string.Join("\n", PATH));
                this.PLENGTH = this.PATH.Length;
                this._PATH = UTF8.GetBytes(string.Join("\n", _PATH));
                this._PLENGTH = this._PATH.Length;
            }

            /// <summary>
            /// 编码配置数据
            /// </summary>
            /// <returns>返回配置数据的字节码</returns>
            public byte[] Serialize()
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    using (BinaryWriter bw = new BinaryWriter(ms, UTF8))
                    {
                        bw.Write(SET_1);
                        bw.Write(SET_2);
                        bw.Write(SET_3);
                        bw.Write(PLENGTH);
                        bw.Write(PATH);
                        bw.Write(_PLENGTH);
                        bw.Write(_PATH);
                        return ms.ToArray();
                    }
                }
            }

            /// <summary>
            /// 解码配置数据
            /// </summary>
            /// <param name="source">配置数据的原始结构</param>
            /// <param name="data">配置数据的字节码</param>
            public static void Deserialize(ref DATA source, byte[] data)
            {
                using (MemoryStream ms = new MemoryStream(data))
                {
                    using (BinaryReader br = new BinaryReader(ms, UTF8))
                    {
                        source.SET_1 = br.ReadUInt16();
                        source.SET_2 = br.ReadUInt16();
                        source.SET_3 = br.ReadBoolean();
                        source.PLENGTH = br.ReadInt32();
                        source.PATH = br.ReadBytes(source.PLENGTH);
                        source._PLENGTH = br.ReadInt32();
                        source._PATH = br.ReadBytes(source._PLENGTH);
                    }
                }
            }

            /// <summary>
            /// 初始化默认配置信息
            /// </summary>
            /// <returns>返回默认配置信息实例</returns>
            public static DATA Empty()
            {
                return new DATA((1920, 1080), false, new string[0], new string[0]);
            }

            /// <summary>
            /// 重写==运算符
            /// </summary>
            /// <param name="left">结构体实例1</param>
            /// <param name="right">结构体实例2</param>
            /// <returns>返回两个结构体是否相等</returns>
            public static bool operator ==(DATA left, DATA right)
            {
                return left.SET_1 == right.SET_1
                    && left.SET_2 == right.SET_2
                    && left.SET_3 == right.SET_3
                    && left.PLENGTH == right.PLENGTH
                    && left.PATH.SequenceEqual(right.PATH)
                    && left._PLENGTH == right._PLENGTH
                    && left._PATH.SequenceEqual(right._PATH);
            }

            /// <summary>
            /// 重写!=运算符
            /// </summary>
            /// <param name="left">结构体实例1</param>
            /// <param name="right">结构体实例2</param>
            /// <returns>返回两个结构体是否不等</returns>
            public static bool operator !=(DATA left, DATA right) => !(left == right);

            /// <summary>
            /// 重写Equals对象方法
            /// </summary>
            /// <param name="obj">比较源</param>
            /// <returns>返回两个对象是否相同</returns>
            public override bool Equals(object obj) => obj is DATA other && this == other;

            /// <summary>
            /// 重写 <see cref="GetHashCode"/> 方法
            /// </summary>
            /// <returns>返回对象（在内置编码器编码后）的字节数组的哈希值</returns>
            public override int GetHashCode() => Serialize().GetHashCode();
        }

        /// <summary>
        /// 存储全局文件流
        /// </summary>
        private readonly FileStream config = null;
        /// <summary>
        /// 存储配置文件初始状态
        /// </summary>
        private readonly bool configexist = false;

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public Bin()
        {
            using (Process CurrentProcess = Process.GetCurrentProcess())
            {
                FILE =
                    $"{Path.GetDirectoryName(CurrentProcess.MainModule.FileName)}\\" +
                    $"{Path.GetFileNameWithoutExtension(CurrentProcess.MainModule.FileName)}.bin";
                configexist = File.Exists(FILE);
                config = File.Open(FILE, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
            }
            Check();
        }

        /// <summary>
        /// 用于存储配置文件路径的全局字段
        /// </summary>
        public string FILE = string.Empty;

        /// <summary>
        /// 用于存储配置信息实例的全局字段
        /// </summary>
        public DATA BINDATA = DATA.Empty();

        /// <summary>
        /// 用于缓存路径信息的全局字段
        /// </summary>
        public HashSet<string> PATHS = new HashSet<string>(64, StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 用于缓存路径信息的全局字段(启动器附加)
        /// </summary>
        public HashSet<string> _PATHS = new HashSet<string>(64, StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 检查配置文件合法性
        /// </summary>
        /// <returns>返回检查结果</returns>
        internal bool Check()
        {
            if (configexist)
            {
                DATA data;
                HashSet<string> paths;
                HashSet<string> _paths;
                try
                {
                    data = Read();
                    paths = new HashSet<string>(UTF8.GetString(data.PATH).Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries));
                    _paths = new HashSet<string>(UTF8.GetString(data._PATH).Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries));
                }
                catch (Exception)
                {
                    BINDATA = DATA.Empty();
                    PATHS.Clear();
                    _PATHS.Clear();
                    Writestat(TimeSpan.Zero);
                    Write();
                    return false;
                }
                foreach (string path in paths)
                {
                    if (!File.Exists(path))
                    {
                        paths.Remove(path);
                    }
                }
                foreach (string _path in _paths)
                {
                    if (!File.Exists(_path))
                    {
                        _paths.Remove(_path);
                    }
                }
                BINDATA = data;
                PATHS = paths;
                _PATHS = _paths;
                return true;
            }
            else
            {
                BINDATA = DATA.Empty();
                PATHS.Clear();
                _PATHS.Clear();
                Writestat(TimeSpan.Zero);
                Write();
                return false;
            }
        }

        /// <summary>
        /// 将相关信息写入配置文件
        /// </summary>
        /// <param name="checkfile">是否检查文件有效性</param>
        /// <returns>返回写入是否成功</returns>
        public bool SetData(bool checkfile = false)
        {
            if (checkfile)
            {
                foreach (string path in PATHS)
                {
                    if (!File.Exists(path))
                    {
                        PATHS.Remove(path);
                    }
                }
                foreach (string _path in _PATHS)
                {
                    if (!File.Exists(_path))
                    {
                        _PATHS.Remove(_path);
                    }
                }
            }
            BINDATA = new DATA((BINDATA.SET_1, BINDATA.SET_2), BINDATA.SET_3, PATHS.ToArray(), _PATHS.ToArray());
            Write();
            return true;
        }
        /// <summary>
        /// 递增计数器
        /// </summary>
        internal void SetCount()
        {
            Interlocked.Increment(ref RunCount);
        }

        /// <summary>
        /// 数据读写同步组件
        /// </summary>
        private readonly SemaphoreSlim ValueLock = new SemaphoreSlim(1, 1);
        /// <summary>
        /// 对时间数据进行自增
        /// </summary>
        /// <param name="source">数据源</param>
        /// <param name="add">自增数量</param>
        internal void AddTime(ref TimeSpan source, TimeSpan add)
        {
            ValueLock.Wait();
            source += add;
            ValueLock.Release();
        }
        /// <summary>
        /// 获取时间数据
        /// </summary>
        /// <returns>返回全部时间数据信息</returns>
        private (TimeSpan Run, TimeSpan Game) GetTime()
        {
            try
            {
                ValueLock.Wait();
                return (RunTime, GameTime);
            }
            finally
            {
                ValueLock.Release();
            }
        }

        /// <summary>
        /// 运行计数器
        /// </summary>
        public long RunCount = 0;
        /// <summary>
        /// 运行时间戳
        /// </summary>
        public TimeSpan RunTime = TimeSpan.Zero;
        /// <summary>
        /// 游戏时长时间戳
        /// </summary>
        public TimeSpan GameTime = TimeSpan.Zero;
        /// <summary>
        /// 获取统计数据
        /// </summary>
        /// <param name="C">运行次数统计</param>
        /// <param name="Now">当前程序运行时间</param>
        /// <param name="RunTime">运行时长统计</param>
        /// <param name="GameTime">游戏时长统计</param>
        public void GetStat(out uint C, TimeSpan Now, out TimeSpan RunTime, out TimeSpan GameTime)
        {
            C = (uint)Interlocked.Read(ref RunCount);
            var (Run, Game) = GetTime();
            Now += Run;
            RunTime = Now;
            GameTime = Game;
        }

        /// <summary>
        /// 用于从文件读取配置信息的内部方法
        /// </summary>
        /// <returns>返回配置信息实例</returns>
        private DATA Read()
        {
            byte[] statdata = new byte[14];
            byte[] cfgdata = new byte[config.Length - 14];
            config.Seek(0, SeekOrigin.Begin);
            _ = config.Read(statdata, 0, statdata.Length);
            _ = config.Read(cfgdata, 0, cfgdata.Length);
            Readstat(statdata);
            DATA.Deserialize(ref BINDATA, cfgdata);
            return BINDATA;
        }
        /// <summary>
        /// 用于从文件读取统计信息的内部方法
        /// </summary>
        /// <param name="bytes">目标数据原始字节数组</param>
        private void Readstat(byte[] bytes)
        {
            using (MemoryStream ms = new MemoryStream(bytes))
            {
                using (BinaryReader br = new BinaryReader(ms, UTF8))
                {
                    uint D1 = br.ReadUInt32();
                    ushort D2 = br.ReadUInt16();
                    byte D3 = br.ReadByte();
                    byte D4 = br.ReadByte();
                    byte D5 = br.ReadByte();
                    ushort D6 = br.ReadUInt16();
                    byte D7 = br.ReadByte();
                    byte D8 = br.ReadByte();
                    byte D9 = br.ReadByte();
                    RunCount = D1;
                    AddTime(ref RunTime, new TimeSpan(D2, D3, D4, D5));
                    AddTime(ref GameTime, new TimeSpan(D6, D7, D8, D9));
                }
            }
        }

        /// <summary>
        /// 用于向文件写入配置信息的内部方法
        /// </summary>
        private void Write()
        {
            byte[] bytes = BINDATA.Serialize();
            config.SetLength(14);
            config.Seek(0, SeekOrigin.End);
            config.Write(bytes, 0, bytes.Length);
            config.Flush();
        }
        /// <summary>
        /// 向配置文件写入统计信息
        /// </summary>
        /// <param name="runtime">当前程序运行时间</param>
        public void Writestat(TimeSpan runtime)
        {
            byte[] bytes;
            var (Run, Game) = GetTime();
            TimeSpan NewTime = Run + runtime;
            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter bw = new BinaryWriter(ms, UTF8))
                {
                    bw.Write((uint)Interlocked.Read(ref RunCount));
                    bw.Write((ushort)NewTime.Days);
                    bw.Write((byte)NewTime.Hours);
                    bw.Write((byte)NewTime.Minutes);
                    bw.Write((byte)NewTime.Seconds);
                    bw.Write((ushort)Game.Days);
                    bw.Write((byte)Game.Hours);
                    bw.Write((byte)Game.Minutes);
                    bw.Write((byte)Game.Seconds);
                    bytes = ms.ToArray();
                }
            }
            config.Seek(0, SeekOrigin.Begin);
            config.Write(bytes, 0, 14);
            config.Flush();
        }
    }
}
