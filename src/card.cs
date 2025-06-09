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
        [JsonPropertyName("cardtype")]
        public string? CardType { get; set; }
        [JsonPropertyName("cardimage")]
        public string? CardImage { get; set; }
        [JsonPropertyName("linkval")]
        public int? LinkValue { get; set; }
        [JsonPropertyName("attribute")]
        public string? Attribute { get; set; }
        [JsonPropertyName("scale")]
        public int? PendulumScale { get; set; }
        [JsonPropertyName("atk")]
        [JsonConverter(typeof(StringNumericConverter))]
        public string? Atk { get; set; }
        [JsonPropertyName("def")]
        [JsonConverter(typeof(StringNumericConverter))]
        public string? Def { get; set; }
        [JsonPropertyName("level")]
        public int? Level { get; set; }
    }
}
