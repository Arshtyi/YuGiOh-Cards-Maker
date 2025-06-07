// 卡片数据结构
using System.Text.Json.Serialization;

namespace Yugioh
{
    public class Card
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        [JsonPropertyName("frameType")]
        public string? FrameType { get; set; }
        [JsonPropertyName("cardimage")]
        public string? CardImage { get; set; }
        // ...可根据需要扩展字段...
    }
}
