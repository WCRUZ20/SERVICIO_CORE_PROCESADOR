namespace Domain.Configuration
{
    public class SapDiApiSettings
    {
        public DiApiCompanyConfig Company { get; set; } = new();
    }

    public class DiApiCompanyConfig
    {
        /// <summary>
        /// Servidor (host/IP) del DB/SAP según landscape.
        /// </summary>
        public string? Server { get; set; }

        /// <summary>
        /// License server (por ejemplo: "HOST:30000").
        /// </summary>
        public string? LicenseServer { get; set; }

        /// <summary>
        /// Base de datos de la compañía (CompanyDB).
        /// </summary>
        public string? CompanyDb { get; set; }

        /// <summary>
        /// Usuario SAP (B1) para DI API.
        /// </summary>
        public string? UserName { get; set; }

        /// <summary>
        /// Password SAP (B1) para DI API.
        /// </summary>
        public string? Password { get; set; }

        /// <summary>
        /// Usuario DB (si aplica; para HANA en la mayoría de landscapes no se usa).
        /// </summary>
        public string? DbUserName { get; set; }

        /// <summary>
        /// Password DB (si aplica).
        /// </summary>
        public string? DbPassword { get; set; }

        /// <summary>
        /// Tipo de servidor de datos (ej: "HANA", "MSSQL2019", etc.). Se mapea a BoDataServerTypes.
        /// </summary>
        public string? DbServerType { get; set; }

        /// <summary>
        /// Idioma (ej: "Spanish", "English"). Opcional.
        /// </summary>
        public string? Language { get; set; }

        /// <summary>
        /// Indica si se debe usar SLD (opcional; depende de configuración).
        /// </summary>
        public bool UseSld { get; set; } = false;

        /// <summary>
        /// SLD Server (opcional).
        /// </summary>
        public string? SldServer { get; set; }
    }
}

