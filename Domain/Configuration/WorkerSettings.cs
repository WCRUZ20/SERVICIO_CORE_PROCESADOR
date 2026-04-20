namespace Domain.Configuration
{
    public class WorkerSettings
    {
        public WorkerConfig? Worker { get; set; }
        public DocumentConfig? getDocumentSAP { get; set; }
        public DocumentConfig? processDocumentSAP { get; set; }
        public DocumentConfig? processHook { get; set; }
        
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
    public class RetentionDaysConfig
    {
        public int RetentionDays { get; set; }
        
    }

    public class FileLoggerConfig
    {
        public string LogLevel { get; set; } = "Information";
    }
}






