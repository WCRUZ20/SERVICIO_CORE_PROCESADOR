
using Application.Interfaces.API;
using Application.Interfaces.UseCases.Items.Cliente;
using Application.Interfaces.UseCases.Items.Dealer;
using Application.Interfaces.UseCases.Order.Cliente;
using Application.Interfaces.UseCases.Order.Dealer;
using Application.Interfaces.UseCases.Precio.Cliente;
using Application.Interfaces.UseCases.Precio.Dealer;
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
                                        
                    await Task.WhenAll(_ventaOnline, _dealer);

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

            await ExecuteSapToHanaClienteStockProcessAsync(scope, stoppingToken);
            await ExecuteHanaToApiClienteStockProcessAsync(scope, stoppingToken);

            await ExecuteSapToHanaClientePrecioProcessAsync(scope, stoppingToken);
            await ExecuteHanaToApiClientePrecioProcessAsync(scope, stoppingToken);

            await ExecuteRequestOrdersClienteProcessAsync(scope, stoppingToken);
            await ExecuteHanaToApiClienteOrderProcessAsync(scope, stoppingToken);
        }

        private async Task ProcessDealerAsync(IServiceScope scope, CancellationToken stoppingToken)
        {
            await ExecuteSapToHanaDealerProcessAsync(scope, stoppingToken);
            await ExecuteHanaToApiDealerProcessAsync(scope, stoppingToken);

            await ExecuteSapToHanaDealerStockProcessAsync(scope, stoppingToken);
            await ExecuteHanaToApiDealerStockProcessAsync(scope, stoppingToken);

            await ExecuteSapToHanaDealerPrecioProcessAsync(scope, stoppingToken);
            await ExecuteHanaToApiDealerPrecioProcessAsync(scope, stoppingToken);

            await ExecuteRequestOrdersDealerProcessAsync(scope, stoppingToken);
            await ExecuteHanaToApiDealerOrderProcessAsync(scope, stoppingToken);
        }

        #region "PRECIO INSERTA COLA CUANDO CAMBIA EL PRECIO"
        private async Task ExecuteSapToHanaClientePrecioProcessAsync(IServiceScope scope, CancellationToken cancellationToken)
        {
            var useCaseArticulo = scope.ServiceProvider.GetRequiredService<IGetClientePrecioSapUseCase>();
            _logger.LogInformation("EJECUTANDO PROCESO ENCOLAMIENTO CLIENTE - PRECIO");
            var resultAticulo = await useCaseArticulo.ExecuteAsync(cancellationToken);

            if (resultAticulo.IsSuccess)
            {
                _logger.LogInformation(
                $"PROCESO ENCOLAMIENTO CLIENTE - PRECIO COMPLETADO: {resultAticulo.Message}. " +
                $"ENCONTRADAS: {resultAticulo.ItemsFound}, INSERTADAS: {resultAticulo.ItemsInserted}"
                );
            }
            else
            {
                _logger.LogError(
                    $"ERROR EN PROCESO ENCOLAMIENTO CLIENTE - PRECIO: {resultAticulo.Message}");
            }

        }

        private async Task ExecuteSapToHanaDealerPrecioProcessAsync(IServiceScope scope, CancellationToken cancellationToken)
        {
            var useCaseArticulo = scope.ServiceProvider.GetRequiredService<IGetDealerPrecioSapUseCase>();
            _logger.LogInformation("EJECUTANDO PROCESO ENCOLAMIENTO DEALER - PRECIO");
            var resultAticulo = await useCaseArticulo.ExecuteAsync(cancellationToken);

            if (resultAticulo.IsSuccess)
            {
                _logger.LogInformation(
                $"EJECUTANDO PROCESO ENCOLAMIENTO DEALER - PRECIO COMPLETADO: {resultAticulo.Message}. " +
                $"ENCONTRADAS: {resultAticulo.ItemsFound}, INSERTADAS: {resultAticulo.ItemsInserted}"
                );
            }
            else
            {
                _logger.LogError(
                    $"ERROR EN PROCESO ENCOLAMIENTO DEALER - PRECIO: {resultAticulo.Message}");
            }
        }
        #endregion

        #region "PRECIO HACIA API"
        private async Task ExecuteHanaToApiClientePrecioProcessAsync(IServiceScope scope, CancellationToken cancellationToken)
        {
            var itemsUseCase = scope.ServiceProvider.GetRequiredService<IProcesarClientePrecioHanaUseCase>();
            _logger.LogInformation("EJECUTANDO PROCESO ENVIO API (SERVICELAYER) CLIENTE - PRECIO");
            var itemsResult = await itemsUseCase.ExecuteAsync(cancellationToken);

            if (itemsResult.IsSuccess)
            {
                _logger.LogInformation(
                    "PROCESO PROCESO ENVIO API (SERVICELAYER) CLIENTE - PRECIO COMPLETADO: {Message}. Procesadas: {Processed}, Enviadas: {Sent}, Fallidas: {Failed}",
                    itemsResult.Message,
                    itemsResult.ItemsProcessed,
                    itemsResult.ItemsSent,
                    itemsResult.ItemsFailed);
            }
            else
            {
                _logger.LogError("ERROR EN PROCESO ENVIO API (SERVICELAYER) CLIENTE - PRECIO: {Error}", itemsResult.ErrorMessage);
            }
        }

        private async Task ExecuteHanaToApiDealerPrecioProcessAsync(IServiceScope scope, CancellationToken cancellationToken)
        {
            var itemsUseCase = scope.ServiceProvider.GetRequiredService<IProcesarDealerPrecioHanaUseCase>();
            _logger.LogInformation("EJECUTANDO PROCESO ENVIO API (SERVICELAYER) DEALER - PRECIO");
            var itemsResult = await itemsUseCase.ExecuteAsync(cancellationToken);

            if (itemsResult.IsSuccess)
            {
                _logger.LogInformation(
                    "PROCESO ENVIO API (SERVICELAYER) DEALER - PRECIO COMPLETADO: {Message}. Procesadas: {Processed}, Enviadas: {Sent}, Fallidas: {Failed}",
                    itemsResult.Message,
                    itemsResult.ItemsProcessed,
                    itemsResult.ItemsSent,
                    itemsResult.ItemsFailed);
            }
            else
            {
                _logger.LogError("ERROR EN PROCESO ENVIO API (SERVICELAYER) DEALER - PRECIO: {Error}", itemsResult.ErrorMessage);
            }
        }
        #endregion

        #region "STOCK INSERTA COLA CUANDO CAMBIA EL STOCK"
        private async Task ExecuteSapToHanaClienteStockProcessAsync(IServiceScope scope, CancellationToken cancellationToken)
        {
            var useCaseArticulo = scope.ServiceProvider.GetRequiredService<IGetClienteStockSapUseCase>();
            _logger.LogInformation("EJECUTANDO PROCESO ENCOLAMIENTO CLIENTE - STOCK");
            var resultAticulo = await useCaseArticulo.ExecuteAsync(cancellationToken);

            if (resultAticulo.IsSuccess)
            {
                _logger.LogInformation(
                $"PROCESO ENCOLAMIENTO CLIENTE - STOCK COMPLETADO: {resultAticulo.Message}. " +
                $"ENCONTRADAS: {resultAticulo.ItemsFound}, INSERTADAS: {resultAticulo.ItemsInserted}"
                );
            }
            else
            {
                _logger.LogError(
                    $"ERROR EN PROCESO ENCOLAMIENTO CLIENTE - STOCK: {resultAticulo.Message}");
            }

        }

        private async Task ExecuteSapToHanaDealerStockProcessAsync(IServiceScope scope, CancellationToken cancellationToken)
        {
            var useCaseArticulo = scope.ServiceProvider.GetRequiredService<IGetDealerStockSapUseCase>();
            _logger.LogInformation("EJECUTANDO PROCESO ENCOLAMIENTO DEALER - STOCK");
            var resultAticulo = await useCaseArticulo.ExecuteAsync(cancellationToken);

            if (resultAticulo.IsSuccess)
            {
                _logger.LogInformation(
                $"PROCESO ENCOLAMIENTO DEALER - STOCK COMPLETADO: {resultAticulo.Message}. " +
                $"ENCONTRADAS: {resultAticulo.ItemsFound}, INSERTADAS: {resultAticulo.ItemsInserted}"
                );
            }
            else
            {
                _logger.LogError(
                    $"ERROR EN PROCESO ENCOLAMIENTO DEALER - STOCK: {resultAticulo.Message}");
            }
        }
        #endregion

        #region "STOCK HACIA API"
        private async Task ExecuteHanaToApiClienteStockProcessAsync(IServiceScope scope, CancellationToken cancellationToken)
        {
            var itemsUseCase = scope.ServiceProvider.GetRequiredService<IProcesarClienteStockHanaUseCase>();
            _logger.LogInformation("EJECUTANDO PROCESO ENVIO API (SERVICELAYER) CLIENTE - STOCK");
            var itemsResult = await itemsUseCase.ExecuteAsync(cancellationToken);

            if (itemsResult.IsSuccess)
            {
                _logger.LogInformation(
                    "PROCESO ENVIO API (SERVICELAYER) CLIENTE - STOCK COMPLETADO: {Message}. Procesadas: {Processed}, Enviadas: {Sent}, Fallidas: {Failed}",
                    itemsResult.Message,
                    itemsResult.ItemsProcessed,
                    itemsResult.ItemsSent,
                    itemsResult.ItemsFailed);
            }
            else
            {
                _logger.LogError("ERROR EN PROCESO ENVIO API (SERVICELAYER) CLIENTE - STOCK: {Error}", itemsResult.ErrorMessage);
            }
        }

        private async Task ExecuteHanaToApiDealerStockProcessAsync(IServiceScope scope, CancellationToken cancellationToken)
        {
            var itemsUseCase = scope.ServiceProvider.GetRequiredService<IProcesarDealerStockHanaUseCase>();
            _logger.LogInformation("EJECUTANDO PROCESO ENVIO API (SERVICELAYER) DEALER - STOCK");
            var itemsResult = await itemsUseCase.ExecuteAsync(cancellationToken);

            if (itemsResult.IsSuccess)
            {
                _logger.LogInformation(
                    "PROCESO ENVIO API (SERVICELAYER) DEALER - STOCK COMPLETADO: {Message}. Procesadas: {Processed}, Enviadas: {Sent}, Fallidas: {Failed}",
                    itemsResult.Message,
                    itemsResult.ItemsProcessed,
                    itemsResult.ItemsSent,
                    itemsResult.ItemsFailed);
            }
            else
            {
                _logger.LogError("ERROR EN PROCESO ENVIO API (SERVICELAYER) DEALER - STOCK: {Error}", itemsResult.ErrorMessage);
            }
        }
        #endregion

        #region "ARTICULOS INSERTA EN COLA"
        private async Task ExecuteSapToHanaClienteProcessAsync(IServiceScope scope, CancellationToken cancellationToken)
        {
            var useCaseArticulo = scope.ServiceProvider.GetRequiredService<IGetClienteItemsSapUseCase>();
            _logger.LogInformation("EJECUTANDO PROCESO ENCOLAMIENTO CLIENTE - ARTICULOS");
            var resultAticulo = await useCaseArticulo.ExecuteAsync(cancellationToken);

            if (resultAticulo.IsSuccess)
            {
                _logger.LogInformation(
                $"PROCESO ENCOLAMIENTO CLIENTE - ARTICULOS COMPLETADO: {resultAticulo.Message}. " +
                $"Encontradas: {resultAticulo.ItemsFound}, Insertadas: {resultAticulo.ItemsInserted}"
                );
            }
            else
            {
                _logger.LogError(
                    $"ERROR EN PROCESO ENCOLAMIENTO CLIENTE - ARTICULOS: {resultAticulo.Message}");
            }

        }

        private async Task ExecuteSapToHanaDealerProcessAsync(IServiceScope scope, CancellationToken cancellationToken)
        {
            var useCaseArticulo = scope.ServiceProvider.GetRequiredService<IGetDealerItemsSapUseCase>();
            _logger.LogInformation("EJECUTANDO PROCESO ENCOLAMIENTO DEALER - ARTICULOS");
            var resultAticulo = await useCaseArticulo.ExecuteAsync(cancellationToken);

            if (resultAticulo.IsSuccess)
            {
                _logger.LogInformation(
                $"PROCESO ENCOLAMIENTO DEALER - ARTICULOS COMPLETADO: {resultAticulo.Message}. " +
                $"Encontradas: {resultAticulo.ItemsFound}, Insertadas: {resultAticulo.ItemsInserted}"
                );
            }
            else
            {
                _logger.LogError(
                    $"ERROR EN PROCESO ENCOLAMIENTO DEALER - ARTICULOS: {resultAticulo.Message}");
            }
        }
        #endregion

        #region "ARTICULOS HACIA API"
        private async Task ExecuteHanaToApiClienteProcessAsync(IServiceScope scope, CancellationToken cancellationToken)
        {
            var itemsUseCase = scope.ServiceProvider.GetRequiredService<IProcesarClienteItemsHanaUseCase>();
            _logger.LogInformation("EJECUTANDO PROCESO ENVIO API (SERVICELAYER) CLIENTE - ARTICULOS");
            var itemsResult = await itemsUseCase.ExecuteAsync(cancellationToken);

            if (itemsResult.IsSuccess)
            {
                _logger.LogInformation(
                    "PROCESO ENVIO API (SERVICELAYER) CLIENTE - ARTICULOS COMPLETADO: {Message}. Procesadas: {Processed}, Enviadas: {Sent}, Fallidas: {Failed}",
                    itemsResult.Message,
                    itemsResult.ItemsProcessed,
                    itemsResult.ItemsSent,
                    itemsResult.ItemsFailed);
            }
            else
            {
                _logger.LogError("ERROR EN PROCESO ENVIO API (SERVICELAYER) CLIENTE - ARTICULOS: {Error}", itemsResult.ErrorMessage);
            }
        }

        private async Task ExecuteHanaToApiDealerProcessAsync(IServiceScope scope, CancellationToken cancellationToken)
        {
            var itemsUseCase = scope.ServiceProvider.GetRequiredService<IProcesarDealerItemsHanaUseCase>();
            _logger.LogInformation("EJECUTANDO PROCESO ENVIO API (SERVICELAYER) DEALER - ARTICULOS");
            var itemsResult = await itemsUseCase.ExecuteAsync(cancellationToken);

            if (itemsResult.IsSuccess)
            {
                _logger.LogInformation(
                    "PROCESO ENVIO API (SERVICELAYER) DEALER - ARTICULOS COMPLETADO: {Message}. Procesadas: {Processed}, Enviadas: {Sent}, Fallidas: {Failed}",
                    itemsResult.Message,
                    itemsResult.ItemsProcessed,
                    itemsResult.ItemsSent,
                    itemsResult.ItemsFailed);
            }
            else
            {
                _logger.LogError("ERROR EN PROCESO ENVIO API (SERVICELAYER) DEALER - ARTICULOS: {Error}", itemsResult.ErrorMessage);
            }
        }
        #endregion

        #region "ORDENES CONSULTA HACIA API MIDDLEWARE E INSERTA EN COLA"
        private async Task ExecuteRequestOrdersClienteProcessAsync(IServiceScope scope, CancellationToken cancellationToken)
        {
            var OrderUseCase = scope.ServiceProvider.GetRequiredService<IGetClienteOrderSapUseCase>();
            _logger.LogInformation("EJECUTANDO PROCESO CONSULTA API (SERVICELAYER) Y ENCOLAMIENTO CLIENTE - ORDENES");
            var resultOrder = await OrderUseCase.ExecuteAsync(cancellationToken);

            if (resultOrder.IsSuccess)
            {
                _logger.LogInformation(
                $"PROCESO CONSULTA API (SERVICELAYER) Y ENCOLAMIENTO CLIENTE - ORDENES COMPLETADO: {resultOrder.Message}. " +
                $"Encontradas: {resultOrder.ItemsFound}, Insertadas: {resultOrder.ItemsInserted}"
                );
            }
            else
            {
                _logger.LogError(
                    $"ERROR EN PROCESO CONSULTA API (SERVICELAYER) Y ENCOLAMIENTO CLIENTE - ORDENES: {resultOrder.Message}");
            }
        }

        private async Task ExecuteRequestOrdersDealerProcessAsync(IServiceScope scope, CancellationToken cancellationToken)
        {
            var OrderUseCase = scope.ServiceProvider.GetRequiredService<IGetDealerOrderSapUseCase>();
            _logger.LogInformation("EJECUTANDO PROCESO CONSULTA API (SERVICELAYER) Y ENCOLAMIENTO DEALER - ORDENES");
            var resultOrder = await OrderUseCase.ExecuteAsync(cancellationToken);

            if (resultOrder.IsSuccess)
            {
                _logger.LogInformation(
                $"PROCESO CONSULTA API (SERVICELAYER) Y ENCOLAMIENTO DEALER - ORDENES COMPLETADO: {resultOrder.Message}. " +
                $"Encontradas: {resultOrder.ItemsFound}, Insertadas: {resultOrder.ItemsInserted}"
                );
            }
            else
            {
                _logger.LogError(
                    $"ERROR EN PROCESO CONSULTA API (SERVICELAYER) Y ENCOLAMIENTO DEALER - ORDENES: {resultOrder.Message}");
            }
        }
        #endregion

        #region "ORDENES CREACION Y ENVIO INTEGRADO HACIA API"
        private async Task ExecuteHanaToApiClienteOrderProcessAsync(IServiceScope scope, CancellationToken cancellationToken)
        {
            var ordersUseCase = scope.ServiceProvider.GetRequiredService<IProcesarClienteOrderSapUseCase>();
            _logger.LogInformation("EJECUTANDO PROCESO ENVIO API (SERVICELAYER) CLIENTE - ORDENES");
            var ordersResult = await ordersUseCase.ExecuteAsync(cancellationToken);

            if (ordersResult.IsSuccess)
            {
                _logger.LogInformation(
                    "PROCESO CREACION ORDEN/ENVIO API (SERVICELAYER) CLIENTE - ORDENES COMPLETADO: {Message}. Procesadas: {Processed}, Enviadas: {Sent}, Fallidas: {Failed}",
                    ordersResult.Message,
                    ordersResult.ItemsProcessed,
                    ordersResult.ItemsSent,
                    ordersResult.ItemsFailed);
            }
            else
            {
                _logger.LogError("ERROR EN PROCESO CREACION ORDEN/ENVIO API (SERVICELAYER) CLIENTE - ORDENES: {Error}", ordersResult.ErrorMessage);
            }
        }

        private async Task ExecuteHanaToApiDealerOrderProcessAsync(IServiceScope scope, CancellationToken cancellationToken)
        {
            var ordersUseCase = scope.ServiceProvider.GetRequiredService<IProcesarDealerOrderSapUseCase>();
            _logger.LogInformation("EJECUTANDO PROCESO ENVIO API (SERVICELAYER) DEALER - ORDENES");
            var ordersResult = await ordersUseCase.ExecuteAsync(cancellationToken);

            if (ordersResult.IsSuccess)
            {
                _logger.LogInformation(
                    "PROCESO CREACION ORDEN/ENVIO API (SERVICELAYER) DEALER - ORDENES COMPLETADO: {Message}. Procesadas: {Processed}, Enviadas: {Sent}, Fallidas: {Failed}",
                    ordersResult.Message,
                    ordersResult.ItemsProcessed,
                    ordersResult.ItemsSent,
                    ordersResult.ItemsFailed);
            }
            else
            {
                _logger.LogError("ERROR EN PROCESO CREACION ORDEN/ENVIO API (SERVICELAYER) DEALER - ORDENES: {Error}", ordersResult.ErrorMessage);
            }
        }
        #endregion
    }
}
