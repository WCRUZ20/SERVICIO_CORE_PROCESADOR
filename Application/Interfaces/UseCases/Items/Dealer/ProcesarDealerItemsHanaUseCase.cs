using Application.Abstractions;
using Application.Commands;
using Application.Commands.Dealer;
using Application.Interfaces.API;
using Application.Interfaces.UseCases.Items;
using Domain.Configuration;
using Domain.Helper;
using Domain.SAP;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Application.UseCases.Items.Dealer;

public class ProcesarDealerItemsHanaUseCase : IProcesarDealerItemsHanaUseCase
{
    private readonly ICommandHandler<GetPendingDealerHanaItemsCommand, IEnumerable<SapItemsTable>> _getItemsHandler;
    private readonly ICommandHandler<SendItemsToApiCommand, (bool IsSuccess, string? Message)> _sendToApiHandler;
    private readonly ICommandHandler<MarkStatusItemAsCommand, bool> _markStatusItemAsHandler;
    private readonly ILogger<ProcesarDealerItemsHanaUseCase> _logger;
    private readonly WorkerSettings _settings;

    public ProcesarDealerItemsHanaUseCase(
        ICommandHandler<GetPendingDealerHanaItemsCommand, IEnumerable<SapItemsTable>> getItemsHandler,
        ICommandHandler<SendItemsToApiCommand, (bool IsSuccess, string? Message)> sendToApiHandler,
        ICommandHandler<MarkStatusItemAsCommand, bool> markStatusItemAsHandler,
        ILogger<ProcesarDealerItemsHanaUseCase> logger,
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
                return new ProcesarItemsResult { IsSuccess = true, Message = "Proceso DEALER deshabilitado" };
            }

            var itemsList = (await _getItemsHandler.HandleAsync(new GetPendingDealerHanaItemsCommand())).ToList();
            if (!itemsList.Any())
            {
                return new ProcesarItemsResult { IsSuccess = true, Message = "No hay articulos DEALER pendientes", ItemsProcessed = 0 };
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
                    new SendItemsToApiCommand(item, ItemDestinationType.Dealer));
                item.Status = sendResult.IsSuccess ? (int)StatusHanaDocumentLevel.Confirmed : (int)StatusHanaDocumentLevel.Error;
                item.Json = sendResult.Message ?? string.Empty;
                if (sendResult.IsSuccess) sent++; else failed++;
                await _markStatusItemAsHandler.HandleAsync(new MarkStatusItemAsCommand(item));
            }

            return new ProcesarItemsResult { IsSuccess = true, ItemsProcessed = itemsList.Count, ItemsSent = sent, ItemsFailed = failed, Message = $"DEALER: {sent} enviados, {failed} fallidos" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en proceso DEALER HANA -> API");
            return new ProcesarItemsResult { IsSuccess = false, ErrorMessage = ex.Message };
        }
    }
}
