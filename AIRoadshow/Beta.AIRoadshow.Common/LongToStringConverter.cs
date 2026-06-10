using Newtonsoft.Json;
using System;

namespace Beta.AIRoadshow.Common;

/// <summary>
/// 
/// </summary>
public class LongToStringConverter : JsonConverter
{
    // JavaScript 最大安全整数
    private const long JavaScriptMaxSafeInteger = 9007199254740991;

    /// <summary>
    /// 
    /// </summary>
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(long) || objectType == typeof(long?);
    }

    /// <summary>
    /// 
    /// </summary>
    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        // 如果需要支持反序列化，可以在这里实现逻辑
        throw new NotImplementedException("Unnecessary because CanRead is false. The type will skip the converter.");
    }

    /// <summary>
    /// 
    /// </summary>
    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        if (value is long longValue)
        {
            // 如果超过 JavaScript 的最大安全整数，则转换为字符串
            if (longValue > JavaScriptMaxSafeInteger || longValue < -JavaScriptMaxSafeInteger)
            {
                writer.WriteValue(longValue.ToString());
            }
            else
            {
                writer.WriteValue(longValue);
            }
        }
        else
        {
            writer.WriteNull();
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public override bool CanRead => false;
}
