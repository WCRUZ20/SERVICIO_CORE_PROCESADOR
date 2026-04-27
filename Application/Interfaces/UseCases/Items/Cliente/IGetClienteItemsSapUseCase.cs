using Application.UseCases.HANA;

namespace Application.Interfaces.UseCases.Items.Cliente;

public interface IGetClienteItemsSapUseCase
{
    Task<ObtenerItemsResult> ExecuteAsync(CancellationToken cancellationToken = default);
}
