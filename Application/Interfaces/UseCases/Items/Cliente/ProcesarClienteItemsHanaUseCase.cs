using Application.Abstractions;
using Application.Commands;
using Application.Commands.Cliente;
using Application.DTO;
using Application.Interfaces.API;
using Application.Interfaces.UseCases.Items;
using Domain.Configuration;
using Domain.Helper;
using Domain.SAP;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Application.UseCases.Items.Cliente;

public class ProcesarClienteItemsHanaUseCase : IProcesarClienteItemsHanaUseCase
{
    private readonly ICommandHandler<GetPendingClienteHanaItemsCommand, IEnumerable<SapItemsTable>> _getItemsHandler;
    private readonly ICommandHandler<SendItemsToApiCommand, (bool IsSuccess, string? Message)> _sendToApiHandler;
    private readonly ICommandHandler<MarkStatusItemAsCommand, bool> _markStatusItemAsHandler;
    private readonly ILogger<ProcesarClienteItemsHanaUseCase> _logger;
    private readonly WorkerSettings _settings;

    public ProcesarClienteItemsHanaUseCase(
        ICommandHandler<GetPendingClienteHanaItemsCommand, IEnumerable<SapItemsTable>> getItemsHandler,
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
            if (_settings.processDocumentSAP?.IsEnableFlag != 1)
            {
                return new ProcesarItemsResult { IsSuccess = true, Message = "Proceso CLIENTE deshabilitado" };
            }

            var itemsList = (await _getItemsHandler.HandleAsync(new GetPendingClienteHanaItemsCommand())).ToList();
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
