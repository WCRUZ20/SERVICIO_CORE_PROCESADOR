using Application.Commands.Items;
using Application.DTO;
using Application.Interfaces.HANA;
using Domain.Configuration;
using Domain.Helper;
using Domain.SAP;
using Infrastructure.Helper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using SAPbobsCOM;
using SAPbouiCOM;
using System.Data.Odbc;
using System.Transactions;
using static System.Runtime.InteropServices.JavaScript.JSType;


namespace Infrastructure.HANA
{
    /// <summary>
    /// Implementación del repositorio de HANA para almacenar y recuperar documentos SAP
    /// </summary>
    public class HanaRepository : IHanaRepository
    {
        private const string TransactionTypeCliente = "2";
        private const string TransactionTypeDealer = "1";

        private readonly IHanaConnectionFactory _connectionFactory;
        private readonly OdbcSettings _settings;
        private readonly ILogger<HanaRepository> _logger;

        private readonly ExecuteStoredProcedureHanaAsync _executeStoredProcedureHanaAsync;

        private readonly ResiliencePipeline<bool> _transactionResiliencia;
        private readonly ResiliencePipeline<IEnumerable<SapItemQueeDTO>> _queryResilienciaItems;
        private readonly ResiliencePipeline<SapItemDetailDTO?> _queryResilienciaItemDetail;

        public HanaRepository(IHanaConnectionFactory connectionFactory, IOptions<OdbcSettings> settings, ILogger<HanaRepository> logger, ExecuteStoredProcedureHanaAsync ejecutarSPDinamico)
        {
            _connectionFactory = connectionFactory;
            _settings = settings.Value;
            _logger = logger;
            _executeStoredProcedureHanaAsync = ejecutarSPDinamico;

            // Inicializar pipelines de resiliencia
            _transactionResiliencia = HanaResiliencePipeline.Create<bool>(logger);
            _queryResilienciaItems = HanaResiliencePipeline.Create<IEnumerable<SapItemQueeDTO>>(logger);
            _queryResilienciaItemDetail = HanaResiliencePipeline.Create<SapItemDetailDTO?>(logger);
        }

        #region "ARTICULOS"
        public async Task<IEnumerable<SapItemQueeDTO>> GetPendingClienteItemsTypeAsync(CancellationToken cancellationToken = default)
        {
            //return GetPendingItemsTypeAsync(TransactionTypeCliente, cancellationToken);
            return await _queryResilienciaItems.ExecuteAsync(async (ct) =>
            {

                using var connection = await _connectionFactory.CreateConnectionAsync(ct);
                await connection.OpenAsync(ct);

                var parameters = new Dictionary<string, object>
                {
                    { "filtro_1", "ITMP" },
                    { "filtro_2", "" },
                    { "filtro_3", Transaction.Item },
                    { "filtro_4", TransactionTypeCliente },
                    { "filtro_5", "" }
                };

                var itemsType = await _executeStoredProcedureHanaAsync.ExecuteStoredProcedureQueryAsync<SapItemQueeDTO>(
                    connection,
                    "sp_integracion_sap_woo_consultas",
                    parameters,
                    ct);

                _logger.LogInformation(
                    "Se obtuvieron {Count} articulos pendientes desde HANA para TransactionType={TransactionType}",
                    itemsType.Count(),
                    TransactionTypeCliente);

                return itemsType;
            }, cancellationToken);
        }

        public async Task<IEnumerable<SapItemQueeDTO>> GetPendingDealerItemsTypeAsync(CancellationToken cancellationToken = default)
        {
            //return GetPendingItemsTypeAsync(TransactionTypeDealer, cancellationToken);
            return await _queryResilienciaItems.ExecuteAsync(async (ct) =>
            {

                using var connection = await _connectionFactory.CreateConnectionAsync(ct);
                await connection.OpenAsync(ct);

                var parameters = new Dictionary<string, object>
                {
                    { "filtro_1", "ITMP" },
                    { "filtro_2", "" },
                    { "filtro_3", Transaction.Item },
                    { "filtro_4", TransactionTypeDealer },
                    { "filtro_5", "" }
                };

                var itemsType = await _executeStoredProcedureHanaAsync.ExecuteStoredProcedureQueryAsync<SapItemQueeDTO>(
                    connection,
                    "sp_integracion_sap_woo_consultas",
                    parameters,
                    ct);

                _logger.LogInformation(
                    "Se obtuvieron {Count} articulos pendientes desde HANA para TransactionType={TransactionType}",
                    itemsType.Count(),
                    TransactionTypeDealer);

                return itemsType;
            }, cancellationToken);
        }

        public async Task<SapItemDetailDTO?> GetItemDetailByItemCodeAsync(ItemDestinationType destinationType, string itemCode, string bodega, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(itemCode))
            {
                _logger.LogWarning("ItemCode vacío al consultar detalle de artículo.");
                return null;
            }

            return await _queryResilienciaItemDetail.ExecuteAsync(async (ct) =>
            {
                using var connection = await _connectionFactory.CreateConnectionAsync(ct);
                await connection.OpenAsync(ct);

                var parameters = new Dictionary<string, object>
                {
                    { "filtro_1", "ITMD" },
                    { "filtro_2", itemCode },
                    { "filtro_3", Transaction.Item},
                    { "filtro_4", destinationType},
                    { "filtro_5", bodega}
                };

                var details = await _executeStoredProcedureHanaAsync.ExecuteStoredProcedureQueryAsync<SapItemDetailDTO>(
                    connection,
                    "sp_integracion_sap_woo_consultas",
                    parameters,
                    ct);

                var detail = details.FirstOrDefault();

                if (detail == null)
                {
                    _logger.LogWarning("No se encontró detalle de artículo para ItemCode={ItemCode}", itemCode);
                    return null;
                }

                _logger.LogInformation("Detalle de artículo obtenido para ItemCode={ItemCode}", itemCode);
                return detail;
            }, cancellationToken);
        }

        public async Task<bool> ExistsItemAsync(int transaction, string docEntry, string bodega, CancellationToken cancellationToken = default)
        {
            return await _transactionResiliencia.ExecuteAsync(async (ct) =>
            {
                var itemQuery = $"Articulo:{docEntry} Transaction:{transaction}";
                using var connection = await _connectionFactory.CreateConnectionAsync(ct);
                await connection.OpenAsync(ct);

                var parameters = new Dictionary<string, object>
                {
                    { "filtro_1", "ITME" },
                    { "filtro_2", docEntry },
                    { "filtro_3", Transaction.Item.ToString() },
                    { "filtro_4", transaction },
                    { "filtro_5", bodega }
                };

                var _listaItems = await _executeStoredProcedureHanaAsync.ExecuteStoredProcedureQueryAsync<SapItemQueeDTO>(
                    connection,
                    "sp_integracion_sap_woo_consultas",
                    parameters,
                    ct);
                var _items = _listaItems.FirstOrDefault();
                if (_items == null || _items.SapDocEntry == "0")
                {

                    _logger.LogInformation($"Articulo {itemQuery} NO existe en cola.");
                    return false;
                }
                else
                {
                    _logger.LogInformation(
                        $"Articulo {itemQuery} Ya existe en cola con el estado: {_items.Status}");
                    return true;
                }

            }, cancellationToken);

        }

        public async Task<bool> InsertItemAsync(SapItemQueeDTO item, CancellationToken cancellationToken = default)
        {
            return await _transactionResiliencia.ExecuteAsync(async (ct) =>
            {

                using var connection = await _connectionFactory.CreateConnectionAsync(ct);
                await connection.OpenAsync(ct);

                var parameters = new Dictionary<string, object> {
                    { "filtro_1", "ITMI" },
                    { "filtro_2", "" }, //"Code"
                    { "filtro_3", "" }, //"Name"
                    { "filtro_4", item.Transaction }, //"Transactions"
                    { "filtro_5", item.TransactionType }, //"TransactionType"
                    { "filtro_6", item.SapDocEntry }, //"SapDocEntry"
                    { "filtro_7", item.SapDocNum }, //"SapDocNum"
                    { "filtro_8", item.SapDocStatus }, //"SapDocStatus"
                    { "filtro_9", "" }, //"Json"
                    { "filtro_10", "" }, //"CreatedBy"
                    { "filtro_11", "" }, //"UpdatedBy"
                    { "filtro_12", "" }, //"Comments"
                    { "filtro_13", StatusHanaDocumentLevel.Inserted}, //"Status"
                    { "filtro_14", item.Stock },
                    { "filtro_15", ""},
                    { "filtro_16", item.regular_price },
                    { "filtro_17", item.Bodega  },
                    { "ResultFlag", 0 } //"ResultFlag"
                };

                var result = await _executeStoredProcedureHanaAsync.ExecuteStoredProcedureTransactionAsync(
                connection,
                "sp_integracion_sap_woo_transactions",
                parameters,
                null,
                ct);

                if (!result)
                {
                    _logger.LogInformation($"Articulo NO insertado : {item.SapDocEntry} ");

                }

                return (result);
            }, cancellationToken);
        }
                
        public async Task<IEnumerable<SapItemQueeDTO>> GetPendingClienteItemsAsync<TResult>(CancellationToken cancellationToken = default) where TResult : class, new()
        {
            //return GetPendingItemsAsync<TResult>(TransactionTypeCliente, cancellationToken);
            return await _queryResilienciaItems.ExecuteAsync(async (ct) =>
            {

                using var connection = await _connectionFactory.CreateConnectionAsync(ct);
                await connection.OpenAsync(ct);

                var parameters = new Dictionary<string, object>
                {
                    { "filtro_1", "PITM" },
                    { "filtro_2", "" },
                    { "filtro_3", Transaction.Item },
                    { "filtro_4", TransactionTypeCliente.ToString() },
                    { "filtro_5", "" }
                };

                var items = await _executeStoredProcedureHanaAsync.ExecuteStoredProcedureQueryAsync<SapItemQueeDTO>(
                    connection,
                    "sp_integracion_sap_woo_consultas",
                    parameters,
                    ct);

                return items;
            }, cancellationToken);
        }

        public async Task<IEnumerable<SapItemQueeDTO>> GetPendingDealerItemsAsync<TResult>(CancellationToken cancellationToken = default) where TResult : class, new()
        {
            //return GetPendingItemsAsync<TResult>(TransactionTypeDealer, cancellationToken);
            return await _queryResilienciaItems.ExecuteAsync(async (ct) =>
            {

                using var connection = await _connectionFactory.CreateConnectionAsync(ct);
                await connection.OpenAsync(ct);

                var parameters = new Dictionary<string, object>
                {
                    { "filtro_1", "PITM" },
                    { "filtro_2", "" },
                    { "filtro_3", Transaction.Item },
                    { "filtro_4", TransactionTypeDealer.ToString() },
                    { "filtro_5", "" }
                };

                var items = await _executeStoredProcedureHanaAsync.ExecuteStoredProcedureQueryAsync<SapItemQueeDTO>(
                    connection,
                    "sp_integracion_sap_woo_consultas",
                    parameters,
                    ct);

                return items;
            }, cancellationToken);
        }

        public async Task<bool> MarkStatusItemAsAsync(SapItemQueeDTO item, CancellationToken cancellationToken = default)
        {
            return await _transactionResiliencia.ExecuteAsync(async (ct) =>
            {
                using var connection = await _connectionFactory.CreateConnectionAsync(ct);
                await connection.OpenAsync(ct);

                var parameters = new Dictionary<string, object>
                    {
                        { "filtro_1", "ARIT" },
                        { "filtro_2", "" },
                        { "filtro_3", "" },
                        { "filtro_4", item.Transaction },
                        { "filtro_5", item.TransactionType},
                        { "filtro_6", item.SapDocEntry },
                        { "filtro_7", item.SapDocNum },
                        { "filtro_8", ""},
                        { "filtro_9", item.Json },
                        { "filtro_10", "" },
                        { "filtro_11", "" },
                        { "filtro_12", "" },
                        { "filtro_13", item.Status },
                        { "filtro_14", item.Stock},
                        { "filtro_15", item.idWoo},
                        { "filtro_16", 0},
                        { "filtro_17", item.Bodega },
                        { "ResultFlag", 0 }
                    };

                var result = await _executeStoredProcedureHanaAsync.ExecuteStoredProcedureTransactionAsync(
                    connection,
                    "sp_integracion_sap_woo_transactions",
                    parameters,
                    null,
                    ct);

                if (!result)
                {
                    _logger.LogInformation($"Articulo NO actualizado : {item.SapDocEntry} ");
                }

                return result;
            }, cancellationToken);
        }
        #endregion

        #region "STOCK"
      
        public async Task<bool> InsertStockAsync(SapItemQueeDTO item, CancellationToken cancellationToken = default)
        {
            return await _transactionResiliencia.ExecuteAsync(async (ct) =>
            {

                using var connection = await _connectionFactory.CreateConnectionAsync(ct);
                await connection.OpenAsync(ct);

                var parameters = new Dictionary<string, object> {
                    { "filtro_1", "ITMS" },
                    { "filtro_2", "" }, //"Code"
                    { "filtro_3", "" }, //"Name"
                    { "filtro_4", item.Transaction }, //"Transactions"
                    { "filtro_5", item.TransactionType }, //"TransactionType"
                    { "filtro_6", item.SapDocEntry }, //"SapDocEntry"
                    { "filtro_7", item.SapDocNum }, //"SapDocNum"
                    { "filtro_8", item.SapDocStatus }, //"SapDocStatus"
                    { "filtro_9", "" }, //"Json"
                    { "filtro_10", "" }, //"CreatedBy"
                    { "filtro_11", "" }, //"UpdatedBy"
                    { "filtro_12", "" }, //"Comments"
                    { "filtro_13", StatusHanaDocumentLevel.Inserted}, //"Status"
                    { "filtro_14", item.Stock },
                    { "filtro_15", ""},
                    { "filtro_16", 0},
                    { "filtro_17", item.Bodega},
                    { "ResultFlag", 0 } //"ResultFlag"
                };

                var result = await _executeStoredProcedureHanaAsync.ExecuteStoredProcedureTransactionAsync(
                connection,
                "sp_integracion_sap_woo_transactions",
                parameters,
                null,
                ct);

                if (!result)
                {
                    _logger.LogInformation($"Articulo NO insertado : {item.SapDocEntry} ");

                }

                return (result);
            }, cancellationToken);
        }

        public async Task<IEnumerable<SapItemQueeDTO>> GetUpdateDealerItemsTypeAsync(CancellationToken cancellationToken = default)
        {
            //return GetPendingItemsTypeAsync(TransactionTypeDealer, cancellationToken);
            return await _queryResilienciaItems.ExecuteAsync(async (ct) =>
            {

                using var connection = await _connectionFactory.CreateConnectionAsync(ct);
                await connection.OpenAsync(ct);

                var parameters = new Dictionary<string, object>
                {
                    { "filtro_1", "ITMU" },
                    { "filtro_2",  ""},
                    { "filtro_3", Transaction.Stock },
                    { "filtro_4", TransactionTypeDealer},
                    { "filtro_5", "" }
                };

                var itemsType = await _executeStoredProcedureHanaAsync.ExecuteStoredProcedureQueryAsync<SapItemQueeDTO>(
                    connection,
                    "sp_integracion_sap_woo_consultas",
                    parameters,
                    ct);

                _logger.LogInformation(
                    "Se obtuvieron {Count} stock pendientes desde HANA para TransactionType={TransactionType}",
                    itemsType.Count(),
                    TransactionTypeDealer);

                return itemsType;
            }, cancellationToken);
        }

        public async Task<IEnumerable<SapItemQueeDTO>> GetUpdateClienteItemsTypeAsync(CancellationToken cancellationToken = default)
        {
            //return GetPendingItemsTypeAsync(TransactionTypeDealer, cancellationToken);
            return await _queryResilienciaItems.ExecuteAsync(async (ct) =>
            {

                using var connection = await _connectionFactory.CreateConnectionAsync(ct);
                await connection.OpenAsync(ct);

                var parameters = new Dictionary<string, object>
                {
                    { "filtro_1", "ITMU" },
                    { "filtro_2",  ""},
                    { "filtro_3", Transaction.Stock },
                    { "filtro_4", TransactionTypeCliente},
                    { "filtro_5", "" }
                };

                var itemsType = await _executeStoredProcedureHanaAsync.ExecuteStoredProcedureQueryAsync<SapItemQueeDTO>(
                    connection,
                    "sp_integracion_sap_woo_consultas",
                    parameters,
                    ct);

                _logger.LogInformation(
                    "Se obtuvieron {Count} stock pendientes desde HANA para TransactionType={TransactionType}",
                    itemsType.Count(),
                    TransactionTypeCliente);

                return itemsType;
            }, cancellationToken);
        }

        public async Task<bool> MarkStatusStockAsAsync(SapItemQueeDTO item, CancellationToken cancellationToken = default)
        {
            return await _transactionResiliencia.ExecuteAsync(async (ct) =>
            {
                using var connection = await _connectionFactory.CreateConnectionAsync(ct);
                await connection.OpenAsync(ct);

                var parameters = new Dictionary<string, object>
                    {
                        { "filtro_1", "SRIT" },
                        { "filtro_2", "" },
                        { "filtro_3", "" },
                        { "filtro_4", item.Transaction },
                        { "filtro_5", item.TransactionType},
                        { "filtro_6", item.SapDocEntry },
                        { "filtro_7", item.SapDocNum },
                        { "filtro_8", ""},
                        { "filtro_9", item.Json },
                        { "filtro_10", "" },
                        { "filtro_11", "" },
                        { "filtro_12", "" },
                        { "filtro_13", item.Status },
                        { "filtro_14", item.Stock},
                        { "filtro_15", item.idWoo},
                        { "filtro_16", 0},
                        { "filtro_17", item.Bodega },
                        { "ResultFlag", 0 }
                    };

                var result = await _executeStoredProcedureHanaAsync.ExecuteStoredProcedureTransactionAsync(
                    connection,
                    "sp_integracion_sap_woo_transactions",
                    parameters,
                    null,
                    ct);

                if (!result)
                {
                    _logger.LogInformation($"Stock NO actualizado : {item.SapDocEntry} ");
                }

                return result;
            }, cancellationToken);
        }

        public async Task<SapItemDetailDTO?> GetStockDetailByItemCodeAsync(Application.Commands.Stock.ItemDestinationType destinationType, string itemCode, string bodega, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(itemCode))
            {
                _logger.LogWarning("ItemCode vacío al consultar detalle de artículo.");
                return null;
            }

            return await _queryResilienciaItemDetail.ExecuteAsync(async (ct) =>
            {
                using var connection = await _connectionFactory.CreateConnectionAsync(ct);
                await connection.OpenAsync(ct);

                var parameters = new Dictionary<string, object>
                {
                    { "filtro_1", "SITM" },
                    { "filtro_2", itemCode },
                    { "filtro_3", Transaction.Stock},
                    { "filtro_4", destinationType},
                    { "filtro_5", bodega}
                };

                var details = await _executeStoredProcedureHanaAsync.ExecuteStoredProcedureQueryAsync<SapItemDetailDTO>(
                    connection,
                    "sp_integracion_sap_woo_consultas",
                    parameters,
                    ct);

                var detail = details.FirstOrDefault();

                if (detail == null)
                {
                    _logger.LogWarning("No se encontró detalle de stock para ItemCode={ItemCode}", itemCode);
                    return null;
                }

                _logger.LogInformation("Detalle de artículo obtenido para ItemCode={ItemCode}", itemCode);
                return detail;
            }, cancellationToken);
        }

        public async Task<IEnumerable<SapItemQueeDTO>> GetPendingClienteStockAsync<TResult>(CancellationToken cancellationToken = default) where TResult : class, new()
        {
            //return GetPendingItemsAsync<TResult>(TransactionTypeCliente, cancellationToken);
            return await _queryResilienciaItems.ExecuteAsync(async (ct) =>
            {

                using var connection = await _connectionFactory.CreateConnectionAsync(ct);
                await connection.OpenAsync(ct);

                var parameters = new Dictionary<string, object>
                {
                    { "filtro_1", "PITMS" },
                    { "filtro_2", "" },
                    { "filtro_3", Transaction.Stock },
                    { "filtro_4", TransactionTypeCliente.ToString() },
                    { "filtro_5", "" }
                };

                var items = await _executeStoredProcedureHanaAsync.ExecuteStoredProcedureQueryAsync<SapItemQueeDTO>(
                    connection,
                    "sp_integracion_sap_woo_consultas",
                    parameters,
                    ct);

                return items;
            }, cancellationToken);
        }

        public async Task<IEnumerable<SapItemQueeDTO>> GetPendingDealerStockAsync<TResult>(CancellationToken cancellationToken = default) where TResult : class, new()
        {
            //return GetPendingItemsAsync<TResult>(TransactionTypeCliente, cancellationToken);
            return await _queryResilienciaItems.ExecuteAsync(async (ct) =>
            {

                using var connection = await _connectionFactory.CreateConnectionAsync(ct);
                await connection.OpenAsync(ct);

                var parameters = new Dictionary<string, object>
                {
                    { "filtro_1", "PITMS" },
                    { "filtro_2", "" },
                    { "filtro_3", Transaction.Stock },
                    { "filtro_4", TransactionTypeDealer.ToString() },
                    { "filtro_5", "" }
                };

                var items = await _executeStoredProcedureHanaAsync.ExecuteStoredProcedureQueryAsync<SapItemQueeDTO>(
                    connection,
                    "sp_integracion_sap_woo_consultas",
                    parameters,
                    ct);

                return items;
            }, cancellationToken);
        }
        #endregion

        #region "PRECIO"

        public async Task<bool> InsertPrecioAsync(SapItemQueeDTO item, CancellationToken cancellationToken = default)
        {
            return await _transactionResiliencia.ExecuteAsync(async (ct) =>
            {

                using var connection = await _connectionFactory.CreateConnectionAsync(ct);
                await connection.OpenAsync(ct);

                var parameters = new Dictionary<string, object> {
                    { "filtro_1", "PRCI" },
                    { "filtro_2", "" }, //"Code"
                    { "filtro_3", "" }, //"Name"
                    { "filtro_4", item.Transaction }, //"Transactions"
                    { "filtro_5", item.TransactionType }, //"TransactionType"
                    { "filtro_6", item.SapDocEntry }, //"SapDocEntry"
                    { "filtro_7", item.SapDocNum }, //"SapDocNum"
                    { "filtro_8", item.SapDocStatus }, //"SapDocStatus"
                    { "filtro_9", "" }, //"Json"
                    { "filtro_10", "" }, //"CreatedBy"
                    { "filtro_11", "" }, //"UpdatedBy"
                    { "filtro_12", "" }, //"Comments"
                    { "filtro_13", StatusHanaDocumentLevel.Inserted}, //"Status"
                    { "filtro_14", 0 },
                    { "filtro_15", ""},
                    { "filtro_16", item.regular_price},
                    { "filtro_17", item.Bodega},
                    { "ResultFlag", 0 } //"ResultFlag"
                };

                var result = await _executeStoredProcedureHanaAsync.ExecuteStoredProcedureTransactionAsync(
                connection,
                "sp_integracion_sap_woo_transactions",
                parameters,
                null,
                ct);

                if (!result)
                {
                    _logger.LogInformation($"Precio NO insertado : {item.SapDocEntry} ");

                }

                return (result);
            }, cancellationToken);
        }

        public async Task<IEnumerable<SapItemQueeDTO>> GetUpdateDealerPrecioTypeAsync(CancellationToken cancellationToken = default)
        {
            //return GetPendingItemsTypeAsync(TransactionTypeDealer, cancellationToken);
            return await _queryResilienciaItems.ExecuteAsync(async (ct) =>
            {

                using var connection = await _connectionFactory.CreateConnectionAsync(ct);
                await connection.OpenAsync(ct);

                var parameters = new Dictionary<string, object>
                {
                    { "filtro_1", "PRCU" },
                    { "filtro_2",  ""},
                    { "filtro_3", Transaction.Price },
                    { "filtro_4", TransactionTypeDealer},
                    { "filtro_5", "" }
                };

                var itemsType = await _executeStoredProcedureHanaAsync.ExecuteStoredProcedureQueryAsync<SapItemQueeDTO>(
                    connection,
                    "sp_integracion_sap_woo_consultas",
                    parameters,
                    ct);

                _logger.LogInformation(
                    "Se obtuvieron {Count} precio pendientes desde HANA para TransactionType={TransactionType}",
                    itemsType.Count(),
                    TransactionTypeDealer);

                return itemsType;
            }, cancellationToken);
        }

        public async Task<IEnumerable<SapItemQueeDTO>> GetUpdateClientePrecioTypeAsync(CancellationToken cancellationToken = default)
        {
            //return GetPendingItemsTypeAsync(TransactionTypeDealer, cancellationToken);
            return await _queryResilienciaItems.ExecuteAsync(async (ct) =>
            {

                using var connection = await _connectionFactory.CreateConnectionAsync(ct);
                await connection.OpenAsync(ct);

                var parameters = new Dictionary<string, object>
                {
                    { "filtro_1", "PRCU" },
                    { "filtro_2",  ""},
                    { "filtro_3", Transaction.Price },
                    { "filtro_4", TransactionTypeCliente},
                    { "filtro_5", "" }
                };

                var itemsType = await _executeStoredProcedureHanaAsync.ExecuteStoredProcedureQueryAsync<SapItemQueeDTO>(
                    connection,
                    "sp_integracion_sap_woo_consultas",
                    parameters,
                    ct);

                _logger.LogInformation(
                    "Se obtuvieron {Count} precio pendientes desde HANA para TransactionType={TransactionType}",
                    itemsType.Count(),
                    TransactionTypeCliente);

                return itemsType;
            }, cancellationToken);
        }

        public async Task<bool> MarkStatusPrecioAsAsync(SapItemQueeDTO item, CancellationToken cancellationToken = default)
        {
            return await _transactionResiliencia.ExecuteAsync(async (ct) =>
            {
                using var connection = await _connectionFactory.CreateConnectionAsync(ct);
                await connection.OpenAsync(ct);

                var parameters = new Dictionary<string, object>
                    {
                        { "filtro_1", "PRCM" },
                        { "filtro_2", "" },
                        { "filtro_3", "" },
                        { "filtro_4", item.Transaction },
                        { "filtro_5", item.TransactionType},
                        { "filtro_6", item.SapDocEntry },
                        { "filtro_7", item.SapDocNum },
                        { "filtro_8", ""},
                        { "filtro_9", item.Json },
                        { "filtro_10", "" },
                        { "filtro_11", "" },
                        { "filtro_12", "" },
                        { "filtro_13", item.Status },
                        { "filtro_14", item.Stock},
                        { "filtro_15", item.idWoo},
                        { "filtro_16", item.regular_price},
                        { "filtro_17", item.Bodega },
                        { "ResultFlag", 0 }
                    };

                var result = await _executeStoredProcedureHanaAsync.ExecuteStoredProcedureTransactionAsync(
                    connection,
                    "sp_integracion_sap_woo_transactions",
                    parameters,
                    null,
                    ct);

                if (!result)
                {
                    _logger.LogInformation($"Precio NO actualizado : {item.SapDocEntry} ");
                }

                return result;
            }, cancellationToken);
        }

        public async Task<SapItemDetailDTO?> GetPrecioDetailByItemCodeAsync(Application.Commands.Precio.ItemDestinationType destinationType, string itemCode, string bodega, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(itemCode))
            {
                _logger.LogWarning("ItemCode vacío al consultar detalle de artículo.");
                return null;
            }

            return await _queryResilienciaItemDetail.ExecuteAsync(async (ct) =>
            {
                using var connection = await _connectionFactory.CreateConnectionAsync(ct);
                await connection.OpenAsync(ct);

                var parameters = new Dictionary<string, object>
                {
                    { "filtro_1", "PRCD" },
                    { "filtro_2", itemCode },
                    { "filtro_3", Transaction.Price},
                    { "filtro_4", destinationType},
                    { "filtro_5", bodega}
                };

                var details = await _executeStoredProcedureHanaAsync.ExecuteStoredProcedureQueryAsync<SapItemDetailDTO>(
                    connection,
                    "sp_integracion_sap_woo_consultas",
                    parameters,
                    ct);

                var detail = details.FirstOrDefault();

                if (detail == null)
                {
                    _logger.LogWarning("No se encontró detalle de precio para ItemCode={ItemCode}", itemCode);
                    return null;
                }

                _logger.LogInformation("Detalle de precio obtenido para ItemCode={ItemCode}", itemCode);
                return detail;
            }, cancellationToken);
        }

        public async Task<IEnumerable<SapItemQueeDTO>> GetPendingClientePrecioAsync<TResult>(CancellationToken cancellationToken = default) where TResult : class, new()
        {
            //return GetPendingItemsAsync<TResult>(TransactionTypeCliente, cancellationToken);
            return await _queryResilienciaItems.ExecuteAsync(async (ct) =>
            {

                using var connection = await _connectionFactory.CreateConnectionAsync(ct);
                await connection.OpenAsync(ct);

                var parameters = new Dictionary<string, object>
                {
                    { "filtro_1", "PRCP" },
                    { "filtro_2", "" },
                    { "filtro_3", Transaction.Price },
                    { "filtro_4", TransactionTypeCliente},
                    { "filtro_5", "" }
                };

                var items = await _executeStoredProcedureHanaAsync.ExecuteStoredProcedureQueryAsync<SapItemQueeDTO>(
                    connection,
                    "sp_integracion_sap_woo_consultas",
                    parameters,
                    ct);

                return items;
            }, cancellationToken);
        }

        public async Task<IEnumerable<SapItemQueeDTO>> GetPendingDealerPrecioAsync<TResult>(CancellationToken cancellationToken = default) where TResult : class, new()
        {
            //return GetPendingItemsAsync<TResult>(TransactionTypeCliente, cancellationToken);
            return await _queryResilienciaItems.ExecuteAsync(async (ct) =>
            {

                using var connection = await _connectionFactory.CreateConnectionAsync(ct);
                await connection.OpenAsync(ct);

                var parameters = new Dictionary<string, object>
                {
                    { "filtro_1", "PRCP" },
                    { "filtro_2", "" },
                    { "filtro_3", Transaction.Price },
                    { "filtro_4", TransactionTypeDealer.ToString() },
                    { "filtro_5", "" }
                };

                var items = await _executeStoredProcedureHanaAsync.ExecuteStoredProcedureQueryAsync<SapItemQueeDTO>(
                    connection,
                    "sp_integracion_sap_woo_consultas",
                    parameters,
                    ct);

                return items;
            }, cancellationToken);
        }
        #endregion

        #region "ORDENES"
        public async Task<bool> InsertOrdenAsync(SapItemQueeDTO orden, CancellationToken cancellationToken = default)
        {
            return await _transactionResiliencia.ExecuteAsync(async (ct) =>
            {

                using var connection = await _connectionFactory.CreateConnectionAsync(ct);
                await connection.OpenAsync(ct);

                var parameters = new Dictionary<string, object> {
                    { "filtro_1", "ORDI" },
                    { "filtro_2", "" }, //"Code"
                    { "filtro_3", "" }, //"Name"
                    { "filtro_4", orden.Transaction }, //"Transactions"
                    { "filtro_5", orden.TransactionType }, //"TransactionType"
                    { "filtro_6", ""}, //"SapDocEntry"
                    { "filtro_7", "" }, //"SapDocNum"
                    { "filtro_8", orden.SapDocStatus }, //"SapDocStatus"
                    { "filtro_9", orden.Json }, //"Json"
                    { "filtro_10", "" }, //"CreatedBy"
                    { "filtro_11", "" }, //"UpdatedBy"
                    { "filtro_12", "" }, //"Comments"
                    { "filtro_13", StatusHanaDocumentLevel.Inserted}, //"Status"
                    { "filtro_14", 0 },
                    { "filtro_15", orden.idWoo },
                    { "filtro_16", 0 },
                    { "filtro_17", "" },
                    { "ResultFlag", 0 } //"ResultFlag"
                };

                var result = await _executeStoredProcedureHanaAsync.ExecuteStoredProcedureTransactionAsync(
                connection,
                "sp_integracion_sap_woo_transactions",
                parameters,
                null,
                ct);

                if (!result)
                {
                    _logger.LogInformation($"Articulo NO insertado : {orden.idWoo} ");

                }

                return (result);
            }, cancellationToken);
        }

        public async Task<bool> ExistsOrderAsync(int transaction, string idWoo, string transaction_type, CancellationToken cancellationToken = default)
        {
            return await _transactionResiliencia.ExecuteAsync(async (ct) =>
            {
                var OrdenQuery = $"Orden:{idWoo} Transaction:{transaction}";
                using var connection = await _connectionFactory.CreateConnectionAsync(ct);
                await connection.OpenAsync(ct);

                var parameters = new Dictionary<string, object>
                {
                    { "filtro_1", "ODRE" },
                    { "filtro_2", idWoo },
                    { "filtro_3", transaction_type},
                    { "filtro_4", transaction },
                    { "filtro_5", "" }
                };

                var _listaOrdenes = await _executeStoredProcedureHanaAsync.ExecuteStoredProcedureQueryAsync<SapItemQueeDTO>(
                    connection,
                    "sp_integracion_sap_woo_consultas",
                    parameters,
                    ct);
                var _Ordenes = _listaOrdenes.FirstOrDefault();
                if (_Ordenes == null || _Ordenes.idWoo == "0")
                {
                    _logger.LogInformation($"Orden {OrdenQuery} NO existe en cola.");
                    return false;
                }
                else
                {
                    _logger.LogInformation(
                        $"Orden {OrdenQuery} Ya existe en cola con el estado: {_Ordenes.Status}");
                    return true;
                }

            }, cancellationToken);

        }

        public async Task<IEnumerable<SapItemQueeDTO>> GetPendingClienteOrdersAsync<TResult>(CancellationToken cancellationToken = default) where TResult : class, new()
        {
            //return GetPendingItemsAsync<TResult>(TransactionTypeCliente, cancellationToken);
            return await _queryResilienciaItems.ExecuteAsync(async (ct) =>
            {

                using var connection = await _connectionFactory.CreateConnectionAsync(ct);
                await connection.OpenAsync(ct);

                var parameters = new Dictionary<string, object>
                {
                    { "filtro_1", "ODRP" },
                    { "filtro_2", "" },
                    { "filtro_3", TransactionTypeCliente.ToString() },
                    { "filtro_4", Transaction.Order },
                    { "filtro_5", "" }
                };

                var items = await _executeStoredProcedureHanaAsync.ExecuteStoredProcedureQueryAsync<SapItemQueeDTO>(
                    connection,
                    "sp_integracion_sap_woo_consultas",
                    parameters,
                    ct);

                return items;
            }, cancellationToken);
        }

        public async Task<IEnumerable<SapItemQueeDTO>> GetPendingDealerOrdersAsync<TResult>(CancellationToken cancellationToken = default) where TResult : class, new()
        {
            //return GetPendingItemsAsync<TResult>(TransactionTypeCliente, cancellationToken);
            return await _queryResilienciaItems.ExecuteAsync(async (ct) =>
            {

                using var connection = await _connectionFactory.CreateConnectionAsync(ct);
                await connection.OpenAsync(ct);

                var parameters = new Dictionary<string, object>
                {
                    { "filtro_1", "ODRP" },
                    { "filtro_2", "" },
                    { "filtro_3", TransactionTypeDealer.ToString() },
                    { "filtro_4", Transaction.Order },
                    { "filtro_5", "" }
                };

                var items = await _executeStoredProcedureHanaAsync.ExecuteStoredProcedureQueryAsync<SapItemQueeDTO>(
                    connection,
                    "sp_integracion_sap_woo_consultas",
                    parameters,
                    ct);

                return items;
            }, cancellationToken);
        }

        public async Task<bool> MarkStatusOrderAsAsync(SapItemQueeDTO item, CancellationToken cancellationToken = default)
        {
            return await _transactionResiliencia.ExecuteAsync(async (ct) =>
            {
                using var connection = await _connectionFactory.CreateConnectionAsync(ct);
                await connection.OpenAsync(ct);

                var parameters = new Dictionary<string, object>
                    {
                        { "filtro_1", "ODRM" },
                        { "filtro_2", "" },
                        { "filtro_3", "" },
                        { "filtro_4", item.Transaction },
                        { "filtro_5", item.TransactionType},
                        { "filtro_6", "" },
                        { "filtro_7", "" },
                        { "filtro_8", "" },
                        { "filtro_9", "" },
                        { "filtro_10", "" },
                        { "filtro_11", "" },
                        { "filtro_12", "" },
                        { "filtro_13", item.Status },
                        { "filtro_14", 0},
                        { "filtro_15", item.idWoo},
                        { "filtro_16", 0},
                        { "filtro_17", "" },
                        { "ResultFlag", 0 }
                    };

                var result = await _executeStoredProcedureHanaAsync.ExecuteStoredProcedureTransactionAsync(
                    connection,
                    "sp_integracion_sap_woo_transactions",
                    parameters,
                    null,
                    ct);

                if (!result)
                {
                    _logger.LogInformation($"Articulo NO actualizado : {item.SapDocEntry} ");
                }

                return result;
            }, cancellationToken);
        }
        #endregion
    }

    public enum Transaction
    {
        Item = 1,
        BusinessPartner = 2,
        Stock = 3,
        Order = 4,
        Price = 5
    }
}




