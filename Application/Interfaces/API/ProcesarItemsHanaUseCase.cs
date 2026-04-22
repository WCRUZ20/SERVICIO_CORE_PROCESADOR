using Application.Abstractions;
using Application.Commands;
using Domain.Configuration;
using Domain.Helper;
using Domain.SAP;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.API
{
    public class ProcesarItemsHanaUseCase
    {
        private readonly ICommandHandler<GetPendingHanaItemsCommand, IEnumerable<SapItemsTable>> _getItemsHandler;
        private readonly ICommandHandler<SendItemsToApiCommand, (bool IsSuccess, string? Message)> _sendToApiHandler;
        private readonly ICommandHandler<MarkStatusItemAsCommand, bool> _markStatusItemAsHandler;
        private readonly ILogger<ProcesarItemsHanaUseCase> _logger;
        private readonly WorkerSettings _settings;

        public ProcesarItemsHanaUseCase(
            ICommandHandler<GetPendingHanaItemsCommand, IEnumerable<SapItemsTable>> getItemsHandler,
            ICommandHandler<SendItemsToApiCommand, (bool IsSuccess, string? Message)> sendToApiHandler,
            ICommandHandler<MarkStatusItemAsCommand, bool> markStatusItemAsHandler,
            ILogger<ProcesarItemsHanaUseCase> logger,
            IOptions<WorkerSettings> settings)
        {
            _getItemsHandler = getItemsHandler;
            _sendToApiHandler = sendToApiHandler;
            _markStatusItemAsHandler = markStatusItemAsHandler;
            _logger = logger;
            _settings = settings.Value;
        }

        public async Task<ProcesarItemsResult> ExecuteAsync(
            string transactionType,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (_settings.processDocumentSAP?.IsEnableFlag != 1)
                {
                    _logger.LogInformation("Proceso de envío de articulos a API deshabilitado");
                    return new ProcesarItemsResult { IsSuccess = true, Message = "Proceso deshabilitado" };
                }

                _logger.LogInformation(
                    "Iniciando procesamiento de articulos desde HANA -> API para TransactionType={TransactionType}",
                    transactionType);

                var getCommand = new GetPendingHanaItemsCommand(transactionType);
                var items = await _getItemsHandler.HandleAsync(getCommand);
                var itemsList = items.ToList();

                if (!itemsList.Any())
                {
                    _logger.LogInformation("No hay articulos pendientes de procesar HANA -> API");
                    return new ProcesarItemsResult
                    {
                        IsSuccess = true,
                        ItemsProcessed = 0,
                        ItemsSent = 0,
                        ItemsFailed = 0,
                        Message = "No hay articulos pendientes HANA -> API"
                    };
                }

                int itemsSent = 0;
                int itemsFailed = 0;
                var errors = new List<string>();

                foreach (var item in itemsList)
                {
                    item.Status = (int)StatusHanaDocumentLevel.Sent;
                    var markSent = await _markStatusItemAsHandler.HandleAsync(new MarkStatusItemAsCommand(item));
                    if (!markSent)
                    {
                        itemsFailed++;
                        errors.Add($"No se pudo marcar como enviado SapDocEntry={item.SapDocEntry}");
                        continue;
                    }

                    var sendResult = await _sendToApiHandler.HandleAsync(new SendItemsToApiCommand(item));
                    if (sendResult.IsSuccess)
                    {
                        item.Status = (int)StatusHanaDocumentLevel.Confirmed;
                        item.Json = sendResult.Message ?? string.Empty;
                        itemsSent++;
                    }
                    else
                    {
                        item.Status = (int)StatusHanaDocumentLevel.Error;
                        item.Json = sendResult.Message ?? string.Empty;
                        itemsFailed++;
                    }

                    await _markStatusItemAsHandler.HandleAsync(new MarkStatusItemAsCommand(item));
                }

                return new ProcesarItemsResult
                {
                    IsSuccess = true,
                    ItemsProcessed = itemsList.Count,
                    ItemsSent = itemsSent,
                    ItemsFailed = itemsFailed,
                    Errors = errors.Any() ? errors : null,
                    Message = $"Proceso completado: {itemsSent} enviados, {itemsFailed} fallidos"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al procesar articulos desde HANA");
                return new ProcesarItemsResult
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }
    }

    public class ProcesarItemsResult
    {
        public bool IsSuccess { get; set; }
        public int ItemsProcessed { get; set; }
        public int ItemsSent { get; set; }
        public int ItemsFailed { get; set; }
        public List<string>? Errors { get; set; }
        public string? Message { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
