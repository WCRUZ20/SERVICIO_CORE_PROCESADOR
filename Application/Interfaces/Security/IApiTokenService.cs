namespace Application.Interfaces.Security
{
    /// <summary>
    /// Servicio para obtener y gestionar tokens JWT de la API externa
    /// </summary>
    public interface IApiTokenService
    {
        /// <summary>
        /// Obtiene un token de acceso de la API. Usa caché si el token aún es válido.
        /// </summary>
        Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Invalida el token en caché, forzando la obtención de uno nuevo en la próxima petición.
        /// </summary>
        Task InvalidateTokenAsync();
    }
}
