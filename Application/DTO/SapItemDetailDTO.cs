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
        public decimal? regularPrice {  get; set; }
        public decimal? stockQuantity {  get; set; }
        public string? status {  get; set; }
        public string? type { get; set; }
        public string? description {  get; set; }
        public bool manageStock { get; set; } = true;
        public string? shortDescription { get; set; }

        public string? taxstatus { get; set; }
        public string? weight { get; set; }

        public DimensionsD? dimension { get; set; }

        public List<Upsell_idsD>? upsell_ids { get; set; }

        public List<Cross_sell_idsD>? cross_sell_ids { get; set; }

    }

    public class DimensionsD
    {
        public string? length { get; set; }
        public string? width { get; set; }
        public string? height { get; set; }
    }
    public class Upsell_idsD
    {
        public string Id { get; set; }
    }

    public class Cross_sell_idsD
    {
        public string Id { get; set; }
    }
}
