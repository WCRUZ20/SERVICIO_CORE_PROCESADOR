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

        [JsonPropertyName("regularPrice")]
        public string RegularPrice { get; set; } = "0.00";

        [JsonPropertyName("sku")]
        public string SKU { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = "draft";

        [JsonPropertyName("stockQuantity")]
        public int StockQuantity { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("manageStock")]
        public bool ManageStock { get; set; } = true;

        [JsonPropertyName("shortDescription")]
        public string ShortDescription { get; set; } = string.Empty;

    }

}
