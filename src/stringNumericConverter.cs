using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Yugioh
{
    public class StringNumericConverter : JsonConverter<string>
    {
        public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // 处理不同类型的攻击力值
            if (reader.TokenType == JsonTokenType.Null)
            {
                return string.Empty;
            }
            else if (reader.TokenType == JsonTokenType.String)
            {
                return reader.GetString() ?? string.Empty;
            }
            else if (reader.TokenType == JsonTokenType.Number)
            {
                // 将数字转换为字符串
                return reader.GetInt64().ToString();
            }
            
            // 处理其他意外情况
            throw new JsonException($"无法将 {reader.TokenType} 转换为字符串");
        }

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
            }
            else
            {
                writer.WriteStringValue(value);
            }
        }
    }
}
