using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;

namespace Mygod.Runtime.Serialization.Json
{
    public static class JsonSerialization
    {
        private static readonly Dictionary<Type, DataContractJsonSerializer>
            Serializers = new Dictionary<Type, DataContractJsonSerializer>();
        private static DataContractJsonSerializer GetSerializer<T>()
        {
            var type = typeof(T);
            DataContractJsonSerializer result;
            if (Serializers.TryGetValue(type, out result)) return result;
            return Serializers[type] = new DataContractJsonSerializer(type);
        }

        public static void SerializeToFile<T>(string path, T value)
        {
            using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read))
                GetSerializer<T>().WriteObject(stream, value);
        }
        public static T DeserializeFromFile<T>(string path)
        {
            if (!File.Exists(path)) return default(T);
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                return (T) GetSerializer<T>().ReadObject(stream);
        }
    }
}
