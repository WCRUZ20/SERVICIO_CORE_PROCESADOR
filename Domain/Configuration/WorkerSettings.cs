namespace Domain.Configuration
{
    public class WorkerSettings
    {
        public WorkerConfig? Worker { get; set; }
        public DocumentConfig? getDocumentSAP { get; set; }
        public DocumentConfig? processDocumentSAP { get; set; }

        //variable para habilitar proceso de articulo
        public ItemConfig? getItemClienteSAP { get; set; }
        public ItemConfig? getItemDealerSAP { get; set; }
        public ItemConfig? processItemClienteSAP { get; set; }
        public ItemConfig? processItemDealerSAP { get; set; }

        //variable para habilitar proceso de stock
        public StockConfig? getStockClienteSAP { get; set; }
        public StockConfig? getStockDealerSAP { get; set; }
        public StockConfig? processStockClienteSAP { get; set; }
        public StockConfig? processStockDealerSAP { get; set; }

        //variable para habilitar proceso de precio
        public PrecioConfig? getPrecioClienteSAP { get; set; }
        public PrecioConfig? getPrecioDealerSAP { get; set; }
        public PrecioConfig? processPrecioClienteSAP { get; set; }
        public PrecioConfig? processPrecioDealerSAP { get; set; }

        //variable para habilitar proceso de ordenes
        public OrderConfig? getOrderWooCliente { get; set; }
        public OrderConfig? getOrderWooDealer { get; set; }
        public OrderConfig? processOrderWooCliente { get; set; }
        public OrderConfig? processOrderWooDealer { get; set; }

        public RetentionDaysConfig? RetentionDaysConfig { get; set; }
        public FileLoggerConfig? FileLoggerConfig { get; set; }
    }

    public class WorkerConfig
    {
        public int IsEnableFlag { get; set; }
        public int LoopInterval { get; set; } // Milisegundos
    }

    public class DocumentConfig
    {
        public int IsEnableFlag { get; set; }
    }

    public class ItemConfig
    {
        public int IsEnableFlag { get; set; }
    }

    public class StockConfig
    {
        public int IsEnableFlag { get; set; }
    }

    public class PrecioConfig
    {
        public int IsEnableFlag { get; set; }
    }

    public class OrderConfig
    {
        public int IsEnableFlag { get; set; }
    }

    public class RetentionDaysConfig
    {
        public int RetentionDays { get; set; }
        
    }

    public class FileLoggerConfig
    {
        public string LogLevel { get; set; } = "Information";
    }
}






