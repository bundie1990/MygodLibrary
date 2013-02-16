namespace Mygod.IO
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Media;

    static class SafeNativeMethods
    {
        [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool WritePrivateProfileString(string section, string key, string val, string filePath);

        [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern uint GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, 
                                                            uint size, string filePath);
    }
    /// <summary>
    /// 提供Ini文件的操作类。
    /// </summary>
    public class IniFile
    {
        #region 参数

        private readonly string iniFilePath = string.Empty, executableDir;
        private readonly uint returnStringLong;

        #endregion

        #region 自定义方法

        /// <summary>
        /// 创建新的IniFile。
        /// </summary>
        /// <param name="sFilePath">Ini文件路径。</param>
        /// <param name="iStringLong">文本长度，如果超过会被截断。</param>
        public IniFile(string sFilePath, uint iStringLong = 1024)
        {
            iniFilePath = sFilePath;
            returnStringLong = iStringLong;
            executableDir = AppDomain.CurrentDomain.BaseDirectory;
        }

        /// <summary>
        /// 读取Ini数据。
        /// </summary>
        /// <param name="section">Ini节名。</param>
        /// <param name="key">Ini键名。</param>
        /// <param name="noText">当指定键不存在时返回值。</param>
        /// <returns></returns>
        public string ReadIniData(string section, string key, string noText = null)
        {
            if (File.Exists(GetFilePath()))
            {
                var temp = new StringBuilder((int)returnStringLong);
                SafeNativeMethods.GetPrivateProfileString(section, key, noText, temp, returnStringLong, GetFilePath());
                return Convert.ToString(temp);
            }
            return noText;
        }

        /// <summary>
        /// 写出Ini数据。
        /// </summary>
        /// <param name="section">Ini节名。</param>
        /// <param name="key">Ini键名。</param>
        /// <param name="value">要写入的值。</param>
        public void WriteIniData(string section, string key, string value)
        {
            Task.Factory.StartNew(() => SafeNativeMethods.WritePrivateProfileString(section, key, value, GetFilePath()));
        }

        private string GetFilePath()
        {
            return iniFilePath.Substring(1, 1) == ":" ? iniFilePath : 
                (iniFilePath.Substring(0, 1) == @"\" ? executableDir.Substring(0, 2) + iniFilePath : executableDir + @"\" + iniFilePath);
        }

        #endregion
    }

    /// <summary>
    /// 提供Ini节的操作类。
    /// </summary>
    public class IniSection
    {
        /// <summary>
        /// 创建新的Ini节。
        /// </summary>
        /// <param name="iniFile">属于的Ini文件。</param>
        /// <param name="sectionName">节名。</param>
        public IniSection(IniFile iniFile, string sectionName)
        {
            IniFile = iniFile;
            Name = sectionName;
        }

        /// <summary>
        /// 获取属于的Ini文件。
        /// </summary>
        public IniFile IniFile { get; private set; }
        /// <summary>
        /// 获取当前节节名。
        /// </summary>
        public string Name { get; private set; }
    }

    /// <summary>
    /// 包含数据被修改的事件数据。
    /// </summary>
    /// <typeparam name="T">被修改的数据的类型，需要是一个类。</typeparam>
    public sealed class IniDataChangedEventArgs<T> : EventArgs
    {
        /// <summary>
        /// 创建一个 ClassDataChangedEventArgs 实例。
        /// </summary>
        /// <param name="oldValue">修改前的值。</param>
        /// <param name="newValue">修改后的值。</param>
        public IniDataChangedEventArgs(T oldValue, T newValue)
        {
            OldValue = oldValue;
            NewValue = newValue;
        }
        /// <summary>
        /// 获取修改前的值。
        /// </summary>
        public T OldValue { get; private set; }
        /// <summary>
        /// 获取修改后的值。
        /// </summary>
        public T NewValue { get; private set; }
    }

    /// <summary>
    /// 定义特定的方法获得或设置值。
    /// </summary>
    /// <typeparam name="T">要获得的类型。</typeparam>
    public interface IIniData<T>
    {
        /// <summary>
        /// 获得ini文件中的值。
        /// </summary>
        /// <returns>返回获得的值。</returns>
        T Get();
        /// <summary>
        /// 设置ini文件中的值。
        /// </summary>
        /// <param name="value">要设置的值。</param>
        void Set(T value);

        event EventHandler<IniDataChangedEventArgs<T>> DataChanged;
    }
    /// <summary>
    /// 定义带参数的特定方法获得或设置值。
    /// </summary>
    /// <typeparam name="TKey">要获得的类型。</typeparam>
    /// <typeparam name="TResult">要获得的类型。</typeparam>
    public interface IIniDataWithParam<in TKey, TResult>
    {
        /// <summary>
        /// 获得ini文件中的值。
        /// </summary>
        /// <param name="key">参数。</param>
        /// <returns>返回获得的值。</returns>
        TResult Get(TKey key);
        /// <summary>
        /// 设置ini文件中的值。
        /// </summary>
        /// <param name="key">参数。</param>
        /// <param name="value">要设置的值。</param>
        void Set(TKey key, TResult value);

        IIniData<TResult> GetData(TKey key);
    }

    /// <summary>
    /// 从ini参数中获得字符串值。
    /// </summary>
    public class StringData : IIniData<string>
    {
        /// <summary>
        /// 构建一个 StringData 实例。
        /// </summary>
        /// <param name="inisection"></param>
        /// <param name="key"></param>
        /// <param name="defaultvalue"></param>
        public StringData(IniSection inisection, string key, string defaultvalue = null)
        {
            section = inisection;
            dataKey = key;
            defaultValue = defaultvalue;
            Get();
        }

        private readonly IniSection section;
        private readonly string defaultValue;
        private string dataKey;

        /// <summary>
        /// Key resetter.
        /// </summary>
        public string DataKey { set { dataKey = value; requested = false; } }

        private string requestedValue;
        private bool requested;

        public string Get()
        {
            if (!requested)
            {
                requested = true;
                requestedValue = section.IniFile.ReadIniData(section.Name, dataKey, defaultValue);
            }
            return requestedValue;
        }
        public void Set(string value)
        {
            if (requestedValue == value) return;
            if (DataChanged != null) DataChanged(this, new IniDataChangedEventArgs<string>(requestedValue, value));
            requested = true;
            requestedValue = value;
            section.IniFile.WriteIniData(section.Name, dataKey, value);
        }

        public event EventHandler<IniDataChangedEventArgs<string>> DataChanged;
    }
    public class Int32Data : StringData, IIniData<int>
    {
        public Int32Data(IniSection section, string key, int value) : base(section, key, value.ToString())
        {
            Get();
        }

        private int requestedValue;

        public new int Get()
        {
            return requestedValue = int.Parse(base.Get());
        }
        public void Set(int value)
        {
            if (requestedValue == value) return;
            if (DataChanged != null) DataChanged(this, new IniDataChangedEventArgs<int>(requestedValue, value));
            Set(value.ToString());
            requestedValue = value;
        }

        public new event EventHandler<IniDataChangedEventArgs<int>> DataChanged;
    }
    public class DoubleData : StringData, IIniData<double>
    {
        public DoubleData(IniSection section, string key, double value) : base(section, key, value.ToString())
        {
            Get();
        }

        private double requestedValue;

        public new double Get()
        {
            return requestedValue = double.Parse(base.Get());
        }
        public void Set(double value)
        {
            // ReSharper disable CompareOfFloatsByEqualityOperator
            if (requestedValue == value) return;
            // ReSharper restore CompareOfFloatsByEqualityOperator
            if (DataChanged != null) DataChanged(this, new IniDataChangedEventArgs<double>(requestedValue, value));
            Set(value.ToString());
            requestedValue = value;
        }

        public new event EventHandler<IniDataChangedEventArgs<double>> DataChanged;
    }
    public class BooleanData : StringData, IIniData<bool>
    {
        public BooleanData(IniSection inisection, string key, bool defaultvalue) : base(inisection, key, defaultvalue.ToString())
        {
            Get();
        }

        private bool requestedValue;

        public new bool Get()
        {
            return requestedValue = bool.Parse(base.Get());
        }
        public void Set(bool value)
        {
            if (value == requestedValue) return;
            if (DataChanged != null) DataChanged(this, new IniDataChangedEventArgs<bool>(requestedValue, value));
            Set(value.ToString());
            requestedValue = value;
        }

        public new event EventHandler<IniDataChangedEventArgs<bool>> DataChanged;
    }
    public class YesNoData : StringData, IIniData<bool>
    {
        public YesNoData(IniSection inisection, string key, bool defaultvalue)
            : base(inisection, key, defaultvalue ? "Yes" : "No")
        {
            Get();
        }

        private bool requestedValue;
        public new bool Get()
        {
            return requestedValue = (base.Get() ?? string.Empty).ToLower() == "yes";
        }
        public void Set(bool value)
        {
            if (requestedValue == value) return;
            if (DataChanged != null) DataChanged(this, new IniDataChangedEventArgs<bool>(requestedValue, value));
            Set(value.ToString());
            requestedValue = value;
        }

        public new event EventHandler<IniDataChangedEventArgs<bool>> DataChanged;
    }
    public class PointData : StringData, IIniData<Point>
    {
        public PointData(IniSection section, string key, Point value)
            : base(section, key, value.ToString())
        {
        }

        private Point requestedValue;

        public new Point Get()
        {
            return Point.Parse(base.Get());
        }
        public void Set(Point value)
        {
            if (requestedValue == value) return;
            if (DataChanged != null) DataChanged(this, new IniDataChangedEventArgs<Point>(requestedValue, value));
            Set(value.ToString());
            requestedValue = value;
        }

        public new event EventHandler<IniDataChangedEventArgs<Point>> DataChanged;
    }
    public class ColorData : StringData, IIniData<Color>
    {
        public ColorData(IniSection section, string key, Color value)
            : base(section, key, value.ToString())
        {
            defaultValue = value;
        }

        private readonly Color defaultValue;
        private Color requestedValue;

        public new Color Get()
        {
            var result = ColorConverter.ConvertFromString(base.Get());
            return result == null ? defaultValue : (Color) result;
        }
        public void Set(Color value)
        {
            if (requestedValue == value) return;
            if (DataChanged != null) DataChanged(this, new IniDataChangedEventArgs<Color>(requestedValue, value));
            Set(value.ToString());
            requestedValue = value;
        }

        public new event EventHandler<IniDataChangedEventArgs<Color>> DataChanged;
    }

    public class StringListData : IniSection, IIniData<List<string>>
    {
        public StringListData(IniFile iniFile, string sectionName) : base(iniFile, sectionName)
        {
            data = new StringData(this, null);
            countData = new Int32Data(this, "Count", -1);
            Get();
        }

        private readonly StringData data;
        private readonly Int32Data countData;

        private List<string> requestedValue;
        private bool requested;

        public List<string> Get()
        {
            if (!requested)
            {
                requested = true;
                requestedValue = new List<string>();
                var count = countData.Get();
                for (var i = 0; i < count; i++)
                {
                    data.DataKey = i.ToString();
                    requestedValue.Add(data.Get());
                }
            }
            return requestedValue.ToList(); // create a copy
        }
        public void Set(List<string> value)
        {
            if (requestedValue == value) return;
            if (DataChanged != null) DataChanged(this, new IniDataChangedEventArgs<List<string>>(requestedValue, value));
            requestedValue = value.ToList();    // create a copy
            var count = value.Count;
            countData.Set(count);
            for (var i = 0; i < count; i++)
            {
                data.DataKey = i.ToString();
                data.Set(value[i]);
            }
        }

        public event EventHandler<IniDataChangedEventArgs<List<string>>> DataChanged;
    }
    public class DoubleListData : IniSection, IIniData<List<double>>
    {
        public DoubleListData(IniFile iniFile, string sectionName) : base(iniFile, sectionName)
        {
            data = new DoubleData(this, null, double.NaN);
            countData = new Int32Data(this, "Count", -1);
            Get();
        }

        private readonly DoubleData data;
        private readonly Int32Data countData;

        private List<double> requestedValue;
        private bool requested;

        public List<double> Get()
        {
            if (!requested)
            {
                requested = true;
                requestedValue = new List<double>();
                var count = countData.Get();
                for (var i = 0; i < count; i++)
                {
                    data.DataKey = i.ToString();
                    requestedValue.Add(data.Get());
                }
            }
            return requestedValue.ToList(); // create a copy
        }
        public void Set(List<double> value)
        {
            if (requestedValue == value) return;
            if (DataChanged != null) DataChanged(this, new IniDataChangedEventArgs<List<double>>(requestedValue, value));
            requestedValue = value.ToList();    // create a copy
            var count = value.Count;
            countData.Set(count);
            for (var i = 0; i < count; i++)
            {
                data.DataKey = i.ToString();
                data.Set(value[i]);
            }
        }

        public event EventHandler<IniDataChangedEventArgs<List<double>>> DataChanged;
    }

    public class StringDictionaryData : IniSection, IIniDataWithParam<string, string>
    {
        public StringDictionaryData(IniFile iniFile, string sectionName, string defaultValue = null) : base(iniFile, sectionName)
        {
            Value = defaultValue;
        }

        protected readonly string Value;
        private readonly Dictionary<string, StringData> dictionary = new Dictionary<string, StringData>();

        public string Get(string key)
        {
            if (!dictionary.ContainsKey(key)) dictionary.Add(key, new StringData(this, key, Value));
            return dictionary[key].Get();
        }
        public void Set(string key, string data)
        {
            if (dictionary.ContainsKey(key)) dictionary.Add(key, new StringData(this, key, Value));
            dictionary[key].Set(data);
        }

        public IIniData<string> GetData(string key)
        {
            if (dictionary.ContainsKey(key)) dictionary.Add(key, new StringData(this, key, Value));
            return dictionary[key];
        }
    }

    public class Int32DoubleDictionaryData : StringDictionaryData, IIniDataWithParam<int, double>
    {
        public Int32DoubleDictionaryData(IniFile iniFile, string sectionName, double value = double.NaN)
            : base(iniFile, sectionName, value.ToString())
        {
        }

        public double Get(int key)
        {
            return double.Parse(Get(key.ToString()));
        }
        public void Set(int key, double data)
        {
            Set(key.ToString(), data.ToString());
        }

        public IIniData<double> GetData(int key)
        {
            return new DoubleData(this, key.ToString(), double.Parse(Value));
        }
    }
}
