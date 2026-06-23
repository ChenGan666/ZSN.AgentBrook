using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;


namespace ZSN.Utils.Core.Helpers
{
    /// <summary>
    ///     序列化辅助类
    /// </summary>
    public class SerializeHelper
    {
        /// <summary>
        ///     序列化对象
        /// </summary>
        /// <typeparam name="T">需要序列化的对象类型，必须声明[Serializable]特征</typeparam>
        /// <param name="obj">需要序列化的对象</param>
        /// <param name="serializeFilePath">序列化后的文件地址</param>
        public static void BinarySerialize<T>(T obj, string serializeFilePath)
        {
            using (var fs = new FileStream(serializeFilePath, FileMode.Create))
            {
                var json = JsonSerializer.Serialize<object>(obj);
                fs.Write(Encoding.UTF8.GetBytes(json));
            }
        }

        /// <summary>
        ///     反序列化对象
        /// </summary>
        /// <typeparam name="T">需要序列化的对象类型，必须声明[Serializable]特征</typeparam>
        /// <param name="serializeFilePath">反序列化对象的文件地址</param>
        public static T BinaryDeserialize<T>(string serializeFilePath) where T : class
        {
            using (FileStream stream = File.Open(serializeFilePath, FileMode.Open))
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    string fileContent = reader.ReadToEnd();
                    return JsonSerializer.Deserialize<T>(fileContent);
                }
            }
        }
    }
}