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
        public string? ArtworkFilename { get; set; }
        [JsonPropertyName("linkVal")]
        public int? LinkValue { get; set; }
        [JsonPropertyName("linkMarkers")]
        public List<string>? LinkMarkers { get; set; }
        [JsonPropertyName("attribute")]
        public string? AttributeName { get; set; }
        [JsonPropertyName("scale")]
        public int? PendulumScale { get; set; }
        [JsonPropertyName("atk")]
        [JsonConverter(typeof(StringNumericConverter))]
        public string? Attack { get; set; }
        [JsonPropertyName("def")]
        [JsonConverter(typeof(StringNumericConverter))]
        public string? Defense { get; set; }
        [JsonPropertyName("level")]
        public int? Level { get; set; }
        [JsonPropertyName("race")]
        public string? Race { get; set; }
        [JsonPropertyName("typeline")]
        public string? TypeLine { get; set; }
        [JsonPropertyName("pendulumDescription")]
        public string? PendulumDescription { get; set; }
        [JsonPropertyName("description")]
        public string? Description { get; set; }
    }
}
