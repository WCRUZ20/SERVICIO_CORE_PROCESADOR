using Application.DTO;

namespace Application.Interfaces.UseCases.Items.Cliente;

public interface IGetClienteItemsSapUseCase
{
    Task<ObtenerItemsResult> ExecuteAsync(CancellationToken cancellationToken = default);
}
