using Application.Interfaces.API;

namespace Application.Interfaces.UseCases.Items.Dealer;

public interface IProcesarDealerItemsHanaUseCase
{
    Task<ProcesarItemsResult> ExecuteAsync(CancellationToken cancellationToken = default);
}
