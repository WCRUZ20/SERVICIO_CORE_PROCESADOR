using Application.UseCases.HANA;

namespace Application.Interfaces.UseCases.Items;

public interface IGetDealerItemsSapUseCase
{
    Task<ObtenerItemsResult> ExecuteAsync(CancellationToken cancellationToken = default);
}
