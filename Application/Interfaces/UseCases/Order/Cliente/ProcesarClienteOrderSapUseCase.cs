using Application.Abstractions;
using Application.Commands.Items;
using Application.Commands.Items.Cliente;
using Application.Commands.Order;
using Application.Commands.Order.Cliente;
using Application.DTO;
using Application.UseCases.Items.Cliente;
using Domain.Configuration;
using Domain.Helper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Application.Interfaces.UseCases.Order.Cliente
{
    public class ProcesarClienteOrderSapUseCase : IProcesarClienteOrderSapUseCase
    {
        private readonly ICommandHandler<GetClienteOrdersToUpdateWoo, IEnumerable<SapItemQueeDTO>> _getOrdersHandler;
        //private readonly ICommandHandler<SendItemsToApiCommand, (bool IsSuccess, string? Message)> _sendToApiHandler;
        private readonly ICommandHandler<MarkStatusOrderAsCommand, bool> _markStatusOrdenAsHandler;
        private readonly ILogger<ProcesarClienteOrderSapUseCase> _logger;
        private readonly WorkerSettings _settings;

        public ProcesarClienteOrderSapUseCase(
        ICommandHandler<GetClienteOrdersToUpdateWoo, IEnumerable<SapItemQueeDTO>> getOrdersHandler,
        //ICommandHandler<SendItemsToApiCommand, (bool IsSuccess, string? Message)> sendToApiHandler,
        ICommandHandler<MarkStatusOrderAsCommand, bool> markStatusOrdenAsHandler,
        ILogger<ProcesarClienteOrderSapUseCase> logger,
        IOptions<WorkerSettings> settings)
        {
            _getOrdersHandler = getOrdersHandler;
            //_sendToApiHandler = sendToApiHandler;
            _markStatusOrdenAsHandler = markStatusOrdenAsHandler;
            _logger = logger;
            _settings = settings.Value;
        }

        public async Task<ProcesarItemsResult> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                if (_settings.processOrderWooCliente?.IsEnableFlag != 1)
                {
                    return new ProcesarItemsResult { IsSuccess = true, Message = "Proceso CLIENTE creacion de ordenes en sap y actualizacion de estado INTEGRADO en Woo deshabilitado" };
                }

                var ordersList = (await _getOrdersHandler.HandleAsync(new GetClienteOrdersToUpdateWoo())).ToList();
                if (!ordersList.Any())
                {
                    return new ProcesarItemsResult { IsSuccess = true, Message = "No hay ordenes CLIENTE pendientes", ItemsProcessed = 0 };
                }

                var sent = 0;
                var failed = 0;

                foreach (var order in ordersList)
                {
                    var jsonCompleto = order.Json;


                    order.Status = (int)StatusHanaDocumentLevel.Got;                    
                    if (!await _markStatusOrdenAsHandler.HandleAsync(new MarkStatusOrderAsCommand(order)))
                    {
                        failed++;
                        continue;
                    }

                    sent++;

                    //var sendResult = await _sendToApiHandler.HandleAsync(
                    //    new SendItemsToApiCommand(item, ItemDestinationType.Cliente));
                    //item.Status = sendResult.IsSuccess ? (int)StatusHanaDocumentLevel.Confirmed : (int)StatusHanaDocumentLevel.Error;
                    //item.Json = sendResult.Message ?? string.Empty;
                    //if (sendResult.IsSuccess && !string.IsNullOrWhiteSpace(sendResult.Message))
                    //{
                    //    var response = JsonSerializer.Deserialize<WooProductResponse>(
                    //        sendResult.Message,
                    //        new JsonSerializerOptions
                    //        {
                    //            PropertyNameCaseInsensitive = true
                    //        });

                    //    item.idWoo = response?.Id.ToString();
                    //}

                    //if (sendResult.IsSuccess) sent++; else failed++;
                    //await _markStatusItemAsHandler.HandleAsync(new MarkStatusItemAsCommand(item));
                }

                return new ProcesarItemsResult { IsSuccess = true, ItemsProcessed = ordersList.Count, ItemsSent = sent, ItemsFailed = failed, Message = $"CLIENTE: {sent} enviados, {failed} fallidos" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en proceso CLIENTE HANA -> API");
                return new ProcesarItemsResult { IsSuccess = false, ErrorMessage = ex.Message };
            }
        }
    }
}
