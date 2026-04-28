
using Application.Interfaces.API;
using Application.Interfaces.UseCases.Items.Cliente;
using Application.Interfaces.UseCases.Items.Dealer;
using Application.Interfaces.UseCases.Stock.Cliente;
using Application.Interfaces.UseCases.Stock.Dealer;
using Domain.Configuration;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Reflection;

namespace SERVICIOCORE_PROCESARDOCUMENTOSSAP
{
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
            var interval = _settings.Worker.LoopInterval;
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

                    //Procedimiento articulo
                    Task _ventaOnline = ProcessVentaOnlineAsync(scope, stoppingToken);
                    Task _dealer = ProcessDealerAsync(scope, stoppingToken);

                    //Procedimiento stock
                    Task _stockCliente = ProcessStockClienteAsync(scope, stoppingToken);
                    Task _stockDealer = ProcessStockDealerAsync(scope, stoppingToken);

                    await Task.WhenAll(_ventaOnline, _dealer, _stockCliente, _stockDealer);

                    stopwatch.Stop();

                    _logger.LogInformation(
                        $"Ciclo completado en {stopwatch.ElapsedMilliseconds} ms ({stopwatch.Elapsed.TotalSeconds:F2} s)");
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

        private async Task ProcessVentaOnlineAsync(IServiceScope scope, CancellationToken stoppingToken)
        {
            await ExecuteSapToHanaClienteProcessAsync(scope, stoppingToken); // articulos - inserta en cola
            await ExecuteHanaToApiClienteProcessAsync(scope, stoppingToken); // articulos - envia api interna
        }

        private async Task ProcessDealerAsync(IServiceScope scope, CancellationToken stoppingToken)
        {
            await ExecuteSapToHanaDealerProcessAsync(scope, stoppingToken);
            await ExecuteHanaToApiDealerProcessAsync(scope, stoppingToken);
        }

        private async Task ProcessStockClienteAsync(IServiceScope scope, CancellationToken stoppingToken)
        {
            await ExecuteSapToHanaClienteStockProcessAsync(scope, stoppingToken);
        }

        private async Task ProcessStockDealerAsync(IServiceScope scope, CancellationToken stoppingToken)
        {
            await ExecuteSapToHanaDealerStockProcessAsync(scope, stoppingToken);
        }

        private async Task ExecuteSapToHanaClienteStockProcessAsync(IServiceScope scope, CancellationToken cancellationToken)
        {
            var useCaseArticulo = scope.ServiceProvider.GetRequiredService<IGetClienteStockSapUseCase>();
            _logger.LogInformation("Ejecutando PROCESO STOCK ONLINE (CLIENTE) SAP → HANA");
            var resultAticulo = await useCaseArticulo.ExecuteAsync(cancellationToken);

            if (resultAticulo.IsSuccess)
            {
                _logger.LogInformation(
                $"Proceso STOCK ONLINE (CLIENTE) SAP → HANA completado: {resultAticulo.Message}. " +
                $"Encontradas: {resultAticulo.ItemsFound}, Insertadas: {resultAticulo.ItemsInserted}"
                );
            }
            else
            {
                _logger.LogError(
                    $"Error en proceso ONLINE (CLIENTE) SAP → HANA: {resultAticulo.Message}");
            }

        }

        private async Task ExecuteSapToHanaDealerStockProcessAsync(IServiceScope scope, CancellationToken cancellationToken)
        {
            var useCaseArticulo = scope.ServiceProvider.GetRequiredService<IGetDealerStockSapUseCase>();
            _logger.LogInformation("Ejecutando PROCESO STOCK DEALER SAP → HANA");
            var resultAticulo = await useCaseArticulo.ExecuteAsync(cancellationToken);

            if (resultAticulo.IsSuccess)
            {
                _logger.LogInformation(
                $"Proceso STOCK DEALER SAP → HANA completado: {resultAticulo.Message}. " +
                $"Encontradas: {resultAticulo.ItemsFound}, Insertadas: {resultAticulo.ItemsInserted}"
                );
            }
            else
            {
                _logger.LogError(
                    $"Error en proceso DEALER SAP → HANA: {resultAticulo.Message}");
            }
        }

        private async Task ExecuteSapToHanaClienteProcessAsync(IServiceScope scope, CancellationToken cancellationToken)
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

        private async Task ExecuteSapToHanaDealerProcessAsync(IServiceScope scope, CancellationToken cancellationToken)
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

        private async Task ExecuteHanaToApiClienteProcessAsync(IServiceScope scope, CancellationToken cancellationToken)
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

        private async Task ExecuteHanaToApiDealerProcessAsync(IServiceScope scope, CancellationToken cancellationToken)
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

    }
}
