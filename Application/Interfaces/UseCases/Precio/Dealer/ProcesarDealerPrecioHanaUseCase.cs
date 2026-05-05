using Application.Abstractions;
using Application.Commands.Precio;
using Application.Commands.Precio.Cliente;
using Application.Commands.Precio.Dealer;
using Application.DTO;
using Application.Interfaces.UseCases.Precio.Cliente;
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

namespace Application.Interfaces.UseCases.Precio.Dealer
{
    public class ProcesarDealerPrecioHanaUseCase : IProcesarDealerPrecioHanaUseCase
    {
        private readonly ICommandHandler<GetPendingDealerPrecioToApiCommand, IEnumerable<SapItemQueeDTO>> _getItemsHandler;
        private readonly ICommandHandler<SendPrecioToApiCommand, (bool IsSuccess, string? Message)> _sendToApiHandler;
        private readonly ICommandHandler<MarkStatusPrecioAsCommand, bool> _markStatusItemAsHandler;
        private readonly ILogger<ProcesarDealerPrecioHanaUseCase> _logger;
        private readonly WorkerSettings _settings;

        public ProcesarDealerPrecioHanaUseCase(
            ICommandHandler<GetPendingDealerPrecioToApiCommand, IEnumerable<SapItemQueeDTO>> getItemsHandler,
            ICommandHandler<SendPrecioToApiCommand, (bool IsSuccess, string? Message)> sendToApiHandler,
            ICommandHandler<MarkStatusPrecioAsCommand, bool> markStatusItemAsHandler,
            ILogger<ProcesarDealerPrecioHanaUseCase> logger,
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
                if (_settings.processPrecioDealerSAP?.IsEnableFlag != 1)
                {
                    return new ProcesarItemsResult { IsSuccess = true, Message = "Proceso CLIENTE deshabilitado" };
                }

                var itemsList = (await _getItemsHandler.HandleAsync(new GetPendingDealerPrecioToApiCommand())).ToList();
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
                        new SendPrecioToApiCommand(item, ItemDestinationType.Dealer));
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

                return new ProcesarItemsResult { IsSuccess = true, ItemsProcessed = itemsList.Count, ItemsSent = sent, ItemsFailed = failed, Message = $"DEALER: {sent} enviados, {failed} fallidos" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en proceso CLIENTE HANA -> API");
                return new ProcesarItemsResult { IsSuccess = false, ErrorMessage = ex.Message };
            }
        }
    }
}
