using Application.Abstractions;
using Application.Commands;
using Application.Interfaces;
using Application.Interfaces.HANA;
using Domain.Configuration;
using Domain.Drivin;
using Domain.Helper;
using Domain.SAP;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Application.UseCases.HANA
{
    /// <summary>
    /// UseCase para obtener documentos desde SAP (ODBC) e insertarlas en HANA
    /// </summary>
    public class GetHookSapUseCase
    {
        private readonly ICommandHandler<GetPendingHooksCommand, IEnumerable<SapDrivinTable>> _getPendingHooksHandler;
        private readonly ICommandHandler<VerifyDocumentStatusSAPCommand, bool> _verifyDocumentStatusSAPHandler;
        private readonly IHanaRepository _hanaRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetHookSapUseCase> _logger;
        private readonly WorkerSettings _settings;

        public GetHookSapUseCase(
            ICommandHandler<GetPendingHooksCommand, IEnumerable<SapDrivinTable>> getDocumentsHandler,
            ICommandHandler<VerifyDocumentStatusSAPCommand, bool> verifyDocumentStatusSAPHandler,
            IHanaRepository hanaRepository,
            IUnitOfWork unitOfWork,
            ILogger<GetHookSapUseCase> logger,
            IOptions<WorkerSettings> settings)
        {
            _getPendingHooksHandler = getDocumentsHandler;
            _verifyDocumentStatusSAPHandler = verifyDocumentStatusSAPHandler;
            _hanaRepository = hanaRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _settings = settings.Value;
        }

        public async Task<ObtenerHookResult> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                if (_settings.processHook?.IsEnableFlag != 1)
                {
                    _logger.LogInformation("Proceso de obtención de hooks deshabilitado");
                    return new ObtenerHookResult { IsSuccess = true, Message = "Proceso de obtención de hooks deshabilitado" };
                }

                _logger.LogInformation(
                    $"Iniciando obtención de hooks desde SAP");


                // 1. Obtener hooks
                var getCommand = new GetPendingHooksCommand();
                var _documents = await _getPendingHooksHandler.HandleAsync(getCommand);
                var _documentList = _documents.ToList();

                if (!_documentList.Any())
                {
                    return new ObtenerHookResult
                    {
                        IsSuccess = true,
                        //HooksFound = 0,
                        //HooksChanged = 0,
                        Message = "No hay hooks nuevos"
                    };
                }

                _logger.LogInformation(
                    $"Se encontraron {_documentList.Count} hooks desde HANA");

                //2.Procesar hooks: verificar y cambiar status
                int HooksChanged = 0;
                int HooksFound = _documentList.Count;

                foreach (var document in _documentList)
                {
                    var completeChangeStatusOrdersFlag = true;
                    var changeStatusTableInternaFlag = true;
                    var _hook = document.Json;
                    try
                    {
                        // INICIAR TRANSACCIÓN para este documento
                        await _unitOfWork.BeginTransactionAsync(cancellationToken);
                        var connection = _unitOfWork.GetConnection();
                        var odbcTransaction = _unitOfWork.GetTransaction();


                        //var payload = JsonSerializer.Deserialize<ResponseWebHookEntity>(_hook);
                        var payload = JsonSerializer.Deserialize<ResponseWebHookEntity>(
                                    _hook,
                                    new JsonSerializerOptions
                                    {
                                        PropertyNameCaseInsensitive = true
                                    });

                        var ordersList = payload.orders;


                        foreach (var item in ordersList)
                        {
                            var flagVerify = false;
                            var ordenCode = item.code;
                            var docEntry = item.alt_code;
                            var statusOrder = item.status;

                            int transaction = 0;
                            var docNum = "";
                            if (!string.IsNullOrWhiteSpace(ordenCode) && ordenCode.Contains('-') && !String.IsNullOrEmpty(docEntry))
                            {
                                var parts = ordenCode.Split('-', 2); // máximo 2 partes

                                if (parts.Length == 2 &&
                                    !string.IsNullOrWhiteSpace(parts[0]) &&
                                    !string.IsNullOrWhiteSpace(parts[1]))
                                {
                                    transaction = parts[0] switch
                                    {
                                        var t when t.StartsWith("EN", StringComparison.OrdinalIgnoreCase) => 1,
                                        var t when t.StartsWith("TR", StringComparison.OrdinalIgnoreCase) => 2,
                                        _ => 0
                                    };

                                    docNum = parts[1];
                                    flagVerify = true;
                                }
                            }

                            if (flagVerify && transaction != 0)
                            {

                                // 2. Verificar si el documento en SAP ya tiene el status de DRIVIN
                                var checkCommand = new VerifyDocumentStatusSAPCommand(transaction, Convert.ToInt32(docEntry), Convert.ToInt32(docNum), statusOrder);
                                var exists = await _verifyDocumentStatusSAPHandler.HandleAsync(checkCommand);

                                if (!exists)
                                {
                                    // 3. Actualizar estado en SAP (con transacción)
                                    var actualizado = await _hanaRepository.ChangeStatusDocumenSAP(
                                        transaction,
                                        Convert.ToInt32(docEntry),
                                        Convert.ToInt32(docNum),
                                        statusOrder,
                                        connection,
                                        odbcTransaction,
                                        cancellationToken);

                                    if (actualizado = false)
                                    {
                                        completeChangeStatusOrdersFlag = false;
                                        _logger.LogWarning($"No se pudo actualizar la orden {ordenCode} | DocNum:{docNum} | estado: {statusOrder}");
                                    }
                                    else
                                    {
                                        HooksChanged++;

                                    }
                                }
                                else
                                {
                                    _logger.LogInformation(
                                    $"Ya existe Order Hook actualiada en SAP: {ordenCode} | DocEntry:{docEntry} | estado: {statusOrder}, se omite");
                                }


                            }
                            else {
                                _logger.LogWarning($"La orden {ordenCode} del hook NO contiene la estructura esperada: Codigo Orden:  {ordenCode} | estado: {statusOrder}");
                            }


                        }

                        if (completeChangeStatusOrdersFlag)
                        {
                            // 4. MArcar como completado en tabla interna despues de recorrer todas las orders del hook
                            var documento = new SapDrivinTable
                            {
                                Code = document.Code,
                                Json = document.Json,
                                Status = (int)StatusHanaDocumentLevel.Confirmed
                            };

                             changeStatusTableInternaFlag = await _hanaRepository.MarkHookStatusDocumentAsAsync(
                                documento,
                                connection,
                                odbcTransaction,
                                cancellationToken);
                        }
                        
                       
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("Transacción revertida debido a error en hook | Error: {error}", ex);

                    }

                    if (changeStatusTableInternaFlag)
                    {
                        // ========== COMMIT ==========
                        await _unitOfWork.CommitAsync(cancellationToken);
                        _logger.LogInformation($"Transacción confirmada para hook con Codigo:{document.Code}");
                    }
                    else
                    {
                        // ========== ROLLBACK ==========


                        await _unitOfWork.RollbackAsync(cancellationToken);
                        _logger.LogWarning($"Transacción revertida para hook con Codigo:{document.Code}");
                    }

                }



                return new ObtenerHookResult
                {
                    IsSuccess = true,
                    Message = $"Hooks procesados: {HooksChanged} de {HooksFound}"
                };

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener documentos desde SAP");
                return new ObtenerHookResult
                {
                    IsSuccess = false,
                    Message = $"Error al obtener documentos desde SAP: {ex.Message}"
                };
            }
        }

    }

    public class ObtenerHookResult
    {
        public bool IsSuccess { get; set; }
        //public int HooksFound { get; set; }
        //public int HooksChanged { get; set; }
        public string? Message { get; set; }

    }
}

