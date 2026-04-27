
using Application.Interfaces.API;
using Application.Interfaces.UseCases.Items;
using Application.UseCases.API;
using Application.UseCases.HANA;
using Domain.Configuration;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Reflection;

namespace SERVICIOCORE_PROCESARDOCUMENTOSSAP
{
    /// <summary>
    /// Worker principal que ejecuta dos procesos:
    /// 1. Obtener documents desde SAP (ODBC) e insertarlas en HANA
    /// 2. Leer documents desde HANA y enviarlas a la API externa propia
    /// </summary>
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly WorkerSettings _settings;

        public Worker(
            ILogger<Worker> logger,
            IServiceScopeFactory scopeFactory,
            IOptions<WorkerSettings> settings)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _settings = settings.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Obtener intervalo de configuración 
            var interval = _settings.Worker.LoopInterval ;
            var intervalTimeSpan = TimeSpan.FromMilliseconds(interval);

            while (!stoppingToken.IsCancellationRequested)
            {
                if (_settings.Worker?.IsEnableFlag != 1)
                {
                    _logger.LogInformation("Worker deshabilitado en configuración");
                    await Task.Delay(intervalTimeSpan, stoppingToken);
                    continue;
                }
                var stopwatch = Stopwatch.StartNew();
                try
                {
                    _logger.LogInformation(
                        $"Iniciando ciclo de procesamiento: {DateTimeOffset.Now}");

                    using var scope = _scopeFactory.CreateScope();

                    // ============================================
                    // PROCESO 1: SAP (ODBC) → HANA
                    // ============================================
                    await ExecuteSapToHanaClienteProcessAsync(scope, stoppingToken);
                    await ExecuteSapToHanaDealerProcessAsync(scope, stoppingToken);

                    // ============================================
                    // PROCESO 2: HANA → API MIDDLEWARE
                    // ============================================
                    await ExecuteHanaToApiClienteProcessAsync(scope, stoppingToken);
                    await ExecuteHanaToApiDealerProcessAsync(scope, stoppingToken);

                    // ============================================
                    // PROCESO 3: API MIDDLEWARE → HANA
                    // ============================================
                    await ExecuteHookToSAPDocumentProcessAsync(scope, stoppingToken);

                    stopwatch.Stop();

                    _logger.LogInformation(
                        $"Ciclo completado en { stopwatch.ElapsedMilliseconds} ms ({stopwatch.Elapsed.TotalSeconds:F2} s)");
                }
                
                catch (Exception ex)
                {
                    stopwatch.Stop();

                    _logger.LogError(
                        ex,
                        $"Error inesperado. Tiempo transcurrido: {stopwatch.ElapsedMilliseconds} ms");
                }
              
                // Esperar antes del siguiente ciclo
                await Task.Delay(intervalTimeSpan, stoppingToken);
            }
        }

        /// <summary>
        /// Ejecuta el proceso de obtención de documentos desde SAP e inserción en HANA
        /// </summary>
        private async Task ExecuteSapToHanaClienteProcessAsync(
            IServiceScope scope,
            CancellationToken cancellationToken)
        {
            var useCaseArticulo = scope.ServiceProvider.GetRequiredService<IGetClienteItemsSapUseCase>();
            _logger.LogInformation("Ejecutando PROCESO ONLINE (CLIENTE) SAP → HANA");
            var resultAticulo = await useCaseArticulo.ExecuteAsync(cancellationToken);

            if (resultAticulo.IsSuccess)
            {
                _logger.LogInformation(
                $"Proceso ONLINE (CLIENTE) SAP → HANA completado: {resultAticulo.Message}. " +
                $"Encontradas: {resultAticulo.ItemsFound}, Insertadas: {resultAticulo.ItemsInserted}"
                );
            }
            else
            {
                _logger.LogError(
                    $"Error en proceso ONLINE (CLIENTE) SAP → HANA: {resultAticulo.Message}");
            }

        }

        private async Task ExecuteSapToHanaDealerProcessAsync(
            IServiceScope scope,
            CancellationToken cancellationToken)
        {
            var useCaseArticulo = scope.ServiceProvider.GetRequiredService<IGetDealerItemsSapUseCase>();
            _logger.LogInformation("Ejecutando PROCESO DEALER SAP → HANA");
            var resultAticulo = await useCaseArticulo.ExecuteAsync(cancellationToken);

            if (resultAticulo.IsSuccess)
            {
                _logger.LogInformation(
                $"Proceso DEALER SAP → HANA completado: {resultAticulo.Message}. " +
                $"Encontradas: {resultAticulo.ItemsFound}, Insertadas: {resultAticulo.ItemsInserted}"
                );
            }
            else
            {
                _logger.LogError(
                    $"Error en proceso DEALER SAP → HANA: {resultAticulo.Message}");
            }
        }

        /// <summary>
        /// Ejecuta el proceso de lectura desde HANA y envío a API externa
        /// </summary>
        private async Task ExecuteHanaToApiClienteProcessAsync(
            IServiceScope scope,
            CancellationToken cancellationToken)
        {
            var itemsUseCase = scope.ServiceProvider.GetRequiredService<IProcesarClienteItemsHanaUseCase>();
            _logger.LogInformation("Ejecutando PROCESO ONLINE (CLIENTE) HANA → API");
            var itemsResult = await itemsUseCase.ExecuteAsync(cancellationToken);

            if (itemsResult.IsSuccess)
            {
                _logger.LogInformation(
                    "Proceso ONLINE (CLIENTE) HANA → API ARTICULOS completado: {Message}. Procesadas: {Processed}, Enviadas: {Sent}, Fallidas: {Failed}",
                    itemsResult.Message,
                    itemsResult.ItemsProcessed,
                    itemsResult.ItemsSent,
                    itemsResult.ItemsFailed);
            }
            else
            {
                _logger.LogError("Error en proceso ONLINE (CLIENTE) HANA → API ARTICULOS: {Error}", itemsResult.ErrorMessage);
            }
        }

        private async Task ExecuteHanaToApiDealerProcessAsync(
            IServiceScope scope,
            CancellationToken cancellationToken)
        {
            var itemsUseCase = scope.ServiceProvider.GetRequiredService<IProcesarDealerItemsHanaUseCase>();
            _logger.LogInformation("Ejecutando PROCESO DEALER HANA → API");
            var itemsResult = await itemsUseCase.ExecuteAsync(cancellationToken);

            if (itemsResult.IsSuccess)
            {
                _logger.LogInformation(
                    "Proceso DEALER HANA → API ARTICULOS completado: {Message}. Procesadas: {Processed}, Enviadas: {Sent}, Fallidas: {Failed}",
                    itemsResult.Message,
                    itemsResult.ItemsProcessed,
                    itemsResult.ItemsSent,
                    itemsResult.ItemsFailed);
            }
            else
            {
                _logger.LogError("Error en proceso DEALER HANA → API ARTICULOS: {Error}", itemsResult.ErrorMessage);
            }
        }

        /// <summary>
        /// Ejecuta el proceso de lectura desde HANA HOOK y actualiza el documento en SAP
        /// </summary>
        private async Task ExecuteHookToSAPDocumentProcessAsync(
            IServiceScope scope,
            CancellationToken cancellationToken)
        {
            try
            {
                var useCase = scope.ServiceProvider
                    .GetRequiredService<GetHookSapUseCase>();

                _logger.LogInformation("Ejecutando proceso HANA → SAP");

                var result = await useCase.ExecuteAsync(cancellationToken);

                if (result.IsSuccess)
                {
                    //_logger.LogInformation(
                    //    "Proceso HANA → SAP completado: {Message}. " +
                    //    "Procesadas: {Processed}, Enviadas: {Sent}",
                    //    result.Message,
                    //    result.HooksFound,
                    //    result.HooksChanged
                    //    );

                    //if (result.Errors != null && result.Errors.Any())
                    //{
                    //    foreach (var error in result.Errors)
                    //    {
                    //        _logger.LogWarning("Error en envío: {Error}", error);
                    //    }
                    //}
                }
                else
                {
                    //_logger.LogError(
                    //    "Error en proceso HANA → API: {Error}",
                    //    result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Excepción en proceso HANA → API");
                throw;
            }
        }
    }
}
