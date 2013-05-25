using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Mygod.Xml.Linq
{
    public static class XHelper
    {
        public static readonly XmlWriterSettings WriterSettings = new XmlWriterSettings { Indent = true, Encoding = Encoding.UTF8 };

        public static XDocument Load(string path)
        {
            return XDocument.Parse(File.ReadAllText(path));
        }

        public static string GetString(this XContainer container)
        {
            var builder = new StringBuilder();
            using (var xw = XmlWriter.Create(builder, WriterSettings)) new XDocument(container).Save(xw);
            return builder.ToString();
        }

        public static XElement GetElement(this XContainer container, XName name)
        {
            var r = container.Element(name);
            if (r == null) throw new FileFormatException();
            return r;
        }

        public static string GetAttributeValue(this XElement element, XName name)
        {
            var attr = element.Attribute(name);
            return attr == null ? null : attr.Value;
        }

        public static T GetAttributeValue<T>(this XElement element, XName name)
        {
            if (typeof(T).IsSubclassOf(typeof(Enum))) return element.GetAttributeValueEnum<T>(name);
            var parse = typeof(T).GetMethod("Parse", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string) }, null);
            if (parse == null || parse.ReturnType != typeof(T)) throw new NotSupportedException(
                "You must define a public static T Parse(string) method before using GetAttributeValueWithDefault<T>!");
            return (T)parse.Invoke(null, new object[] { element.GetAttributeValue(name) });
        }

        private static T GetAttributeValueEnum<T>(this XElement element, XName name)
        {
            return (T)Enum.Parse(typeof(T), element.GetAttributeValue(name));
        }

        public static void GetAttributeValue<T>(this XElement element, out T result, XName name)
        {
            if (typeof(T).IsSubclassOf(typeof(Enum))) element.GetAttributeValueEnum(out result, name);
            else result = element.GetAttributeValue<T>(name);
        }

        private static void GetAttributeValueEnum<T>(this XElement element, out T result, XName name)
        {
            result = (T)Enum.Parse(typeof(T), element.GetAttributeValue(name));
        }

        public static T GetAttributeValueWithDefault<T>(this XElement element, XName name, T defaultValue = default(T))
        {
            if (typeof(T).IsSubclassOf(typeof(Enum))) return element.GetAttributeValueEnumWithDefault(name, defaultValue);
            var str = element.GetAttributeValue(name);
            if (string.IsNullOrWhiteSpace(str)) return defaultValue;
            var parse = typeof(T).GetMethod("Parse", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string) }, null);
            if (parse == null || parse.ReturnType != typeof(T)) throw new NotSupportedException(
                "You must define a public static T Parse(string) method before using GetAttributeValueWithDefault<T>!");
            return (T) parse.Invoke(null, new object[] { str });
        }

        public static void GetAttributeValueWithDefault<T>(this XElement element, out T result, XName name, T defaultValue = default(T))
        {
            if (typeof(T).IsSubclassOf(typeof(Enum))) element.GetAttributeValueEnumWithDefault(out result, name, defaultValue);
            else result = element.GetAttributeValueWithDefault(name, defaultValue);
        }

        private static T GetAttributeValueEnumWithDefault<T>(this XElement element, XName name, T defaultValue = default(T))
        {
            var str = element.GetAttributeValue(name);
            return string.IsNullOrWhiteSpace(str) ? defaultValue : (T)Enum.Parse(typeof(T), str);
        }

        private static void GetAttributeValueEnumWithDefault<T>(this XElement element, out T result, XName name,
                                                                T defaultValue = default(T))
        {
            var str = element.GetAttributeValue(name);
            result = string.IsNullOrWhiteSpace(str) ? defaultValue : (T)Enum.Parse(typeof(T), str);
        }

        public static void SetAttributeValueWithDefault<T>(this XElement element, XName name, T value, T defaultValue = default(T))
        {
            if (!ReferenceEquals(value, defaultValue) && !ReferenceEquals(value, null) && !value.Equals(defaultValue))
                element.SetAttributeValue(name, value.ToString());
        }
    }
}