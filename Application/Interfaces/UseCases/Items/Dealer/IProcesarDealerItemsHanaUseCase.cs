using Application.DTO;

namespace Application.Interfaces.UseCases.Items.Dealer;

public interface IProcesarDealerItemsHanaUseCase
{
    Task<ProcesarItemsResult> ExecuteAsync(CancellationToken cancellationToken = default);
}
