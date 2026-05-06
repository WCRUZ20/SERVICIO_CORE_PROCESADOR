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

        [JsonPropertyName("type")]
        public string Type { get; set; } = "simple";

        [JsonPropertyName("tax_status")]
        public string? taxstatus { get; set; }

        [JsonPropertyName("weight")]
        public string? weight { get; set; }

        [JsonPropertyName("dimensions")]
        public Dimensions? dimension { get; set; }

        [JsonPropertyName("upsell_ids")]
        public List<Upsell_ids>? upsell_ids { get; set; }

        [JsonPropertyName("cross_sell_ids")]
        public List<Cross_sell_ids>? cross_sell_ids { get; set; }

    }

    public class Dimensions
    {
        [JsonPropertyName("length")]
        public string? length { get; set; }

        [JsonPropertyName("width")]
        public string? width { get; set; }

        [JsonPropertyName("height")]
        public string? height { get; set; }
    }

    public class Upsell_ids
    {
        public string Id { get; set; }
    }

    public class Cross_sell_ids
    {
        public string Id { get; set; }
    }

}
