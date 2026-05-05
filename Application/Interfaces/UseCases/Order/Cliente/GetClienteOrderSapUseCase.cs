using Application.Abstractions;
using Application.Commands.Items;
using Application.Commands.Items.Cliente;
using Application.Commands.Order;
using Application.Commands.Order.Cliente;
using Application.DTO;
using Application.UseCases.Items.Cliente;
using Domain.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Application.Interfaces.UseCases.Order.Cliente
{
    public class GetClienteOrderSapUseCase:IGetClienteOrderSapUseCase
    {
        private readonly ICommandHandler<GetPendingsClienteOrdersCommand, IEnumerable<WooOrderDTO>> _getOrdersHandler;        
        private readonly ILogger<GetClienteOrderSapUseCase> _logger;
        private readonly ICommandHandler<InsertOrdersCommand, bool> _insertOrderHandler;
        private readonly ICommandHandler<CheckOrderExistsCommand, bool> _checkOrderExists;
        private readonly WorkerSettings _settings;

        public GetClienteOrderSapUseCase(
            ICommandHandler<GetPendingsClienteOrdersCommand, IEnumerable<WooOrderDTO>> getOrdersHandler,
            ILogger<GetClienteOrderSapUseCase> logger,
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
                if (_settings.getOrderWooCliente?.IsEnableFlag != 1)
                {
                    _logger.LogInformation("PROCESO ENCOLAMIENTO CLIENTE - ORDENES (GET) DESHABILITADO");
                    return new ObtenerItemsResult { IsSuccess = true, Message = "Proceso deshabilitado" };
                }

                var orderList = (await _getOrdersHandler.HandleAsync(new GetPendingsClienteOrdersCommand())).ToList();
                if (!orderList.Any())
                {
                    return new ObtenerItemsResult { IsSuccess = true, ItemsFound = 0, ItemsInserted = 0, Message = "No hay ordenes CLIENTE nuevos" };
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
                        TransactionType = "2", //2 cliente
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
                    Message = $"CLIENTE: {itemsInserted}/{orderList.Count} insertados"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en proceso CLIENTE de obtención de ordenes");
                return new ObtenerItemsResult { IsSuccess = false, Message = ex.Message };
            }
                        
        }
    }
}
