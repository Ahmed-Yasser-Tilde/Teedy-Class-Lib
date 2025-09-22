using System.Text.Json.Serialization;

namespace Teedy.CL.Models.Tags
{
    public class GetTags
    {
        [JsonPropertyName("tags")]
        public List<Tag> Tags { get; set; }  // List of tags
    }
}
