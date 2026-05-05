using Application.Abstractions;
using Application.Commands.Stock;
using Application.Commands.Stock.Cliente;
using Application.DTO;
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
    public class ProcesarClienteStockHanaUseCase : IProcesarClienteStockHanaUseCase
    {
        private readonly ICommandHandler<GetPendingClienteStockToApiCommand, IEnumerable<SapItemQueeDTO>> _getItemsHandler;
        private readonly ICommandHandler<SendStockToApiCommand, (bool IsSuccess, string? Message)> _sendToApiHandler;
        private readonly ICommandHandler<MarkStatusStockAsCommand, bool> _markStatusItemAsHandler;
        private readonly ILogger<ProcesarClienteStockHanaUseCase> _logger;
        private readonly WorkerSettings _settings;

        public ProcesarClienteStockHanaUseCase(
            ICommandHandler<GetPendingClienteStockToApiCommand, IEnumerable<SapItemQueeDTO>> getItemsHandler,
            ICommandHandler<SendStockToApiCommand, (bool IsSuccess, string? Message)> sendToApiHandler,
            ICommandHandler<MarkStatusStockAsCommand, bool> markStatusItemAsHandler,
            ILogger<ProcesarClienteStockHanaUseCase> logger,
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
                if (_settings.processStockClienteSAP?.IsEnableFlag != 1)
                {
                    return new ProcesarItemsResult { IsSuccess = true, Message = "Proceso CLIENTE deshabilitado" };
                }

                var itemsList = (await _getItemsHandler.HandleAsync(new GetPendingClienteStockToApiCommand())).ToList();
                if (!itemsList.Any())
                {
                    return new ProcesarItemsResult { IsSuccess = true, Message = "No hay articulos CLIENTE pendientes", ItemsProcessed = 0 };
                }

                var sent = 0;
                var failed = 0;
                foreach (var item in itemsList)
                {
                    item.Status = (int)StatusHanaDocumentLevel.Sent;
                    if (!await _markStatusItemAsHandler.HandleAsync(new MarkStatusStockAsCommand(item)))
                    {
                        failed++;
                        continue;
                    }

                    var sendResult = await _sendToApiHandler.HandleAsync(
                        new SendStockToApiCommand(item, ItemDestinationType.Cliente));
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
                    await _markStatusItemAsHandler.HandleAsync(new MarkStatusStockAsCommand(item));
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
}
