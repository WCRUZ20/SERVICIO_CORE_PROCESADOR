using Application.Abstractions;
using Application.Commands;
using Application.Commands.Cliente;
using Application.DTO;
using Application.Interfaces;
using Application.Interfaces.UseCases.Items;
using Application.UseCases.HANA;
using Domain.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Application.UseCases.Items.Cliente;

public class GetClienteItemsSapUseCase : IGetClienteItemsSapUseCase
{
    private readonly ICommandHandler<GetPendingClienteItemsTypeCommand, IEnumerable<SapItemQueeDTO>> _getItemsHandler;
    private readonly ICommandHandler<CheckItemExistsCommand, bool> _checkExistsHandler;
    private readonly ICommandHandler<InsertItemsCommand, bool> _insertItemHandler;
    private readonly ILogger<GetClienteItemsSapUseCase> _logger;
    private readonly WorkerSettings _settings;

    public GetClienteItemsSapUseCase(
        ICommandHandler<GetPendingClienteItemsTypeCommand, IEnumerable<SapItemQueeDTO>> getItemsHandler,
        ICommandHandler<CheckItemExistsCommand, bool> checkExistsHandler,
        ICommandHandler<InsertItemsCommand, bool> insertItemHandler,
        ILogger<GetClienteItemsSapUseCase> logger,
        IOptions<WorkerSettings> settings)
    {
        _getItemsHandler = getItemsHandler;
        _checkExistsHandler = checkExistsHandler;
        _insertItemHandler = insertItemHandler;
        _logger = logger;
        _settings = settings.Value;
    }

    public async Task<ObtenerItemsResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (_settings.getDocumentSAP?.IsEnableFlag != 1)
            {
                _logger.LogInformation("Proceso CLIENTE de obtención de items deshabilitado");
                return new ObtenerItemsResult { IsSuccess = true, Message = "Proceso deshabilitado" };
            }

            var itemsList = (await _getItemsHandler.HandleAsync(new GetPendingClienteItemsTypeCommand())).ToList();
            if (!itemsList.Any())
            {
                return new ObtenerItemsResult { IsSuccess = true, ItemsFound = 0, ItemsInserted = 0, Message = "No hay articulos CLIENTE nuevos" };
            }

            var itemsInserted = 0;
            foreach (var item in itemsList)
            {
                var exists = await _checkExistsHandler.HandleAsync(new CheckItemExistsCommand(item.Transaction, item.SapDocEntry));
                if (exists) continue;

                var inserted = await _insertItemHandler.HandleAsync(new InsertItemsCommand(item));
                if (inserted) itemsInserted++;
            }

            return new ObtenerItemsResult
            {
                IsSuccess = true,
                ItemsFound = itemsList.Count,
                ItemsInserted = itemsInserted,
                Message = $"CLIENTE: {itemsInserted}/{itemsList.Count} insertados"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en proceso CLIENTE de obtención de items");
            return new ObtenerItemsResult { IsSuccess = false, Message = ex.Message };
        }
    }
}
