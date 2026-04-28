namespace Domain.Configuration
{
    public class WorkerSettings
    {
        public WorkerConfig? Worker { get; set; }
        public DocumentConfig? getDocumentSAP { get; set; }
        public DocumentConfig? processDocumentSAP { get; set; }
        public ItemConfig? getItemClienteSAP { get; set; }
        public ItemConfig? getItemDealerSAP { get; set; }
        public ItemConfig? processItemClienteSAP { get; set; }
        public ItemConfig? processItemDealerSAP { get; set; }
        public ItemConfig? getStockClienteSAP { get; set; }
        public ItemConfig? getStockDealerSAP { get; set; }
        public StockConfig? processStockClienteSAP { get; set; }
        public StockConfig? processStockDealerSAP { get; set; }
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

    public class RetentionDaysConfig
    {
        public int RetentionDays { get; set; }
        
    }

    public class FileLoggerConfig
    {
        public string LogLevel { get; set; } = "Information";
    }
}






