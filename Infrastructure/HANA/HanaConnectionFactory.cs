using Application.Interfaces.HANA;
using Domain.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Data.Odbc;

namespace Infrastructure.HANA
{
    /// <summary>
    /// Factory para crear y gestionar conexiones a HANA mediante ODBC
    /// </summary>
    public class HanaConnectionFactory : IHanaConnectionFactory
    {
        private readonly OdbcSettings _settings;
        private readonly ILogger<HanaConnectionFactory> _logger;
        private readonly string _connectionString;

        public HanaConnectionFactory(
            IOptions<OdbcSettings> settings,
            ILogger<HanaConnectionFactory> logger)
        {
            _settings = settings.Value;
            _logger = logger;
            _connectionString = BuildConnectionString();
        }

        /// <summary>
        /// Crea una nueva conexión a HANA
        /// </summary>
        public Task<OdbcConnection> CreateConnectionAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var connection = new OdbcConnection(_connectionString);
                return Task.FromResult(connection);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear conexión a HANA");
                throw;
            }
        }

        /// <summary>
        /// Valida que la conexión a HANA esté disponible
        /// </summary>
        public async Task<bool> ValidateConnectionAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                using var connection = await CreateConnectionAsync(cancellationToken);
                await connection.OpenAsync(cancellationToken);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al validar conexión a HANA");
                return false;
            }
        }

        /// <summary>
        /// Obtiene el connection string configurado
        /// </summary>
        public string GetConnectionString()
        {
            return _connectionString;
        }

        /// <summary>
        /// Construye el connection string a partir de la configuración
        /// </summary>
        private string BuildConnectionString()
        {
            var hanaConfig = _settings.HANA;
            if (hanaConfig == null)
                throw new InvalidOperationException("La configuración de HANA no está disponible");

            // Si hay un connection string completo, usarlo directamente
            if (!string.IsNullOrWhiteSpace(hanaConfig.ConnectionString))
                return hanaConfig.ConnectionString;

            // Construir connection string desde componentes individuales
            var parts = new List<string>();

            if (!string.IsNullOrWhiteSpace(hanaConfig.Driver))
                parts.Add($"Driver={hanaConfig.Driver}");

            if (!string.IsNullOrWhiteSpace(hanaConfig.Server))
                parts.Add($"SERVERNODE={hanaConfig.Server}");

            if (!string.IsNullOrWhiteSpace(hanaConfig.UserId))
                parts.Add($"UID={hanaConfig.UserId}");

            if (!string.IsNullOrWhiteSpace(hanaConfig.Password))
                parts.Add($"PWD={hanaConfig.Password}");

            if (!string.IsNullOrWhiteSpace(hanaConfig.Database))
                parts.Add($"Database={hanaConfig.Database}");

            if (parts.Count == 0)
                throw new InvalidOperationException("No se pudo construir el connection string de HANA: configuración incompleta");

            return string.Join(";", parts);
        }
    }
}




