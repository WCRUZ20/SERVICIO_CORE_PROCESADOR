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
    }

}
