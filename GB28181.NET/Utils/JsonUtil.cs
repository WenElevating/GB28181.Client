using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GB28181.NET.Utils
{
    public class JsonUtil
    {
        /// <summary>
        /// Json文件读取
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public async static Task<T?> ReadJsonFileAsync<T>(string filePath)
        {
            // 判断文件是否存在和有权限
            if (!File.Exists(filePath))
            {
                return default;
            }

            string json = await File.ReadAllTextAsync(filePath)
                .ConfigureAwait(false);

            return JsonConvert.DeserializeObject<T>(json);
        }

        /// <summary>
        /// Json文件写入
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="filePath"></param>
        /// <param name="obj"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public async static Task WriteJsonFileAsync<T>(string filePath, T? obj, Encoding? encoding = null) where T : class
        {
            if (obj == null)
            {
                return;
            }

            if (!File.Exists(filePath))
            {
                File.Create(filePath).Close();
            }

            var json = JsonConvert.SerializeObject(obj);

            await File.WriteAllTextAsync(filePath, json, encoding ?? Encoding.UTF8)
                .ConfigureAwait(false);
        }
    }
}
