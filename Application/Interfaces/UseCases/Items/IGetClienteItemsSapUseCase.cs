using Application.UseCases.HANA;

namespace Application.Interfaces.UseCases.Items;

public interface IGetClienteItemsSapUseCase
{
    Task<ObtenerItemsResult> ExecuteAsync(CancellationToken cancellationToken = default);
}
