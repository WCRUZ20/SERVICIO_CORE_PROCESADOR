using Application.DTO;
namespace Application.Interfaces.UseCases.Items.Dealer;

public interface IGetDealerItemsSapUseCase
{
    Task<ObtenerItemsResult> ExecuteAsync(CancellationToken cancellationToken = default);
}
