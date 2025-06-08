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
        [JsonPropertyName("linkval")]
        public int? LinkValue { get; set; }
    }
}
