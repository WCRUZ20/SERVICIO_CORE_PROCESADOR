using Application.Abstractions;
using Application.Commands;
using Application.Commands.Dealer;
using Application.DTO;
using Application.Interfaces.UseCases.Items.Dealer;
using Application.UseCases.HANA;
using Domain.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Application.UseCases.Items.Dealer;

public class GetDealerItemsSapUseCase : IGetDealerItemsSapUseCase
{
    private readonly ICommandHandler<GetPendingDealerItemsTypeCommand, IEnumerable<SapItemQueeDTO>> _getItemsHandler;
    private readonly ICommandHandler<CheckItemExistsCommand, bool> _checkExistsHandler;
    private readonly ICommandHandler<InsertItemsCommand, bool> _insertItemHandler;
    private readonly ILogger<GetDealerItemsSapUseCase> _logger;
    private readonly WorkerSettings _settings;

    public GetDealerItemsSapUseCase(
        ICommandHandler<GetPendingDealerItemsTypeCommand, IEnumerable<SapItemQueeDTO>> getItemsHandler,
        ICommandHandler<CheckItemExistsCommand, bool> checkExistsHandler,
        ICommandHandler<InsertItemsCommand, bool> insertItemHandler,
        ILogger<GetDealerItemsSapUseCase> logger,
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
            if (_settings.getItemDealerSAP?.IsEnableFlag != 1)
            {
                _logger.LogInformation("Proceso DEALER de obtención de items deshabilitado");
                return new ObtenerItemsResult { IsSuccess = true, Message = "Proceso deshabilitado" };
            }

            var itemsList = (await _getItemsHandler.HandleAsync(new GetPendingDealerItemsTypeCommand())).ToList();
            if (!itemsList.Any())
            {
                return new ObtenerItemsResult { IsSuccess = true, ItemsFound = 0, ItemsInserted = 0, Message = "No hay articulos DEALER nuevos" };
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
                Message = $"DEALER: {itemsInserted}/{itemsList.Count} insertados"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en proceso DEALER de obtención de items");
            return new ObtenerItemsResult { IsSuccess = false, Message = ex.Message };
        }
    }
}
