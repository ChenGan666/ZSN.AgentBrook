using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ZSN.AI.Core.Utils
{
    /// <summary>
    /// 宽松的布尔值转换器，支持字符串到布尔值的转换
    /// 用于兼容MCP协议等可能将布尔值序列化为字符串的场景
    /// </summary>
    public class BooleanConverter : JsonConverter<bool>
    {
        public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.True:
                    return true;
                case JsonTokenType.False:
                    return false;
                case JsonTokenType.String:
                    // 支持字符串格式的布尔值
                    string stringValue = reader.GetString();
                    if (bool.TryParse(stringValue, out bool result))
                    {
                        return result;
                    }
                    // 支持数字字符串 "1" = true, "0" = false
                    if (stringValue == "1") return true;
                    if (stringValue == "0") return false;
                    
                    throw new JsonException($"Unable to convert \"{stringValue}\" to Boolean.");
                case JsonTokenType.Number:
                    // 支持数字格式 1 = true, 0 = false
                    int numberValue = reader.GetInt32();
                    return numberValue != 0;
                default:
                    throw new JsonException($"Unexpected token type {reader.TokenType} when parsing Boolean.");
            }
        }

        public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
        {
            writer.WriteBooleanValue(value);
        }
    }

    /// <summary>
    /// 可空布尔值转换器
    /// </summary>
    public class NullableBooleanConverter : JsonConverter<bool?>
    {
        public override bool? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            switch (reader.TokenType)
            {
                case JsonTokenType.True:
                    return true;
                case JsonTokenType.False:
                    return false;
                case JsonTokenType.String:
                    string stringValue = reader.GetString();
                    if (string.IsNullOrEmpty(stringValue))
                    {
                        return null;
                    }
                    if (bool.TryParse(stringValue, out bool result))
                    {
                        return result;
                    }
                    if (stringValue == "1") return true;
                    if (stringValue == "0") return false;
                    
                    throw new JsonException($"Unable to convert \"{stringValue}\" to Boolean.");
                case JsonTokenType.Number:
                    int numberValue = reader.GetInt32();
                    return numberValue != 0;
                default:
                    throw new JsonException($"Unexpected token type {reader.TokenType} when parsing Boolean.");
            }
        }

        public override void Write(Utf8JsonWriter writer, bool? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
            {
                writer.WriteBooleanValue(value.Value);
            }
            else
            {
                writer.WriteNullValue();
            }
        }
    }
}
