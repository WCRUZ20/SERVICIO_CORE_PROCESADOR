using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTO
{
    /// <summary>
    /// Detalle del artículo consultado desde HANA/SAP para construir el payload de integración.
    /// </summary>
    public class SapItemDetailDTO
    {
        public string? ItemCode { get; set; }
        public string? ItemName { get; set; }
        public decimal? Price { get; set; }
        public string? name {  get; set; }
        public string? sku { get; set; }
        public string? regularPrice {  get; set; }
        public decimal? stockQuantity {  get; set; }
        public string? status {  get; set; }
        public string? type { get; set; }
        public string? description {  get; set; }
        public bool manageStock { get; set; } = true;
        public string? shortDescription { get; set; }

    }

}
