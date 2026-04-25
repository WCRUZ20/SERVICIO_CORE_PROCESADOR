using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTO
{
    public class WooProductResponse
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Status { get; set; }
        public string? Sku { get; set; }
        public decimal Price { get; set; }
        public string? Type { get; set; }
        public decimal StockQuantity { get; set; }
        public decimal RegularPrice { get; set; }
    }
}
