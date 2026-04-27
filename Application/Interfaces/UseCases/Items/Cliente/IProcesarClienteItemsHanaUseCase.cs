using Application.Interfaces.API;

namespace Application.Interfaces.UseCases.Items.Cliente;

public interface IProcesarClienteItemsHanaUseCase
{
    Task<ProcesarItemsResult> ExecuteAsync(CancellationToken cancellationToken = default);
}
