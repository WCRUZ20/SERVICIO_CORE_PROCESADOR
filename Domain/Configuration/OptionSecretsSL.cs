namespace Domain.Configuration
{
    public class OptionSecretsSL
    {
        //public string CompanyDBSAP { get; set; } = string.Empty;
        //public string UserSLSAP { get; set; } = string.Empty;
        //public string PassWordSLSAP { get; set; } = string.Empty;
        //public string BaseUrlSLSAP { get; set; } = string.Empty;
        //public string LoginSLSAP { get; set; } = string.Empty;
        //public string TransferEndPointSLSAP { get; set; } = string.Empty;


        public string ApiMiddlewareIPUrl { get; set; } = string.Empty;
        public string ProcesarDocumentoEndPoint { get; set; } = string.Empty;
        public string? ProcesarArticuloClienteEndPoint { get; set; }
        public string? ProcesarArticuloDealerEndPoint { get; set; }

        public string? ProcesarStockClienteEndPoint { get; set; }
        public string? ProcesarStockDealerEndPoint { get; set; }

        public string? ProcesarPrecioClienteEndPoint { get; set; }
        public string? ProcesarPrecioDealerEndPoint { get; set; }

        public string? GetOrdenesClienteEndPoint { get; set; }
        public string? GetOrdenesDealerEndPoint { get; set; }

        /// <summary>
        /// Endpoint de autenticación para obtener tokens JWT (ej: "/api/auth/login")
        /// </summary>
        public string? AuthEndpoint { get; set; }
        
        /// <summary>
        /// Client ID para autenticación con la API (opcional)
        /// </summary>
        public string? ApiClientId { get; set; }
        
        /// <summary>
        /// Client Secret para autenticación con la API (opcional, puede estar encriptado)
        /// </summary>
        public string? ApiClientSecret { get; set; }
    }
}

