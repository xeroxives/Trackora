using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace WpfApp14.Models
{
    public class FontConfig
    {
        [JsonPropertyName("Context")]
        public string Context { get; set; } = string.Empty;

        [JsonPropertyName("FontFile")]
        public string FontFile { get; set; } = string.Empty;

        [JsonPropertyName("FontSize")]
        public double FontSize { get; set; } = 0.0;
    }
}
