using Application.Interfaces.API;

namespace Application.Interfaces.UseCases.Items;

public interface IProcesarClienteItemsHanaUseCase
{
    Task<ProcesarItemsResult> ExecuteAsync(CancellationToken cancellationToken = default);
}
