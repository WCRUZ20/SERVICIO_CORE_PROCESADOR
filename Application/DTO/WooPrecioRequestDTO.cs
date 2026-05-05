using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Application.DTO
{
    public class WooPrecioRequestDTO
    {
        [JsonPropertyName("sku")]
        public string SKU { get; set; } = string.Empty;

        [JsonPropertyName("regularPrice")]
        public string RegularPrice { get; set; } = "0.00";
    }
}
