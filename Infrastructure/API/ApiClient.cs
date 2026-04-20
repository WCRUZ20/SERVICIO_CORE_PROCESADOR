using Application.DTO;
using Application.Interfaces.API;
using Domain.Configuration;
using Domain.SAP;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Reflection.Metadata;
using System.Text.Json;

namespace Infrastructure.API
{
    /// <summary>
    /// Implementación del cliente de API externa para enviar documentos SAP a Drivin
    /// </summary>
    public class ApiClient : IApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ApiClient> _logger;
        private readonly OptionSecretsSL _secrets;
        private readonly JsonSerializerOptions _jsonOptions;
        private const int DefaultTimeoutSeconds = 30;

        public ApiClient(
            HttpClient httpClient,
            ILogger<ApiClient> logger,
            IOptions<OptionSecretsSL> secrets)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _secrets = secrets?.Value ?? throw new ArgumentNullException(nameof(secrets));

            // Configurar JsonSerializerOptions una sola vez
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };

            // Validar y configurar BaseAddress
            if (string.IsNullOrWhiteSpace(_secrets.ApiMiddlewareIPUrl))
            {
                throw new InvalidOperationException(
                    "ApiBaseURL no está configurada en OptionSecretsSL");
            }

            var baseUrl = _secrets.ApiMiddlewareIPUrl.TrimEnd('/');
            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri))
            {
                throw new InvalidOperationException(
                    $"ApiBaseURL no es una URL válida: {_secrets.ApiMiddlewareIPUrl}");
            }

            _httpClient.BaseAddress = baseUri;
            
            // NO establecer Content-Type aquí - PostAsJsonAsync lo hace automáticamente
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            
            // Configurar timeout
            _httpClient.Timeout = TimeSpan.FromSeconds(DefaultTimeoutSeconds);

            _logger.LogInformation(
                "ApiClient inicializado con BaseAddress: {BaseAddress}",
                baseUri);
        }

        /// <summary>
        /// Envía un documento SAP a la API externa
        /// </summary>
        public async Task<(bool IsSuccess, string? Message)> SendDocumentAsync(
            SapDrivinTable document,
            CancellationToken cancellationToken = default)
        {
            // Validar entrada
            if (document == null)
            {
                const string error = "El documento no puede ser null";
                _logger.LogWarning(error);
                return (false, error);
            }

            if (document.SapDocEntry <= 0)
            {
                var error = $"SapDocEntry inválido: {document.SapDocEntry}";
                _logger.LogWarning(error);
                return (false, error);
            }

            // Validar endpoint
            if (string.IsNullOrWhiteSpace(_secrets.ProcesarDocumentoEndPoint))
            {
                const string error = "GetSapDrivinTableEndPoint no está configurado";
                _logger.LogError(error);
                return (false, error);
            }

            var endpoint = _secrets.ProcesarDocumentoEndPoint.TrimStart('/');
            
            try
            {
                _logger.LogInformation(
                    "Enviando documento SapDocEntry={SapDocEntry}, SapDocNum={SapDocNum} al endpoint {Endpoint}",
                    document.SapDocEntry,
                    document.SapDocNum,
                    endpoint);
                var json = JsonSerializer.Serialize(document, _jsonOptions);
                using var response = await _httpClient.PostAsJsonAsync(
                    endpoint,
                    document,
                    //_jsonOptions,
                    cancellationToken);

                return await ProcessResponseAsync(
                    response,
                    endpoint,
                    $"SapDocEntry={document.SapDocEntry}",
                    cancellationToken);
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                var errorMessage = $"Timeout al enviar documento SapDocEntry={document.SapDocEntry}";
                _logger.LogError(ex, $"{errorMessage}. Timeout configurado: {DefaultTimeoutSeconds} segundos");
                return (false, errorMessage);
            }
            catch (TaskCanceledException ex)
            {
                var errorMessage = $"Operación cancelada al enviar documento SapDocEntry={document.SapDocEntry}";
                _logger.LogWarning(ex, errorMessage);
                return (false, errorMessage);
            }
            catch (HttpRequestException ex)
            {
                var errorMessage = $"Error de comunicación con la API: {ex.Message}";
                _logger.LogError(ex, $"{errorMessage} para documento SapDocEntry={document.SapDocEntry}");
                return (false, errorMessage);
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error inesperado: {ex.Message}";
                _logger.LogError( ex,$"{errorMessage} al enviar documento SapDocEntry={document.SapDocEntry}");
                return (false, errorMessage);
            }
        }

        public async Task<(bool IsSuccess, string? Message)> SendItemAsync(
            WooProductRequestDTO item,
            CancellationToken cancellationToken = default)
        {
            if (item == null)
            {
                const string error = "El articulo no puede ser null";
                _logger.LogWarning(error);
                return (false, error);
            }

            if (string.IsNullOrWhiteSpace(item.Name))
            {
                var error = "Name inválido para el payload de artículo";
                _logger.LogWarning(error);
                return (false, error);
            }

            var endpoint = _secrets.ProcesarArticuloEndPoint;

            if (string.IsNullOrWhiteSpace(endpoint))
            {
                const string error = "No hay endpoint configurado para articulos";
                _logger.LogError(error);
                return (false, error);
            }

            endpoint = endpoint.TrimStart('/');

            try
            {
                _logger.LogInformation(
                    "Enviando payload de artículo SKU={Sku}, Name={Name} al endpoint {Endpoint}",
                    item.SKU,
                    item.Name,
                    endpoint);

                using var response = await _httpClient.PostAsJsonAsync(
                    endpoint,
                    item,
                    cancellationToken);

                return await ProcessResponseAsync(
                    response,
                    endpoint,
                    $"SKU={item.SKU}",
                    cancellationToken);
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                var errorMessage = $"Timeout al enviar articulo SKU={item.SKU}";
                _logger.LogError(ex, $"{errorMessage}. Timeout configurado: {DefaultTimeoutSeconds} segundos");
                return (false, errorMessage);
            }
            catch (TaskCanceledException ex)
            {
                var errorMessage = $"Operación cancelada al enviar articulo SKU={item.SKU}";
                _logger.LogWarning(ex, errorMessage);
                return (false, errorMessage);
            }
            catch (HttpRequestException ex)
            {
                var errorMessage = $"Error de comunicación con la API: {ex.Message}";
                _logger.LogError(ex, $"{errorMessage} para articulo SKU={item.SKU}");
                return (false, errorMessage);
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error inesperado: {ex.Message}";
                _logger.LogError(ex, $"{errorMessage} al enviar articulo SKU={item.SKU}");
                return (false, errorMessage);
            }
        }


        /// <summary>
        /// Procesa la respuesta HTTP y determina si fue exitosa
        /// </summary>
        private async Task<(bool IsSuccess, string? Message)> ProcessResponseAsync(
            HttpResponseMessage response,
            string endpoint,
            string entityRef,
            CancellationToken cancellationToken)
        {
            var statusCode = response.StatusCode;
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "Documento SapDocEntry={SapDocEntry} enviado exitosamente. " +
                    "StatusCode={StatusCode}, Response={Response}",
                    entityRef,
                    statusCode,
                    content);

                return (true, content);
            }

            // Manejo específico por código HTTP
            var errorMessage = statusCode switch
            {
                HttpStatusCode.BadRequest => 
                    $"Solicitud inválida (400): {ExtractErrorMessage(content)}",
                
                HttpStatusCode.Unauthorized => 
                    "No autorizado (401). Verificar credenciales de la API",
                
                HttpStatusCode.Forbidden => 
                    "Acceso prohibido (403). Verificar permisos",
                
                HttpStatusCode.NotFound =>
                    $"Endpoint no encontrado (404): {endpoint}",

                HttpStatusCode.Conflict =>
                    $"Conflicto (409): El registro {entityRef} ya existe. {ExtractErrorMessage(content)}",

                HttpStatusCode.UnprocessableEntity => 
                    $"Entidad no procesable (422): {ExtractErrorMessage(content)}",
                
                HttpStatusCode.TooManyRequests => 
                    "Demasiadas solicitudes (429). El API está limitando la velocidad",
                
                HttpStatusCode.InternalServerError => 
                    FormatInternalServerError(content),
                
                HttpStatusCode.BadGateway => 
                    "Bad Gateway (502). El servidor API no está disponible",
                
                HttpStatusCode.ServiceUnavailable => 
                    "Servicio no disponible (503). El API está temporalmente fuera de servicio",
                
                _ => $"Error HTTP {statusCode}: {ExtractErrorMessage(content)}"
            };

            _logger.LogWarning(
                "Error al enviar documento SapDocEntry={SapDocEntry} al endpoint {Endpoint}. " +
                "StatusCode={StatusCode}, Error={ErrorMessage}",
                entityRef,
                endpoint,
                statusCode,
                errorMessage);

            return (false, errorMessage);
        }

        /// <summary>
        /// Formatea el mensaje de error 500 de manera más clara
        /// </summary>
        private string FormatInternalServerError(string content)
        {
            var errorMessage = ExtractErrorMessage(content);
            
            // Detectar errores comunes de configuración del servidor
            if (errorMessage.Contains("Unable to resolve service", StringComparison.OrdinalIgnoreCase) ||
                errorMessage.Contains("InvalidOperationException", StringComparison.OrdinalIgnoreCase))
            {
                return $"Error de configuración del servidor API (500): " +
                       $"El servidor no puede resolver sus dependencias. " +
                       $"Verificar la configuración de Dependency Injection en el servidor. " +
                       $"Detalle: {errorMessage}";
            }

            return $"Error interno del servidor (500): {errorMessage}";
        }

        /// <summary>
        /// Extrae el mensaje de error más relevante del contenido de la respuesta
        /// </summary>
        private string ExtractErrorMessage(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return "Sin detalles del error";

            // Intentar extraer solo la primera línea del error si es muy largo
            var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length > 0)
            {
                var firstLine = lines[0].Trim();
                // Limitar la longitud del mensaje para evitar logs muy largos
                if (firstLine.Length > 500)
                {
                    return firstLine.Substring(0, 500) + "...";
                }
                return firstLine;
            }

            // Si no hay líneas, devolver el contenido completo (limitado)
            if (content.Length > 500)
            {
                return content.Substring(0, 500) + "...";
            }

            return content;
        }

       
    }
}

