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
        private const string TransactionTypeCliente = "1";
        private const string TransactionTypeDealer = "2";

        private readonly IHanaConnectionFactory _connectionFactory;
        private readonly OdbcSettings _settings;
        private readonly ILogger<HanaRepository> _logger;

        private readonly ExecuteStoredProcedureHanaAsync _executeStoredProcedureHanaAsync;

        private readonly ResiliencePipeline<bool> _transactionResiliencia;
        private readonly ResiliencePipeline<IEnumerable<SapDrivinTable>> _queryResiliencia;
        private readonly ResiliencePipeline<IEnumerable<SapItemsTable>> _queryResilienciaItems;
        private readonly ResiliencePipeline<SapItemDetailDTO?> _queryResilienciaItemDetail;
        //private readonly ResiliencePipeline<IEnumerable<SapDrivinTable>> _sapDrivinTablePipeline;
        //private readonly ResiliencePipeline<bool> _boolPipeline;

        public HanaRepository(
            IHanaConnectionFactory connectionFactory,
            IOptions<OdbcSettings> settings,
            ILogger<HanaRepository> logger,
            ExecuteStoredProcedureHanaAsync ejecutarSPDinamico)
        {
            _connectionFactory = connectionFactory;
            _settings = settings.Value;
            _logger = logger;
            _executeStoredProcedureHanaAsync = ejecutarSPDinamico;


            // Inicializar pipelines de resiliencia
            _transactionResiliencia = HanaResiliencePipeline.Create<bool>(logger);
            _queryResiliencia = HanaResiliencePipeline.Create<IEnumerable<SapDrivinTable>>(logger);
            _queryResilienciaItems = HanaResiliencePipeline.Create<IEnumerable<SapItemsTable>>(logger);
            _queryResilienciaItemDetail = HanaResiliencePipeline.Create<SapItemDetailDTO?>(logger);
            //_boolPipeline = HanaResiliencePipeline.Create<bool>(logger);
        }

        public async Task<IEnumerable<SapDrivinTable>> GetPendingDocumentsTypeAsync(
            CancellationToken cancellationToken = default)
        {
            return await _queryResiliencia.ExecuteAsync(async (ct) =>
            {
              
                using var connection = await _connectionFactory.CreateConnectionAsync(ct);
                await connection.OpenAsync(ct);

                var parameters = new Dictionary<string, object>
                {
                    { "filtro_1", "ODCP" },
                    { "filtro_2", "" },
                    { "filtro_3", "" },
                    { "filtro_4", "" },
                    { "filtro_5", "" }
                };

                var documentsType = await _executeStoredProcedureHanaAsync.ExecuteStoredProcedureQueryAsync<SapDrivinTable>(
                    connection,
                    "sp_integracion_sap_drivin_consultas",
                    parameters,
                    ct);

                _logger.LogInformation(
                    $"Se obtuvieron {documentsType.Count()} documentos pendientes desde HANA");

                return documentsType;
            }, cancellationToken);
        }

        public async Task<IEnumerable<SapItemsTable>> GetPendingItemsTypeAsync(
            string transactionType,
            CancellationToken cancellationToken = default)
        {
            return await _queryResilienciaItems.ExecuteAsync(async (ct) =>
            {

                using var connection = await _connectionFactory.CreateConnectionAsync(ct);
                await connection.OpenAsync(ct);

                var parameters = new Dictionary<string, object>
                {
                    { "filtro_1", "ITMP" },
                    { "filtro_2", transactionType },
                    { "filtro_3", "" },
                    { "filtro_4", "" },
                    { "filtro_5", "" }
                };

                var itemsType = await _executeStoredProcedureHanaAsync.ExecuteStoredProcedureQueryAsync<SapItemsTable>(
                    connection,
                    "sp_integracion_sap_drivin_consultas",
                    parameters,
                    ct);

                _logger.LogInformation(
                    "Se obtuvieron {Count} articulos pendientes desde HANA para TransactionType={TransactionType}",
                    itemsType.Count(),
                    transactionType);

                return itemsType;
            }, cancellationToken);
        }

        public Task<IEnumerable<SapItemsTable>> GetPendingClienteItemsTypeAsync(
            CancellationToken cancellationToken = default)
        {
            return GetPendingItemsTypeAsync(TransactionTypeCliente, cancellationToken);
        }

        public Task<IEnumerable<SapItemsTable>> GetPendingDealerItemsTypeAsync(
            CancellationToken cancellationToken = default)
        {
            return GetPendingItemsTypeAsync(TransactionTypeDealer, cancellationToken);
        }

        public async Task<SapItemDetailDTO?> GetItemDetailByItemCodeAsync(
            string itemCode,
            CancellationToken cancellationToken = default)
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
                    { "filtro_3", "" },
                    { "filtro_4", "" },
                    { "filtro_5", "" }
                };

                var details = await _executeStoredProcedureHanaAsync.ExecuteStoredProcedureQueryAsync<SapItemDetailDTO>(
                    connection,
                    "sp_integracion_sap_drivin_consultas",
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

        public async Task<bool> ExistsAsync(
            int transaction,
            int docEntry,
            int docNum, 
           
            CancellationToken cancellationToken = default)
        {
            return await _transactionResiliencia.ExecuteAsync(async (ct) =>
            {
                var documentQuery = $"DocEntry:{docEntry} Transaction:{transaction}";
                using var connection = await _connectionFactory.CreateConnectionAsync(ct);
                await connection.OpenAsync(ct);

                var parameters = new Dictionary<string, object>
                {
                    { "filtro_1", "VDCE" },
                    { "filtro_2", docEntry },
                    { "filtro_3", docNum },
                    { "filtro_4", transaction },
                    { "filtro_5", "" }
                };

                var _listaDocumentos = await _executeStoredProcedureHanaAsync.ExecuteStoredProcedureQueryAsync<SapDrivinTable>(
                    connection,
                    "sp_integracion_sap_drivin_consultas",
                    parameters,
                    ct);
                var _documento = _listaDocumentos.FirstOrDefault();
                if (_documento == null || _documento.SapDocEntry == 0)
                {

                    _logger.LogInformation($"Documento {documentQuery} NO existe en cola.");
                    return false;
                }
                else
                {
                    _logger.LogInformation(
                        $"Documento {documentQuery} Ya existe en cola con el estado: {_documento.Status}");
                    return true;
                }

                    

              
            }, cancellationToken);

        }

        public async Task<bool> ExistsItemAsync(
            int transaction,
            string docEntry,
            string docNum,

            CancellationToken cancellationToken = default)
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
                    { "filtro_3", docNum },
                    { "filtro_4", transaction },
                    { "filtro_5", "" }
                };

                var _listaItems = await _executeStoredProcedureHanaAsync.ExecuteStoredProcedureQueryAsync<SapItemsTable>(
                    connection,
                    "sp_integracion_sap_drivin_consultas",
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


        public async Task<bool> InsertDocumentAsync(
            SapDrivinTable document,
            //OdbcConnection connection,
            //OdbcTransaction transaction,
            CancellationToken cancellationToken = default)
        {
           
            return await _transactionResiliencia.ExecuteAsync(async (ct) =>
            {
               
                using var connection = await _connectionFactory.CreateConnectionAsync(ct);
                await connection.OpenAsync(ct);


                
                    var parameters = new Dictionary<string, object>
                    {
                        { "filtro_1", "IRSD" },
                        { "Code", "" },
                        { "Name", "" },
                        { "Transactions", document.Transaction },
                        { "TransactionType", "" },
                        { "SapDocEntry", document.SapDocEntry },
                        { "SapDocNum", document.SapDocNum },
                        { "SapDocStatus", document.SapDocStatus },
                        { "Json", "" },
                        { "CreatedBy", "" },
                        { "UpdatedBy", "" },
                        { "Comments", "" },
                        { "Status", StatusHanaDocumentLevel.Inserted},
                        { "ResultFlag", 0 }


                    };

                   var result = await _executeStoredProcedureHanaAsync.ExecuteStoredProcedureTransactionAsync(
                   connection,
                   "sp_integracion_sap_drivin_transactions",
                   parameters,
                   null,
                   ct);

                    if (!result)
                    {
                        _logger.LogInformation($"Documento NO insertada : {document.SapDocEntry} ");
                       
                    }
                   
               
                 return (result);
            }, cancellationToken);
        }

        public async Task<bool> InsertItemAsync(
            SapItemsTable item,
            //OdbcConnection connection,
            //OdbcTransaction transaction,
            CancellationToken cancellationToken = default)
        {

            return await _transactionResiliencia.ExecuteAsync(async (ct) =>
            {

                using var connection = await _connectionFactory.CreateConnectionAsync(ct);
                await connection.OpenAsync(ct);



                var parameters = new Dictionary<string, object>
                    {
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
                        { "ResultFlag", 0 } //"ResultFlag"


                    };

                var result = await _executeStoredProcedureHanaAsync.ExecuteStoredProcedureTransactionAsync(
                connection,
                "sp_integracion_sap_drivin_transactions",
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


        public async Task<IEnumerable<SapDrivinTable>> GetPendingDocumentsAsync<TResult>(
           
            CancellationToken cancellationToken = default) where TResult : class, new()
        {
            return await _queryResiliencia.ExecuteAsync(async (ct) =>
            {
                
                using var connection = await _connectionFactory.CreateConnectionAsync(ct);
                await connection.OpenAsync(ct);

                var parameters = new Dictionary<string, object>
                {
                    { "filtro_1", "DPEM" },
                    { "filtro_2", "" },
                    { "filtro_3", "" },
                    { "filtro_4", "" },
                    { "filtro_5", "" }
                };

                var documents = await _executeStoredProcedureHanaAsync.ExecuteStoredProcedureQueryAsync<SapDrivinTable>(
                    connection,
                    "sp_integracion_sap_drivin_consultas",
                    parameters,
                    ct);

                return documents;
            }, cancellationToken);
        }

        public async Task<IEnumerable<SapItemsTable>> GetPendingItemsAsync<TResult>(
            string transactionType,

            CancellationToken cancellationToken = default) where TResult : class, new()
        {
            return await _queryResilienciaItems.ExecuteAsync(async (ct) =>
            {

                using var connection = await _connectionFactory.CreateConnectionAsync(ct);
                await connection.OpenAsync(ct);

                var parameters = new Dictionary<string, object>
                {
                    { "filtro_1", "PITM" },
                    { "filtro_2", transactionType },
                    { "filtro_3", "" },
                    { "filtro_4", "" },
                    { "filtro_5", "" }
                };

                var items = await _executeStoredProcedureHanaAsync.ExecuteStoredProcedureQueryAsync<SapItemsTable>(
                    connection,
                    "sp_integracion_sap_drivin_consultas",
                    parameters,
                    ct);

                return items;
            }, cancellationToken);
        }

        public Task<IEnumerable<SapItemsTable>> GetPendingClienteItemsAsync<TResult>(
            CancellationToken cancellationToken = default) where TResult : class, new()
        {
            return GetPendingItemsAsync<TResult>(TransactionTypeCliente, cancellationToken);
        }

        public Task<IEnumerable<SapItemsTable>> GetPendingDealerItemsAsync<TResult>(
            CancellationToken cancellationToken = default) where TResult : class, new()
        {
            return GetPendingItemsAsync<TResult>(TransactionTypeDealer, cancellationToken);
        }


        public async Task<bool> MarkStatusDocumentAsAsync(
            SapDrivinTable document,
            CancellationToken cancellationToken = default)
        {


            return await _transactionResiliencia.ExecuteAsync(async (ct) =>
            {

                using var connection = await _connectionFactory.CreateConnectionAsync(ct);
                await connection.OpenAsync(ct);

                var parameters = new Dictionary<string, object>
                    {
                        { "filtro_1", "ARSD" },
                        { "Code", "" },
                        { "Name", "" },
                        { "Transactions", 0 },
                        { "TransactionType", "" },
                        { "SapDocEntry", document.SapDocEntry },
                        { "SapDocNum", document.SapDocNum },
                        { "SapDocStatus", ""},
                        { "Json", document.Json },
                        { "CreatedBy", "" },
                        { "UpdatedBy", "" },
                        { "Comments", "" },
                        { "Status", document.Status },
                        { "ResultFlag", 0 }


                    };

                var result = await _executeStoredProcedureHanaAsync.ExecuteStoredProcedureTransactionAsync(
                connection,
                "sp_integracion_sap_drivin_transactions",
                parameters,
                null,
                ct);

                if (!result)
                {
                    _logger.LogInformation($"Documento NO insertado : {document.SapDocEntry} ");

                }


                return result;
            }, cancellationToken);


        }

        public async Task<bool> MarkStatusItemAsAsync(
            SapItemsTable item,
            CancellationToken cancellationToken = default)
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
                        { "filtro_5", "" },
                        { "filtro_6", item.SapDocEntry },
                        { "filtro_7", item.SapDocNum },
                        { "filtro_8", ""},
                        { "filtro_9", item.Json },
                        { "filtro_10", "" },
                        { "filtro_11", "" },
                        { "filtro_12", "" },
                        { "filtro_13", item.Status },
                        { "ResultFlag", 0 }
                    };

                var result = await _executeStoredProcedureHanaAsync.ExecuteStoredProcedureTransactionAsync(
                    connection,
                    "sp_integracion_sap_drivin_transactions",
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

        public async Task<IEnumerable<SapDrivinTable>> GetPendingHooksAsync<TResult>(

            CancellationToken cancellationToken = default) where TResult : class, new()
        {
            return await _queryResiliencia.ExecuteAsync(async (ct) =>
            {

                using var connection = await _connectionFactory.CreateConnectionAsync(ct);
                await connection.OpenAsync(ct);

                var parameters = new Dictionary<string, object>
                {
                    { "filtro_1", "OHPS" },
                    { "filtro_2", "" },
                    { "filtro_3", "" },
                    { "filtro_4", "" },
                    { "filtro_5", "" }
                };

                var documents = await _executeStoredProcedureHanaAsync.ExecuteStoredProcedureQueryAsync<SapDrivinTable>(
                    connection,
                    "sp_integracion_sap_drivin_consultas",
                    parameters,
                    ct);

                return documents;
            }, cancellationToken);
        }


        public async Task<bool> VerifyHookAsync(
            int transaction, int docEntry, int docNum, string statusOrder,
            CancellationToken cancellationToken = default)
        {
            return await _transactionResiliencia.ExecuteAsync(async (ct) =>
            {
                var query = $"DocEntry: {docEntry}, DocNum: {docNum}, Transaction: {transaction}, StatusOrder: {statusOrder}";
                using var connection = await _connectionFactory.CreateConnectionAsync(ct);
                await connection.OpenAsync(ct);

                var parameters = new Dictionary<string, object>
                {
                    { "filtro_1", "VHES" },
                    { "filtro_2",  transaction},
                    { "filtro_3",  docEntry},
                    { "filtro_4", docNum },
                    { "filtro_5", statusOrder }
                };

                var _listaDocumentos = await _executeStoredProcedureHanaAsync.ExecuteStoredProcedureQueryAsync<SapDrivinTable>(
                    connection,
                    "sp_integracion_sap_drivin_consultas",
                    parameters,
                    ct);
                var _documento = _listaDocumentos.FirstOrDefault();
                if (_documento == null || _documento.SapDocEntry == 0)
                {

                    //_logger.LogInformation($"Documento {query} no procesado.");
                    return false;
                }
                else
                {
                    //_logger.LogInformation(
                    //    $"Documento {query} Ya existe en cola con el estado: {_documento.Status}");
                    return true;
                }




            }, cancellationToken);

        }



        public async Task<bool> ChangeStatusDocumenSAP(
           int transaction, int docEntry, int docNum, string statusOrder,
            OdbcConnection connection,
            OdbcTransaction odbcTransaction,
            CancellationToken cancellationToken = default)
        {
            var parameters = new Dictionary<string, object>
            {
                { "filtro_1", "ADSH" },
                { "Code", "" },
                { "Name", "" },
                { "Transactions", transaction },
                { "TransactionType", "" },
                { "SapDocEntry", docEntry },
                { "SapDocNum", docNum },
                { "SapDocStatus", statusOrder },
                { "Json", "" },
                { "CreatedBy", "" },
                { "UpdatedBy", "" },
                { "Comments", "" },
                { "Status", 0},
                { "ResultFlag", 0 }
            };

            var result = await _executeStoredProcedureHanaAsync.ExecuteStoredProcedureTransactionAsync(
                connection,
                "sp_integracion_sap_drivin_transactions",
                parameters,
                odbcTransaction,
                cancellationToken);

            if (!result)
            {
                _logger.LogWarning(
                   "Estado del documento NO actualizado | Transaction {Transaction} | DocEntry {DocEntry} | DocNum {DocNum} | Status {Status}",
                   transaction,
                   docEntry,
                   docNum,
                   statusOrder);
            }
            return result;
        }


        public async Task<bool> MarkHookStatusDocumentAsAsync(
            SapDrivinTable document,
            OdbcConnection connection,
            OdbcTransaction? odbcTransaction,
            CancellationToken cancellationToken = default)
        {
            var parameters = new Dictionary<string, object>
            {
                { "filtro_1", "ARHD" },
                { "Code", document.Code },
                { "Name", "" },
                { "Transactions", 0 },
                { "TransactionType", "" },
                { "SapDocEntry", 0 },
                { "SapDocNum", 0 },
                { "SapDocStatus", ""},
                { "Json", document.Json },
                { "CreatedBy", "" },
                { "UpdatedBy", "" },
                { "Comments", "" },
                { "Status", document.Status },
                { "ResultFlag", 0 }
            };

            var result = await _executeStoredProcedureHanaAsync.ExecuteStoredProcedureTransactionAsync(
                connection,
                "sp_integracion_sap_drivin_transactions",
                parameters,
                odbcTransaction,
                cancellationToken);

            if (!result)
            {
                _logger.LogInformation($"Documento NO insertado : {document.SapDocEntry} ");
            }

            return result;
        }


    }
}




