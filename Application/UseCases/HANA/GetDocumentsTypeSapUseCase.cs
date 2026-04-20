using Application.Abstractions;
using Application.Commands;
using Application.DTO;
using Application.Interfaces;
using Domain.Configuration;
using Domain.SAP;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SAPbouiCOM;
using System.Globalization;

namespace Application.UseCases.HANA
{
    /// <summary>
    /// UseCase para obtener documentos desde SAP (ODBC) e insertarlas en HANA
    /// </summary>
    public class GetDocumentsTypeSapUseCase
    {
        private readonly ICommandHandler<GetPendingDocumentsTypeCommand, IEnumerable<SapDrivinTableDTO>> _getDocumentsTypeHandler;
        private readonly ICommandHandler<CheckDocumentExistsCommand, bool> _checkExistsHandler;
        private readonly ICommandHandler<InsertDocumentCommand, Boolean> _insertTransferHandler;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetDocumentsTypeSapUseCase> _logger;
        private readonly WorkerSettings _settings;

        public GetDocumentsTypeSapUseCase(
            ICommandHandler<GetPendingDocumentsTypeCommand, IEnumerable<SapDrivinTableDTO>> getDocumentsHandler,
            ICommandHandler<CheckDocumentExistsCommand, bool> checkExistsHandler,
            ICommandHandler<InsertDocumentCommand, Boolean> insertTransferHandler,
            IUnitOfWork unitOfWork,
            ILogger<GetDocumentsTypeSapUseCase> logger,
            IOptions<WorkerSettings> settings)
        {
            _getDocumentsTypeHandler = getDocumentsHandler;
            _checkExistsHandler = checkExistsHandler;
            _insertTransferHandler = insertTransferHandler;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _settings = settings.Value;
        }

        public async Task<ObtenerDocumentsResult> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                if (_settings.getDocumentSAP?.IsEnableFlag != 1)
                {
                    _logger.LogInformation("Proceso de obtención de documentos deshabilitado");
                    return new ObtenerDocumentsResult { IsSuccess = true, Message = "Proceso deshabilitado" };
                }

                _logger.LogInformation(
                    $"Iniciando obtención de documentos desde SAP");

                try
                {
                    // 1. Obtener documentos (retorna DTOs)
                    var getCommand = new GetPendingDocumentsTypeCommand();
                    var documents = await _getDocumentsTypeHandler.HandleAsync(getCommand);
                    var documentsList = documents.ToList();

                    if (!documentsList.Any())
                    {
                        return new ObtenerDocumentsResult
                        {
                            IsSuccess = true,
                            DocumentsFound = 0,
                            DocumentsInserted = 0,
                            Message = "No hay documentos nuevos"
                        };
                    }

                    _logger.LogInformation(
                        $"Se encontraron {documentsList.Count} documentos desde SAP");

                    // 2. Procesar registro por registro: verificar e insertar
                    int documentsInserted = 0;
                    int documentsFound = documentsList.Count;

                    foreach (var document in documentsList)
                    {
                        // Verificar si existe
                        var checkCommand = new CheckDocumentExistsCommand(document.Transaction, document.SapDocEntry, document.SapDocNum);
                        var exists = await _checkExistsHandler.HandleAsync(checkCommand);
                        
                        if (!exists)
                        {
                            // Insertar inmediatamente (una solo documento)
                            var insertCommand = new InsertDocumentCommand(document);
                            var inserted = await _insertTransferHandler.HandleAsync(insertCommand);
                            
                            if (inserted)
                            {
                                documentsInserted++;

                            }
                            else
                            {
                                _logger.LogWarning( $"No se pudo insertar el documento DocEntry:{document.SapDocEntry} Transaction: {document.Transaction} ");
                            }
                        }
                        else
                        {
                            _logger.LogDebug(
                                $"documento DocEntry:{document.SapDocEntry} Transaction: {document.Transaction} ya existe, se omite");
                        }
                    }

                    return new ObtenerDocumentsResult
                    {
                        IsSuccess = true,
                        DocumentsFound = documentsFound,
                        DocumentsInserted = documentsInserted,
                        Message = $"Se procesaron {documentsFound} documentos, {documentsInserted} insertadas"
                    };
                }
                catch (Exception ex)
                {
                    //await _unitOfWork.RollbackAsync(cancellationToken);
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener documentos desde SAP");
                return new ObtenerDocumentsResult
                {
                    IsSuccess = false,
                    Message = $"Error al obtener documentos desde SAP: {ex.Message}"
                };
            }
        }

    }

    public class ObtenerDocumentsResult
    {
        public bool IsSuccess { get; set; }
        public int DocumentsFound { get; set; }
        public int DocumentsInserted { get; set; }
        public string? Message { get; set; }
       
    }
}

