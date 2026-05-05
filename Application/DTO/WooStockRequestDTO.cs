using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Application.DTO
{
    public class WooStockRequestDTO
    {
        [JsonPropertyName("sku")]
        public string SKU { get; set; } = string.Empty;

        [JsonPropertyName("stockQuantity")]
        public int StockQuantity { get; set; }        
    }
}
