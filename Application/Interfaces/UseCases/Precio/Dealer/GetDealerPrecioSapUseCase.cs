using Application.Abstractions;
using Application.Commands.Items;
using Application.Commands.Precio;
using Application.Commands.Precio.Cliente;
using Application.Commands.Precio.Dealer;
using Application.DTO;
using Application.Interfaces.UseCases.Precio.Cliente;
using Domain.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.UseCases.Precio.Dealer
{
    public class GetDealerPrecioSapUseCase : IGetDealerPrecioSapUseCase
    {
        private readonly ICommandHandler<GetPendingDealerPrecioTypeCommand, IEnumerable<SapItemQueeDTO>> _getItemsHandler;
        private readonly ICommandHandler<CheckItemExistsCommand, bool> _checkExistsHandler;
        private readonly ICommandHandler<InsertPrecioCommand, bool> _insertItemHandler;
        private readonly ILogger<GetDealerPrecioSapUseCase> _logger;
        private readonly WorkerSettings _settings;

        public GetDealerPrecioSapUseCase(
            ICommandHandler<GetPendingDealerPrecioTypeCommand, IEnumerable<SapItemQueeDTO>> getItemsHandler,
            ICommandHandler<CheckItemExistsCommand, bool> checkExistsHandler,
            ICommandHandler<InsertPrecioCommand, bool> insertItemHandler,
            ILogger<GetDealerPrecioSapUseCase> logger,
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
                if (_settings.getPrecioDealerSAP?.IsEnableFlag != 1)
                {
                    _logger.LogInformation("Proceso CLIENTE de obtención de items deshabilitado");
                    return new ObtenerItemsResult { IsSuccess = true, Message = "Proceso deshabilitado" };
                }

                var itemsList = (await _getItemsHandler.HandleAsync(new GetPendingDealerPrecioTypeCommand())).ToList();
                if (!itemsList.Any())
                {
                    return new ObtenerItemsResult { IsSuccess = true, ItemsFound = 0, ItemsInserted = 0, Message = "No hay articulos CLIENTE nuevos" };
                }

                var itemsInserted = 0;
                foreach (var item in itemsList)
                {
                    //var exists = await _checkExistsHandler.HandleAsync(new CheckItemExistsCommand(item.Transaction, item.SapDocEntry.ToString(), item.Bodega));
                    //if (exists) continue;

                    var inserted = await _insertItemHandler.HandleAsync(new InsertPrecioCommand(item));
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
}
