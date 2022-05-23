using System.Text.Json.Serialization;

namespace TouchController
{
    public class TouchPoint
    {
        [JsonPropertyName("id")]
        public object Id { get; set; }
        [JsonPropertyName("x")]
        public float X { get; set; }
        [JsonPropertyName("y")]
        public float Y { get; set; }
    }
}
