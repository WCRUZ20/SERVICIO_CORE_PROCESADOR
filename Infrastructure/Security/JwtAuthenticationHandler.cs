using Application.Interfaces.Security;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Headers;

namespace Infrastructure.Security
{
    /// <summary>
    /// Handler HTTP que inyecta automáticamente el token JWT en todas las peticiones
    /// </summary>
    public class JwtAuthenticationHandler : DelegatingHandler
    {
        private readonly IApiTokenService _tokenService;
        private readonly ILogger<JwtAuthenticationHandler> _logger;

        public JwtAuthenticationHandler(
            IApiTokenService tokenService,
            ILogger<JwtAuthenticationHandler> logger)
        {
            _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            try
            {
                if (request.Headers.Authorization == null)
                {
                    var token = await _tokenService.GetAccessTokenAsync(cancellationToken);
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener token JWT para la petición");
                throw;
            }

            var response = await base.SendAsync(request, cancellationToken);

            // Si recibimos 401, invalidar token y reintentar
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _logger.LogWarning("Token JWT rechazado (401), invalidando y reintentando...");
                await _tokenService.InvalidateTokenAsync();

                try
                {
                    var newToken = await _tokenService.GetAccessTokenAsync(cancellationToken);
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newToken);

                    // Disposing la respuesta anterior antes de reintentar
                    response.Dispose();
                    response = await base.SendAsync(request, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al obtener nuevo token después de 401");
                    throw;
                }
            }

            return response;
        }
    }
}
