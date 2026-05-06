using Application.DTO;
using Application.Interfaces.HANA;
using Application.Interfaces.SAP;
using Domain.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Data;
using System.Data.Odbc;
using System.Net.Http.Json;
using System.Runtime;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Infrastructure.SAP
{
    public class SapBusinessPartnerService : ISapBusinessPartnerService
    {
        private readonly IHanaConnectionFactory _connectionFactory;
        private readonly HttpClient _httpClient;
        private readonly OptionSecretsSL _secrets;
        private readonly OdbcSettings _settings;
        private readonly ILogger<SapBusinessPartnerService> _logger;
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public SapBusinessPartnerService(
            IHanaConnectionFactory connectionFactory,
            HttpClient httpClient,
            IOptions<OptionSecretsSL> secrets,
            IOptions<OdbcSettings> settings,
            ILogger<SapBusinessPartnerService> logger)
        {
            _connectionFactory = connectionFactory;
            _httpClient = httpClient;
            _secrets = secrets.Value;
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<SapSalesOrderLookupDTO?> GetSalesOrderByWooIdAsync(string wooId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(wooId))
            {
                return null;
            }

            using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
            await connection.OpenAsync(cancellationToken);

            using var command = connection.CreateCommand();
            command.CommandText = """
                
                SELECT TOP 1 "DocEntry", "DocNum" 
                
                """ +
                $"FROM {_settings.HANA.Database}" +

                """
                ."ORDR"
                WHERE "U_ID_WOO" = ?
                ORDER BY "DocEntry" DESC
                """;
            command.Parameters.Add("U_ID_WOO", OdbcType.VarChar).Value = wooId.Trim();

            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            return new SapSalesOrderLookupDTO
            {
                DocEntry = reader["DocEntry"]?.ToString() ?? string.Empty,
                DocNum = reader["DocNum"]?.ToString() ?? string.Empty
            };
        }

        public async Task<SapBusinessPartnerLookupDTO?> GetCustomerByIdentificationAsync(string identification, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(identification))
            {
                return null;
            }

            using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
            await connection.OpenAsync(cancellationToken);

            using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT TOP 1 "CardCode", "CardName", "LicTradNum"
               
                """ +

                $"FROM {_settings.HANA.Database}" +
                
                """
                ."OCRD"
                WHERE "LicTradNum" = ?
                  AND "CardType" = 'C'
                ORDER BY "CardCode"
                """;
            command.Parameters.Add("LicTradNum", OdbcType.VarChar).Value = identification.Trim();

            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            return new SapBusinessPartnerLookupDTO
            {
                CardCode = reader["CardCode"]?.ToString() ?? string.Empty,
                CardName = reader["CardName"]?.ToString() ?? string.Empty,
                LicTradNum = reader["LicTradNum"]?.ToString() ?? string.Empty
            };
        }

        public async Task<SapCreateBusinessPartnerResult> CreateCustomerAsync(SapBusinessPartnerCreateRequest request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.CardCode) || string.IsNullOrWhiteSpace(request.Identification))
            {
                return new SapCreateBusinessPartnerResult
                {
                    IsSuccess = false,
                    CardCode = request.CardCode,
                    Message = "CardCode o identificación vacía para crear socio de negocio."
                };
            }

            var loginResult = await LoginAsync(cancellationToken);
            if (!loginResult.IsSuccess)
            {
                return new SapCreateBusinessPartnerResult
                {
                    IsSuccess = false,
                    CardCode = request.CardCode,
                    Message = loginResult.Message
                };
            }

            var payload = new
            {
                request.CardCode,
                CardName = request.CardName,
                CardType = "cCustomer",
                FederalTaxID = request.Identification,
                Phone1 = request.Phone,
                EmailAddress = request.Email,
                BPAddresses = request.Addresses.Select(address => new
                {
                    AddressName = address.AddressName,
                    AddressType = address.AddressType,
                    Street = address.Street,
                    Block = address.Block,
                    City = address.City,
                    State = address.State,
                    ZipCode = address.ZipCode,
                    Country = address.Country
                }).ToList()
            };

            var response = await _httpClient.PostAsJsonAsync(BuildServiceLayerUrl("BusinessPartners"), payload, _jsonOptions, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Socio de negocio {CardCode} creado correctamente en SAP.", request.CardCode);
                return new SapCreateBusinessPartnerResult
                {
                    IsSuccess = true,
                    CardCode = request.CardCode,
                    Message = content
                };
            }

            _logger.LogWarning("No se pudo crear socio de negocio {CardCode} en SAP. StatusCode={StatusCode}. Respuesta={Response}", request.CardCode, response.StatusCode, content);
            return new SapCreateBusinessPartnerResult
            {
                IsSuccess = false,
                CardCode = request.CardCode,
                Message = content
            };
        }

        private async Task<(bool IsSuccess, string? Message)> LoginAsync(CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(_secrets.CompanyDBSAP) || string.IsNullOrWhiteSpace(_secrets.UserSLSAP) || string.IsNullOrWhiteSpace(_secrets.PassWordSLSAP))
            {
                return (false, "Credenciales de SAP Service Layer incompletas.");
            }

            var loginPayload = new
            {
                CompanyDB = _secrets.CompanyDBSAP,
                UserName = _secrets.UserSLSAP,
                Password = _secrets.PassWordSLSAP
            };

            var response = await _httpClient.PostAsJsonAsync(BuildServiceLayerUrl(_secrets.LoginSLSAP), loginPayload, _jsonOptions, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return (true, content);
            }

            _logger.LogWarning("Login SAP Service Layer falló. StatusCode={StatusCode}. Respuesta={Response}", response.StatusCode, content);
            return (false, content);
        }

        private string BuildServiceLayerUrl(string? relativeOrAbsoluteUrl)
        {
            if (Uri.TryCreate(relativeOrAbsoluteUrl, UriKind.Absolute, out var absoluteUri))
            {
                return absoluteUri.ToString();
            }

            if (string.IsNullOrWhiteSpace(_secrets.BaseUrlSLSAP))
            {
                return relativeOrAbsoluteUrl?.TrimStart('/') ?? string.Empty;
            }

            var baseUrl = _secrets.BaseUrlSLSAP.TrimEnd('/');
            var relativeUrl = (relativeOrAbsoluteUrl ?? string.Empty).TrimStart('/');
            return $"{baseUrl}/{relativeUrl}";
        }
    }
}
