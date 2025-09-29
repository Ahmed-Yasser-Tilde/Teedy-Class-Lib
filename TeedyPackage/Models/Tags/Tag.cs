using System.Text.Json.Serialization;

namespace TeedyPackage.Models.Tags
{
    public class Tag
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("color")]
        public string Color { get; set; }
        [JsonPropertyName("parent")]
        public string Parent { get; set; }
    }

}
