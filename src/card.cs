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
        public string? CardImage { get; set; }
        [JsonPropertyName("linkVal")]
        public int? LinkValue { get; set; }
        [JsonPropertyName("linkMarkers")]
        public List<string>? LinkMarkers { get; set; }
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
        [JsonPropertyName("race")]
        public string? Race { get; set; }
        [JsonPropertyName("typeline")]
        public string? Typeline { get; set; }
        [JsonPropertyName("pendulumDescription")]
        public string? PendulumDescription { get; set; }
        [JsonPropertyName("description")]
        public string? Description { get; set; }
    }
}
