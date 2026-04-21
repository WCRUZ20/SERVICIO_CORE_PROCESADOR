using Application.Abstractions;
using Application.Commands;
using Application.DTO;
using Application.Interfaces;
using Domain.Configuration;
using Domain.SAP;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.HANA
{
    public class GetItemsSapUseCase
    {
        private readonly ICommandHandler<GetPendingItemsTypeCommand, IEnumerable<SapItemQueeDTO>> _getItemsHandler;
        private readonly ICommandHandler<CheckItemExistsCommand, bool> _checkExistsHandler;
        private readonly ICommandHandler<InsertItemsCommand, Boolean> _insertItemHandler;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetItemsSapUseCase> _logger;
        private readonly WorkerSettings _settings;

        public GetItemsSapUseCase(
            ICommandHandler<GetPendingItemsTypeCommand, IEnumerable<SapItemQueeDTO>> getItemsHandler,
            ICommandHandler<CheckItemExistsCommand, bool> checkExistsHandler,
            ICommandHandler<InsertItemsCommand, Boolean> insertItemHandler,
            IUnitOfWork unitOfWork,
            ILogger<GetItemsSapUseCase> logger,
            IOptions<WorkerSettings> settings)
        {
            _getItemsHandler = getItemsHandler;
            _checkExistsHandler = checkExistsHandler;
            _insertItemHandler = insertItemHandler;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _settings = settings.Value;
        }

        public async Task<ObtenerItemsResult> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                if (_settings.getDocumentSAP?.IsEnableFlag != 1)
                {
                    _logger.LogInformation("Proceso de obtención de items deshabilitado");
                    return new ObtenerItemsResult { IsSuccess = true, Message = "Proceso deshabilitado" };
                }

                _logger.LogInformation(
                    $"Iniciando obtención de items desde SAP");

                try
                {
                    // 1. Obtener articulos (retorna DTOs)
                    var getCommand = new GetPendingItemsTypeCommand();
                    var items = await _getItemsHandler.HandleAsync(getCommand);
                    var itemsList = items.ToList();

                    if (!itemsList.Any())
                    {
                        return new ObtenerItemsResult
                        {
                            IsSuccess = true,
                            ItemsFound = 0,
                            ItemsInserted = 0,
                            Message = "No hay articulos nuevos"
                        };
                    }

                    _logger.LogInformation(
                        $"Se encontraron {itemsList.Count} articulos desde SAP");

                    // 2. Procesar registro por registro: verificar e insertar
                    int itemsInserted = 0;
                    int itemsFound = itemsList.Count;

                    foreach (var item in itemsList)
                    {
                        // Verificar si existe
                        var checkCommand = new CheckItemExistsCommand(item.Transaction, item.SapDocEntry);
                        var exists = await _checkExistsHandler.HandleAsync(checkCommand);

                        if (!exists)
                        {
                            // Insertar inmediatamente (una solo documento)
                            var insertCommand = new InsertItemsCommand(item);
                            var inserted = await _insertItemHandler.HandleAsync(insertCommand);

                            if (inserted)
                            {
                                itemsInserted++;

                            }
                            else
                            {
                                _logger.LogWarning($"No se pudo insertar el articulo ItemCode:{item.SapDocEntry} Transaction: {item.Transaction} ");
                            }
                        }
                        else
                        {
                            _logger.LogDebug(
                                $"Articulo ItemCode:{item.SapDocEntry} Transaction: {item.Transaction} ya existe, se omite");
                        }
                    }

                    return new ObtenerItemsResult
                    {
                        IsSuccess = true,
                        ItemsFound = itemsFound,
                        ItemsInserted = itemsInserted,
                        Message = $"Se procesaron {itemsFound} articulos, {itemsInserted} insertadas"
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
                _logger.LogError(ex, "Error al obtener articulos desde SAP");
                return new ObtenerItemsResult
                {
                    IsSuccess = false,
                    Message = $"Error al obtener articulos desde SAP: {ex.Message}"
                };
            }
        }

    }

    public class ObtenerItemsResult
    {
        public bool IsSuccess { get; set; }
        public int ItemsFound { get; set; }
        public int ItemsInserted { get; set; }
        public string? Message { get; set; }

    }
}
