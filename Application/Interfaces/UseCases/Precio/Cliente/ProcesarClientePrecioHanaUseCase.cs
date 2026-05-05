using Application.Abstractions;
using Application.Commands.Precio;
using Application.Commands.Precio.Cliente;
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

namespace Application.Interfaces.UseCases.Precio.Cliente
{
    public class ProcesarClientePrecioHanaUseCase : IProcesarClientePrecioHanaUseCase
    {
        private readonly ICommandHandler<GetPendingClientePrecioToApiCommand, IEnumerable<SapItemQueeDTO>> _getItemsHandler;
        private readonly ICommandHandler<SendPrecioToApiCommand, (bool IsSuccess, string? Message)> _sendToApiHandler;
        private readonly ICommandHandler<MarkStatusPrecioAsCommand, bool> _markStatusItemAsHandler;
        private readonly ILogger<ProcesarClientePrecioHanaUseCase> _logger;
        private readonly WorkerSettings _settings;

        public ProcesarClientePrecioHanaUseCase(
            ICommandHandler<GetPendingClientePrecioToApiCommand, IEnumerable<SapItemQueeDTO>> getItemsHandler,
            ICommandHandler<SendPrecioToApiCommand, (bool IsSuccess, string? Message)> sendToApiHandler,
            ICommandHandler<MarkStatusPrecioAsCommand, bool> markStatusItemAsHandler,
            ILogger<ProcesarClientePrecioHanaUseCase> logger,
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
                if (_settings.processPrecioClienteSAP?.IsEnableFlag != 1)
                {
                    return new ProcesarItemsResult { IsSuccess = true, Message = "Proceso CLIENTE deshabilitado" };
                }

                var itemsList = (await _getItemsHandler.HandleAsync(new GetPendingClientePrecioToApiCommand())).ToList();
                if (!itemsList.Any())
                {
                    return new ProcesarItemsResult { IsSuccess = true, Message = "No hay articulos CLIENTE pendientes", ItemsProcessed = 0 };
                }

                var sent = 0;
                var failed = 0;
                foreach (var item in itemsList)
                {
                    item.Status = (int)StatusHanaDocumentLevel.Sent;
                    if (!await _markStatusItemAsHandler.HandleAsync(new MarkStatusPrecioAsCommand(item)))
                    {
                        failed++;
                        continue;
                    }

                    var sendResult = await _sendToApiHandler.HandleAsync(
                        new SendPrecioToApiCommand(item, ItemDestinationType.Cliente));
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
                    await _markStatusItemAsHandler.HandleAsync(new MarkStatusPrecioAsCommand(item));
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
