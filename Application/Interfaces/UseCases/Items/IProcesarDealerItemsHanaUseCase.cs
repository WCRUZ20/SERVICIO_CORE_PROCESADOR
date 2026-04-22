using Application.Interfaces.API;

namespace Application.Interfaces.UseCases.Items;

public interface IProcesarDealerItemsHanaUseCase
{
    Task<ProcesarItemsResult> ExecuteAsync(CancellationToken cancellationToken = default);
}
