using Application.Commands;
using Application.DTO;
using Domain.SAP;
using System.Threading.Tasks;

namespace Application.Interfaces.API
{
    /// <summary>
    /// Interfaz para cliente de API externa
    /// </summary>
    public interface IApiClient
    {
        /// <summary>
        /// Envía una transferencia de stock a la API externa
        /// </summary>
        //Task<(bool IsSuccess, string? Message)> SendDocumentAsync(
        //    SapDrivinTable document, 
        //    CancellationToken cancellationToken = default);

        /// <summary>
        /// Envía un artículo a la API externa
        /// </summary>
        Task<(bool IsSuccess, string? Message)> SendItemAsync(
            WooProductRequestDTO item,
            ItemDestinationType destinationType,
            string? bearerToken = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Envía múltiples transferencias a la API externa
        /// </summary>
        //Task<(int SuccessCount, int FailureCount, List<string> Errors)> SendStockTransfersAsync(
        //    IEnumerable<SapDrivinTable> transfers, 
        //    CancellationToken cancellationToken = default);
    }
}





