namespace Domain.Configuration
{
    public class OdbcSettings
    {
        //public SapOdbcConfig? SAP { get; set; }
        public HanaOdbcConfig HANA { get; set; }
    }

    public class HanaOdbcConfig
    {
        public string? ConnectionString { get; set; }
        public string? Driver { get; set; }
        public string? Server { get; set; }
        public string? Database { get; set; }
        public string? UserId { get; set; }
        public string? Password { get; set; }
        public int CommandTimeout { get; set; } = 30;
       
    }
}






