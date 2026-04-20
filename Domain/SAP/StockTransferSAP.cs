using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.SAP
{


    /// <summary>
    /// Representa la cabecera de una Transferencia de Stock (SAP B1 - OWTR)
    /// </summary>
    public class StockTransferSAP
    {
        /// <summary>
        /// Identificador interno del documento
        /// </summary>
        public int DocEntry { get; set; }

        /// <summary>
        /// Número del documento
        /// </summary>
        public int DocNum { get; set; }

        /// <summary>
        /// Fecha del documento
        /// </summary>
        public DateTime DocDate { get; set; }

        /// <summary>
        /// Fecha de contabilización
        /// </summary>
        public DateTime TaxDate { get; set; }


        public string CardCode { get; set; }
        public string CardName { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string JrnlMemo { get; set; }

        /// <summary>
        /// Fecha de vencimiento
        /// </summary>
        public DateTime DueDate { get; set; }

        /// <summary>
        /// Comentarios del documento
        /// </summary>
        public string? Comments { get; set; }

        /// <summary>
        /// Referencia
        /// </summary>
        public string? Reference { get; set; }

        /// <summary>
        /// Usuario que creó el documento
        /// </summary>
        public int UserSign { get; set; }

        /// <summary>
        /// Usuario que actualizó el documento
        /// </summary>
        public int? UserSign2 { get; set; }

        /// <summary>
        /// Estado del documento (O = Open, C = Closed)
        /// </summary>
        public string DocStatus { get; set; } = string.Empty;

        /// <summary>
        /// Indicador de cancelación (Y / N)
        /// </summary>
        public string Canceled { get; set; } = string.Empty;

        /// <summary>
        /// Serie del documento
        /// </summary>
        public int Series { get; set; }

        /// <summary>
        /// Indicador de impresión
        /// </summary>
        public string Printed { get; set; } = string.Empty;

        /// <summary>
        /// Fecha de creación del registro
        /// </summary>
        public DateTime CreateDate { get; set; }

        /// <summary>
        /// Hora de creación (formato SAP: HHMM)
        /// </summary>
        public int CreateTime { get; set; }

        /// <summary>
        /// Fecha de última actualización
        /// </summary>
        public DateTime UpdateDate { get; set; }

        /// <summary>
        /// Hora de última actualización (formato SAP: HHMM)
        /// </summary>
        public int UpdateTime { get; set; }

        /// <summary>
        /// Indicador de origen del documento
        /// </summary>
        public string? ObjType { get; set; }

        /// <summary>
        /// Indicador de integración externa
        /// </summary>
        public string? U_ExternalRef { get; set; }

        public IEnumerable<StockTransferLine> StockTransferLines { get; set; }



    }



    /// <summary>
    /// Representa una línea de Transferencia de Stock (SAP B1 - WTR1)
    /// </summary>
    public class StockTransferLine
    {
        /// <summary>
        /// Identificador interno del documento (FK a OWTR)
        /// </summary>
        public int DocEntry { get; set; }

        /// <summary>
        /// Número de línea dentro del documento
        /// </summary>
        public int LineNum { get; set; }

        /// <summary>
        /// Código del artículo
        /// </summary>
        public string ItemCode { get; set; } = string.Empty;

        /// <summary>
        /// Descripción del artículo
        /// </summary>
        public string? Dscription { get; set; }

        /// <summary>
        /// Cantidad transferida
        /// </summary>
        public decimal Quantity { get; set; }

        /// <summary>
        /// Precio unitario
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// Moneda del documento
        /// </summary>
        public string? Currency { get; set; }

        /// <summary>
        /// Almacén de origen
        /// </summary>
        public string FromWhsCod { get; set; } = string.Empty;

        /// <summary>
        /// Almacén de destino
        /// </summary>
        public string WhsCode { get; set; } = string.Empty;

        /// <summary>
        /// Unidad de medida
        /// </summary>
        public string? UnitMsr { get; set; }

        /// <summary>
        /// Cantidad en unidades de inventario
        /// </summary>
        public decimal? InvQty { get; set; }

        /// <summary>
        /// Indicador de gestión por lote o serie
        /// </summary>
        public string? ManBtchNum { get; set; }

        /// <summary>
        /// Indicador de gestión por número de serie
        /// </summary>
        public string? ManSerNum { get; set; }

        /// <summary>
        /// Código de proyecto
        /// </summary>
        public string? Project { get; set; }

        /// <summary>
        /// Centro de costo (dimensión 1)
        /// </summary>
        public string? OcrCode { get; set; }

        /// <summary>
        /// Centro de costo (dimensión 2)
        /// </summary>
        public string? OcrCode2 { get; set; }

        /// <summary>
        /// Centro de costo (dimensión 3)
        /// </summary>
        public string? OcrCode3 { get; set; }

        /// <summary>
        /// Centro de costo (dimensión 4)
        /// </summary>
        public string? OcrCode4 { get; set; }

        /// <summary>
        /// Centro de costo (dimensión 5)
        /// </summary>
        public string? OcrCode5 { get; set; }

        /// <summary>
        /// Referencia a documento base (si aplica)
        /// </summary>
        public int? BaseEntry { get; set; }

        /// <summary>
        /// Tipo de documento base
        /// </summary>
        public int? BaseType { get; set; }

        public string? LineStatus { get; set; }

        /// <summary>
        /// Línea del documento base
        /// </summary>
        public int? BaseLine { get; set; }

        /// <summary>
        /// Código de lote (si aplica)
        /// </summary>
        public StockTransferBatch StockTransferBatches { get; set; }

        /// <summary>
        /// Número de serie (si aplica)
        /// </summary>
        public string? SerialNum { get; set; }

        /// <summary>
        /// Campo de usuario – referencia externa
        /// </summary>
        public string? U_ExternalRef { get; set; }
    }




    /// <summary>
    /// Representa un lote asociado a una línea de Transferencia de Stock
    /// (SAP B1 - IBT1)
    /// </summary>
    public class StockTransferBatch
    {
        /// <summary>
        /// Tipo de documento SAP (67 = Transferencia de stock)
        /// </summary>
        public int BaseType { get; set; }

        /// <summary>
        /// DocEntry del documento base (OWTR)
        /// </summary>
        public int BaseEntry { get; set; }

        /// <summary>
        /// Número de línea del documento base (WTR1.LineNum)
        /// </summary>
        public int BaseLinNum { get; set; }

        /// <summary>
        /// Código del artículo
        /// </summary>
        public string ItemCode { get; set; } = string.Empty;

        /// <summary>
        /// Código del lote
        /// </summary>
        public string BatchNum { get; set; } = string.Empty;

        /// <summary>
        /// Cantidad del lote utilizada en la línea
        /// </summary>
        public decimal Quantity { get; set; }

        /// <summary>
        /// Almacén donde se toma el lote (origen)
        /// </summary>
        public string WhsCode { get; set; } = string.Empty;

        /// <summary>
        /// Dirección del movimiento:
        /// 0 = Salida, 1 = Entrada
        /// </summary>
        public int Direction { get; set; }

        /// <summary>
        /// Fecha de vencimiento del lote
        /// </summary>
        public DateTime? ExpDate { get; set; }

        /// <summary>
        /// Fecha de fabricación del lote
        /// </summary>
        public DateTime? MnfDate { get; set; }

        /// <summary>
        /// Indicador de sistema (SAP interno)
        /// </summary>
        public string? SysNumber { get; set; }

    }


}
