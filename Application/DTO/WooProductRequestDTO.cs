using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Application.DTO
{
    /// <summary>
    /// Payload mínimo compatible con estructura WooCommerce para creación de artículos.
    /// </summary>
    public class WooProductRequestDTO
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = "simple";

        [JsonPropertyName("regular_price")]
        public string RegularPrice { get; set; } = "0.00";

        [JsonPropertyName("sku")]
        public string SKU { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = "draft";
    }

}
