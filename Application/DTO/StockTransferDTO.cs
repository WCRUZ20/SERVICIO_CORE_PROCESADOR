using System;
using System.Collections.Generic;

namespace Application.DTO
{
    /// <summary>
    /// DTO para transferencias de stock - usado en la capa de aplicación
    /// </summary>
    public class StockTransferDTO
    {
        public int DocEntry { get; set; }
        public int DocNum { get; set; }
        public DateTime DocDate { get; set; }
        public string FromWarehouse { get; set; } = string.Empty;
        public string ToWarehouse { get; set; } = string.Empty;
        public string CardCode { get; set; } = string.Empty;
        public string CardName { get; set; } = string.Empty;
        public string DocStatus { get; set; } = string.Empty;
        public string Comments { get; set; } = string.Empty;
        public DateTime CreationDate { get; set; }
        public DateTime UpdateDate { get; set; }
        public string AuthorizationStatus { get; set; } = string.Empty;
        public string BPLID { get; set; } = string.Empty;
        public string BPLName { get; set; } = string.Empty;
        
        // Propiedades adicionales según necesidad
        public List<StockTransferLineDTO>? Lines { get; set; }
    }

    /// <summary>
    /// DTO para líneas de transferencia de stock
    /// </summary>
    public class StockTransferLineDTO
    {
        public int LineNum { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string ItemDescription { get; set; } = string.Empty;
        public double Quantity { get; set; }
        public double Price { get; set; }
        public string WarehouseCode { get; set; } = string.Empty;
        public string FromWarehouseCode { get; set; } = string.Empty;
    }
}
