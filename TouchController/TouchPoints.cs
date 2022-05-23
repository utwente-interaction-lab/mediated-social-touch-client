using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TouchController
{
    public class TouchPoints
    {
        [JsonPropertyName("width")]
        public float Width { get; set; }
        [JsonPropertyName("height")]
        public float Height { get; set; }
        [JsonPropertyName("touchpoints")]
        public List<TouchPoint> Points { get; set; } = new List<TouchPoint>();

        [JsonPropertyName("intensity")]
        public byte Intensity { get; set; }
    }
}
