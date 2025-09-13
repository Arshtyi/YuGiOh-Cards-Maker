using System.Text.Json.Serialization;
namespace Yugioh
{
    public class Card
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        [JsonPropertyName("frameType")]
        public string? FrameType { get; set; }
        [JsonPropertyName("cardType")]
        public string? CardType { get; set; }
        [JsonPropertyName("cardImage")]
        // 原 cardImage -> 更语义化为 ArtworkFilename (保留 JSON 名称)
        public string? ArtworkFilename { get; set; }
        [JsonPropertyName("linkVal")]
        // 原 linkVal -> LinkRating 更明确表示 link 数值
        public int? LinkRating { get; set; }
        [JsonPropertyName("linkMarkers")]
        public List<string>? LinkMarkers { get; set; }
        [JsonPropertyName("attribute")]
        // 原 attribute -> AttributeName 更不易与 C# Attribute 混淆
        public string? AttributeName { get; set; }
        [JsonPropertyName("scale")]
        public int? PendulumScale { get; set; }
        [JsonPropertyName("atk")]
        [JsonConverter(typeof(StringNumericConverter))]
        // 原 atk -> Attack
        public string? Attack { get; set; }
        [JsonPropertyName("def")]
        [JsonConverter(typeof(StringNumericConverter))]
        // 原 def -> Defense
        public string? Defense { get; set; }
        [JsonPropertyName("level")]
        public int? Level { get; set; }
        [JsonPropertyName("race")]
        public string? Race { get; set; }
        [JsonPropertyName("typeline")]
        // 原 typeline -> TypeLine
        public string? TypeLine { get; set; }
        [JsonPropertyName("pendulumDescription")]
        public string? PendulumDescription { get; set; }
        [JsonPropertyName("description")]
        public string? Description { get; set; }
    }
}
