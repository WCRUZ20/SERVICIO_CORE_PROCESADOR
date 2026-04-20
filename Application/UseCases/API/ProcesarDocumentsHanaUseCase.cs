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
using System.Threading;
using System.Threading.Tasks;

namespace Application.UseCases.API
{
    /// <summary>
    /// UseCase para leer documentos desde HANA y enviarlas a la API externa
    /// Procesa registro por registro: obtiene, envía al API y marca como procesada
    /// </summary>
    public class ProcesarDocumentsHanaUseCase
    {
        private readonly ICommandHandler<GetPendingHanaDocumentsCommand, IEnumerable<SapDrivinTable>> _getDocumentsHandler;
        private readonly ICommandHandler<SendDocumentsToApiCommand, (bool IsSuccess, string? Message)> _sendToApiHandler;
        private readonly ICommandHandler<MarkStatusDocuemntAsCommand, bool> _markStatusDocumentAsHandler;
        private readonly ILogger<ProcesarDocumentsHanaUseCase> _logger;
        private readonly WorkerSettings _settings;

        public ProcesarDocumentsHanaUseCase(
            ICommandHandler<GetPendingHanaDocumentsCommand, IEnumerable<SapDrivinTable>> getDocumentsHandler,
            ICommandHandler<SendDocumentsToApiCommand, (bool IsSuccess, string? ErrorMessage)> sendToApiHandler,
            ICommandHandler<MarkStatusDocuemntAsCommand, bool> markAsProcessedHandler,
            ILogger<ProcesarDocumentsHanaUseCase> logger,
            IOptions<WorkerSettings> settings)
        {
            _getDocumentsHandler = getDocumentsHandler;
            _sendToApiHandler = sendToApiHandler;
            _markStatusDocumentAsHandler = markAsProcessedHandler;
            _logger = logger;
            _settings = settings.Value;
        }

        public async Task<ProcesarDocumentsResult> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                if (_settings.processDocumentSAP?.IsEnableFlag != 1)
                {
                    _logger.LogInformation("Proceso de envío a API deshabilitado");
                    return new ProcesarDocumentsResult { IsSuccess = true, Message = "Proceso deshabilitado" };
                }

                _logger.LogInformation("Iniciando procesamiento de documentos desde HANA -> API");

                // 1. Obtener documentos pendientes desde HANA
                var getCommand = new GetPendingHanaDocumentsCommand();
                var documents = await _getDocumentsHandler.HandleAsync(getCommand);
                var documentsList = documents.ToList();

                if (!documentsList.Any())
                {
                    _logger.LogInformation("No hay documentos pendientes de procesar HANA -> API");
                    return new ProcesarDocumentsResult
                    {
                        IsSuccess = true,
                        DocumentsProcessed = 0,
                        DocumentSent = 0,
                        DocumentFailed = 0,
                        Message = "No hay documentos pendientes HANA -> API"
                    };
                }

                _logger.LogInformation(
                    "Se encontraron {Count} documentos pendientes en HANA -> API",
                    documentsList.Count);

                // 2. Procesar registro por registro: enviar al API y marcar como procesada
                int documentSent = 0;
                int documentFailed = 0;
                var errors = new List<string>();

                foreach (var documento in documentsList)
                {

                    // 3. Cambiar Estado a Enviado
                    documento.Status = (int)StatusHanaDocumentLevel.Sent;
                    var markCommand = new MarkStatusDocuemntAsCommand(documento);
                    var sentFlag = await _markStatusDocumentAsHandler.HandleAsync(markCommand);
                    if (sentFlag)
                    {
                        //Enviar al API
                        var sendCommand = new SendDocumentsToApiCommand(documento);
                        var sendResult = await _sendToApiHandler.HandleAsync(sendCommand);

                        if (sendResult.IsSuccess)
                        {
                            // Si el envío fue exitoso, marcar como Confirmado
                            documento.Status = (int)StatusHanaDocumentLevel.Confirmed;
                            documento.Json = sendResult.Message ?? "";
                            documentSent++;
                            _logger.LogDebug($"Documento DocEntry:{documento.SapDocEntry} DocNum: {documento.SapDocNum} enviado y marcado como procesado");
                        }
                        else
                        {
                            // Si el envío NO fue exitoso, marcar como Error
                            documento.Status = (int)StatusHanaDocumentLevel.Error;
                            documento.Json = sendResult.Message ?? "";
                            documentFailed++;
                            _logger.LogError($"Documento DocEntry:{documento.SapDocEntry} DocNum: {documento.SapDocNum} error: {sendResult.Message}");

                        }

                        // 3. Actualizar registro por registro
                        var markCommandPost = new MarkStatusDocuemntAsCommand(documento);
                        await _markStatusDocumentAsHandler.HandleAsync(markCommandPost);
                    }
                    
                }

                return new ProcesarDocumentsResult
                {
                    IsSuccess = true,
                    DocumentsProcessed = documentsList.Count,
                    DocumentSent = documentSent,
                    DocumentFailed = documentFailed,
                    Errors = errors.Any() ? errors : null,
                    Message = $"Proceso completado: {documentSent} enviadas, {documentFailed} fallidas"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al procesar Documento desde HANA");
                return new ProcesarDocumentsResult
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }
    }

    public class ProcesarDocumentsResult
    {
        public bool IsSuccess { get; set; }
        public int DocumentsProcessed { get; set; }
        public int DocumentSent { get; set; }
        public int DocumentFailed { get; set; }
        public List<string>? Errors { get; set; }
        public string? Message { get; set; }
        public string? ErrorMessage { get; set; }
    }
}

