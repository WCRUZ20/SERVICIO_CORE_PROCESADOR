using Application.Abstractions;
using Application.Commands.Items;
using Application.Commands.Stock.Cliente;
using Application.DTO;
using Application.Interfaces.UseCases.Stock.Cliente;
using Domain.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.UseCases.Stock.Dealer
{
    public class GetDealerStockSapUseCase : IGetDealerStockSapUseCase
    {
        private readonly ICommandHandler<GetPendingDealerStockTypeCommand, IEnumerable<SapItemQueeDTO>> _getItemsHandler;
        private readonly ICommandHandler<CheckItemExistsCommand, bool> _checkExistsHandler;
        private readonly ICommandHandler<InsertStockCommand, bool> _insertStockHandler;
        private readonly ILogger<GetDealerStockSapUseCase> _logger;
        private readonly WorkerSettings _settings;

        public GetDealerStockSapUseCase(
            ICommandHandler<GetPendingDealerStockTypeCommand, IEnumerable<SapItemQueeDTO>> getItemsHandler,
            ICommandHandler<CheckItemExistsCommand, bool> checkExistsHandler,
            ICommandHandler<InsertStockCommand, bool> insertItemHandler,
            ILogger<GetDealerStockSapUseCase> logger,
            IOptions<WorkerSettings> settings)
        {
            _getItemsHandler = getItemsHandler;
            _checkExistsHandler = checkExistsHandler;
            _insertStockHandler = insertItemHandler;
            _logger = logger;
            _settings = settings.Value;
        }

        public async Task<ObtenerItemsResult> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                if (_settings.getStockDealerSAP?.IsEnableFlag != 1)
                {
                    _logger.LogInformation("Proceso DEALER de obtención de items deshabilitado");
                    return new ObtenerItemsResult { IsSuccess = true, Message = "Proceso deshabilitado" };
                }

                var itemsList = (await _getItemsHandler.HandleAsync(new GetPendingDealerStockTypeCommand())).ToList();
                if (!itemsList.Any())
                {
                    return new ObtenerItemsResult { IsSuccess = true, ItemsFound = 0, ItemsInserted = 0, Message = "No hay articulos DEALER nuevos" };
                }

                var itemsInserted = 0;
                foreach (var item in itemsList)
                {
                    //var exists = await _checkExistsHandler.HandleAsync(new CheckItemExistsCommand(item.Transaction, item.SapDocEntry));
                    //if (exists) continue;

                    var inserted = await _insertStockHandler.HandleAsync(new InsertStockCommand(item));
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
}
