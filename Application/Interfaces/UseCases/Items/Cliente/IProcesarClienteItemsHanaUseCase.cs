using Application.DTO;

namespace Application.Interfaces.UseCases.Items.Cliente;

public interface IProcesarClienteItemsHanaUseCase
{
    Task<ProcesarItemsResult> ExecuteAsync(CancellationToken cancellationToken = default);
}
