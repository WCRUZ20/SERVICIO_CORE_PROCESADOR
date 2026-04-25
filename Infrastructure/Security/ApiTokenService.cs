using Application.Interfaces.Security;
using Domain.Configuration;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Infrastructure.Security
{
    /// <summary>
    /// Implementación del servicio para obtener tokens JWT de la API externa
    /// </summary>
    public class ApiTokenService : IApiTokenService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ApiTokenService> _logger;
        private readonly OptionSecretsSL _secrets;
        private readonly JwtSecurityTokenHandler _tokenHandler;
        private string? _cachedToken;
        private DateTime _tokenExpiry = DateTime.MinValue;
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private readonly TimeSpan _refreshThreshold = TimeSpan.FromMinutes(5); // Margen de 5 minutos antes de expirar

        public ApiTokenService(
            IHttpClientFactory httpClientFactory,
            ILogger<ApiTokenService> logger,
            IOptions<OptionSecretsSL> secrets)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _secrets = secrets?.Value ?? throw new ArgumentNullException(nameof(secrets));
            _tokenHandler = new JwtSecurityTokenHandler();
        }

        public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
        {
            // Verificar si tenemos un token en caché y si aún es válido
            if (!string.IsNullOrEmpty(_cachedToken) && IsTokenValid(_cachedToken))
            {
                _logger.LogDebug("Usando token JWT en caché (expira en: {Expiry})", _tokenExpiry);
                return _cachedToken;
            }

            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                // Double-check locking: verificar nuevamente después de adquirir el semáforo
                if (!string.IsNullOrEmpty(_cachedToken) && IsTokenValid(_cachedToken))
                {
                    return _cachedToken;
                }

                _logger.LogInformation("Obteniendo nuevo token JWT de la API");

                // Crear HttpClient usando la factory
                using var httpClient = _httpClientFactory.CreateClient();
                
                // Configurar BaseAddress del HttpClient
                if (string.IsNullOrWhiteSpace(_secrets.ApiMiddlewareIPUrl))
                {
                    throw new InvalidOperationException(
                        "ApiIPUrl no está configurada en OptionSecretsSL");
                }

                var baseUrl = _secrets.ApiMiddlewareIPUrl.TrimEnd('/');
                if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri))
                {
                    throw new InvalidOperationException(
                        $"ApiIPUrl no es una URL válida: {_secrets.ApiMiddlewareIPUrl}");
                }

                httpClient.BaseAddress = baseUri;
                httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

                // Construir la URL del endpoint de autenticación
                var authEndpoint = _secrets.AuthEndpoint ?? "/api/auth/login";
                if (!authEndpoint.StartsWith("/"))
                {
                    authEndpoint = "/" + authEndpoint;
                }

                // Preparar el request de autenticación
                var loginRequest = new
                {
                    ClientId = _secrets.ApiClientId,
                    ClientSecret = _secrets.ApiClientSecret
                };

                var response = await httpClient.PostAsJsonAsync(
                    authEndpoint,
                    loginRequest,
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError(
                        "Error al obtener token JWT. StatusCode: {StatusCode}, Response: {Response}",
                        response.StatusCode,
                        errorContent);
                    throw new HttpRequestException(
                        $"Error al obtener token: {response.StatusCode}. {errorContent}");
                }

                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var tokenResponse = DeserializeTokenResponse(responseContent);

                if (string.IsNullOrWhiteSpace(tokenResponse?.Token))
                {
                    throw new InvalidOperationException("La API no devolvió un token válido");
                }

                _cachedToken = tokenResponse.Token;

                // Decodificar el JWT para obtener la fecha de expiración real
                _tokenExpiry = GetTokenExpiration(_cachedToken);

                _logger.LogInformation(
                    "Token JWT obtenido exitosamente. Expira en: {Expiry} (UTC)",
                    _tokenExpiry);

                return _cachedToken;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Verifica si el token es válido y no está próximo a expirar
        /// </summary>
        private bool IsTokenValid(string token)
        {
            try
            {
                if (_tokenExpiry == DateTime.MinValue)
                {
                    // Si no tenemos la fecha de expiración guardada, intentar obtenerla del token
                    _tokenExpiry = GetTokenExpiration(token);
                }

                var now = DateTime.UtcNow;
                var timeUntilExpiry = _tokenExpiry - now;

                // El token es válido si aún no ha expirado y no está próximo a expirar
                return timeUntilExpiry > _refreshThreshold;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al validar token JWT, se considerará inválido");
                return false;
            }
        }

        /// <summary>
        /// Obtiene la fecha de expiración del token JWT decodificándolo
        /// </summary>
        private DateTime GetTokenExpiration(string token)
        {
            try
            {
                // Decodificar el token sin validar la firma (solo necesitamos leer los claims)
                var jwtToken = _tokenHandler.ReadJwtToken(token);
                
                // Obtener el claim 'exp' (expiration time)
                var expClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "exp");
                
                if (expClaim != null && long.TryParse(expClaim.Value, out var expUnix))
                {
                    // Convertir Unix timestamp a DateTime UTC
                    var expiryDate = DateTimeOffset.FromUnixTimeSeconds(expUnix).UtcDateTime;
                    _logger.LogDebug(
                        "Fecha de expiración del JWT leída del token: {Expiry} (UTC)", 
                        expiryDate);
                    return expiryDate;
                }

                // Si no tiene el claim 'exp', intentar obtener del ValidTo del token
                if (jwtToken.ValidTo != DateTime.MinValue && jwtToken.ValidTo > DateTime.UtcNow)
                {
                    _logger.LogDebug(
                        "Fecha de expiración del JWT obtenida de ValidTo: {Expiry} (UTC)", 
                        jwtToken.ValidTo);
                    return jwtToken.ValidTo;
                }

                // Fallback: asumir 1 hora si no se puede determinar
                _logger.LogWarning(
                    "No se pudo determinar la fecha de expiración del token, asumiendo 1 hora");
                return DateTime.UtcNow.AddHours(1);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al decodificar JWT para obtener expiración, asumiendo 1 hora");
                return DateTime.UtcNow.AddHours(1);
            }
        }

        public Task InvalidateTokenAsync()
        {
            _cachedToken = null;
            _tokenExpiry = DateTime.MinValue;
            _logger.LogDebug("Token JWT invalidado");
            return Task.CompletedTask;
        }

        private TokenResponse? DeserializeTokenResponse(string responseContent)
        {
            if (string.IsNullOrWhiteSpace(responseContent))
            {
                return null;
            }

            try
            {
                var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(
                    responseContent,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                if (!string.IsNullOrWhiteSpace(tokenResponse?.Token))
                {
                    return tokenResponse;
                }

                using var jsonDocument = JsonDocument.Parse(responseContent);
                var root = jsonDocument.RootElement;

                var token = TryGetString(root, "access_token")
                            ?? TryGetString(root, "accessToken")
                            ?? TryGetString(root, "token");

                return string.IsNullOrWhiteSpace(token)
                    ? null
                    : new TokenResponse { Token = token };
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "No se pudo deserializar la respuesta de autenticación: {Response}", responseContent);
                return null;
            }
        }

        private static string? TryGetString(JsonElement root, string propertyName)
        {
            if (root.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String)
            {
                return value.GetString();
            }

            return null;
        }

        private class TokenResponse
        {
            [JsonPropertyName("token")]
            public string Token { get; set; } = string.Empty;

            [JsonPropertyName("expiresAt")]
            public DateTime? ExpiresAt { get; set; }

            [JsonPropertyName("tokenType")]
            public string? TokenType { get; set; }
        }

    }
}
