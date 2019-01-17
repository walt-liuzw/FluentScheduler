using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace JobwsClient.Common
{
    public static class StringExtension
    {
        public static int? ToInt(this string source)
        {
            int tempInt = -1;
            if (int.TryParse(source, out tempInt))
            {
                return tempInt;
            }
            return null;
        }
        public static DateTime? ToDateTime(this string source)
        {
            DateTime tempInt = DateTime.MinValue;
            if (DateTime.TryParse(source, out tempInt))
            {
                return tempInt;
            }
            return null;
        }
    }

    public static class JsonExtension
    {
        public static string ToJson(this object obj)
        {
            string jsonValue = "";
            try
            {
                if (obj != null)
                    jsonValue = JsonConvert.SerializeObject(obj);
            }
            catch (Exception ex)
            {
                LogHelper.Instance.Dump("Json序列化异常", LogType.Error, ex);
            }
            return jsonValue;
        }
        public static T ToObject<T>(this string jsonValue)
        {
            try
            {
                if (!string.IsNullOrEmpty(jsonValue))
                    return JsonConvert.DeserializeObject<T>(jsonValue);
                else
                    return default(T);
            }
            catch (Exception ex)
            {
                LogHelper.Instance.Dump("Json反序列化异常", LogType.Error, ex);
                return default(T);
            }
        }
    }
}
