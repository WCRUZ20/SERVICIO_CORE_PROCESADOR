using Application.Abstractions;
using Application.Commands.Order;
using Application.Commands.Order.Cliente;
using Application.Commands.Order.Dealer;
using Application.DTO;
using Application.Interfaces.UseCases.Order.Cliente;
using Domain.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Application.Interfaces.UseCases.Order.Dealer
{
    public class GetDealerOrderSapUseCase : IGetDealerOrderSapUseCase
    {
        private readonly ICommandHandler<GetPendingsDealerOrdersCommand, IEnumerable<WooOrderDTO>> _getOrdersHandler;
        private readonly ILogger<GetDealerOrderSapUseCase> _logger;
        private readonly ICommandHandler<InsertOrdersCommand, bool> _insertOrderHandler;
        private readonly ICommandHandler<CheckOrderExistsCommand, bool> _checkOrderExists;
        private readonly WorkerSettings _settings;

        public GetDealerOrderSapUseCase(
            ICommandHandler<GetPendingsDealerOrdersCommand, IEnumerable<WooOrderDTO>> getOrdersHandler,
            ILogger<GetDealerOrderSapUseCase> logger,
            ICommandHandler<InsertOrdersCommand, bool> insertOrderHandler,
            ICommandHandler<CheckOrderExistsCommand, bool> checkOrderExists,
            IOptions<WorkerSettings> settings)
        {
            _getOrdersHandler = getOrdersHandler;
            _logger = logger;
            _insertOrderHandler = insertOrderHandler;
            _checkOrderExists = checkOrderExists;
            _settings = settings.Value;
        }

        public async Task<ObtenerItemsResult> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                if (_settings.getOrderWooDealer?.IsEnableFlag != 1)
                {
                    _logger.LogInformation("PROCESO ENCOLAMIENTO DEALER - ORDENES (GET) DESHABILITADO");
                    return new ObtenerItemsResult { IsSuccess = true, Message = "Proceso deshabilitado" };
                }

                var orderList = (await _getOrdersHandler.HandleAsync(new GetPendingsDealerOrdersCommand())).ToList();
                if (!orderList.Any())
                {
                    return new ObtenerItemsResult { IsSuccess = true, ItemsFound = 0, ItemsInserted = 0, Message = "No hay ordenes DEALER nuevos" };
                }

                var itemsInserted = 0;
                foreach (var order in orderList)
                {
                    var options = new JsonSerializerOptions
                    {
                        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                    };

                    var json = JsonSerializer.Serialize(order, options);

                    var orderQeue = new SapItemQueeDTO
                    {
                        idWoo = order.Id.ToString(),
                        Transaction = 4,
                        TransactionType = "1", //2 cliente
                        Json = json
                    };

                    var exists = await _checkOrderExists.HandleAsync(new CheckOrderExistsCommand(orderQeue.Transaction, orderQeue.idWoo, orderQeue.TransactionType));
                    if (exists) continue;

                    var inserted = await _insertOrderHandler.HandleAsync(new InsertOrdersCommand(orderQeue));
                    if (inserted) itemsInserted++;
                }

                return new ObtenerItemsResult
                {
                    IsSuccess = true,
                    ItemsFound = orderList.Count,
                    ItemsInserted = itemsInserted,
                    Message = $"DEALER: {itemsInserted}/{orderList.Count} insertados"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en proceso DEALER de obtención de ordenes");
                return new ObtenerItemsResult { IsSuccess = false, Message = ex.Message };
            }

        }
    }
}
