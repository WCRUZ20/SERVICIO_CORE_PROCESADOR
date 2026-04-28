using Application.Abstractions;
using Application.Commands.Items;
using Application.Commands.Items.Cliente;
using Application.DTO;
using Application.Interfaces.UseCases.Items.Cliente;
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

namespace Application.Interfaces.UseCases.Stock.Cliente
{
    internal class ProcesarClienteStockHanaUseCase : IProcesarClienteItemsHanaUseCase
    {
        private readonly ICommandHandler<GetPendingClienteItemsToApiCommand, IEnumerable<SapItemQueeDTO>> _getItemsHandler;
        private readonly ICommandHandler<SendItemsToApiCommand, (bool IsSuccess, string? Message)> _sendToApiHandler;
        private readonly ICommandHandler<MarkStatusItemAsCommand, bool> _markStatusItemAsHandler;
        private readonly ILogger<ProcesarClienteItemsHanaUseCase> _logger;
        private readonly WorkerSettings _settings;

        public ProcesarClienteStockHanaUseCase(
            ICommandHandler<GetPendingClienteItemsToApiCommand, IEnumerable<SapItemQueeDTO>> getItemsHandler,
            ICommandHandler<SendItemsToApiCommand, (bool IsSuccess, string? Message)> sendToApiHandler,
            ICommandHandler<MarkStatusItemAsCommand, bool> markStatusItemAsHandler,
            ILogger<ProcesarClienteItemsHanaUseCase> logger,
            IOptions<WorkerSettings> settings)
        {
            _getItemsHandler = getItemsHandler;
            _sendToApiHandler = sendToApiHandler;
            _markStatusItemAsHandler = markStatusItemAsHandler;
            _logger = logger;
            _settings = settings.Value;
        }

        public async Task<ProcesarItemsResult> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                if (_settings.processItemClienteSAP?.IsEnableFlag != 1)
                {
                    return new ProcesarItemsResult { IsSuccess = true, Message = "Proceso CLIENTE deshabilitado" };
                }

                var itemsList = (await _getItemsHandler.HandleAsync(new GetPendingClienteItemsToApiCommand())).ToList();
                if (!itemsList.Any())
                {
                    return new ProcesarItemsResult { IsSuccess = true, Message = "No hay articulos CLIENTE pendientes", ItemsProcessed = 0 };
                }

                var sent = 0;
                var failed = 0;
                foreach (var item in itemsList)
                {
                    item.Status = (int)StatusHanaDocumentLevel.Sent;
                    if (!await _markStatusItemAsHandler.HandleAsync(new MarkStatusItemAsCommand(item)))
                    {
                        failed++;
                        continue;
                    }

                    var sendResult = await _sendToApiHandler.HandleAsync(
                        new SendItemsToApiCommand(item, ItemDestinationType.Cliente));
                    item.Status = sendResult.IsSuccess ? (int)StatusHanaDocumentLevel.Confirmed : (int)StatusHanaDocumentLevel.Error;
                    item.Json = sendResult.Message ?? string.Empty;
                    if (sendResult.IsSuccess && !string.IsNullOrWhiteSpace(sendResult.Message))
                    {
                        var response = JsonSerializer.Deserialize<WooProductResponse>(
                            sendResult.Message,
                            new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });

                        item.idWoo = response?.Id.ToString();
                    }

                    if (sendResult.IsSuccess) sent++; else failed++;
                    await _markStatusItemAsHandler.HandleAsync(new MarkStatusItemAsCommand(item));
                }

                return new ProcesarItemsResult { IsSuccess = true, ItemsProcessed = itemsList.Count, ItemsSent = sent, ItemsFailed = failed, Message = $"CLIENTE: {sent} enviados, {failed} fallidos" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en proceso CLIENTE HANA -> API");
                return new ProcesarItemsResult { IsSuccess = false, ErrorMessage = ex.Message };
            }
        }
    }
    {
    }
}
