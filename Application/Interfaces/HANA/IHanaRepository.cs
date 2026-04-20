using Domain.SAP;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.Odbc;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Interfaces.HANA
{
    /// <summary>
    /// Interfaz para repositorio de acceso a HANA mediante ODBC
    /// </summary>
    public interface IHanaRepository
    {




        /// <summary>
        /// Inserta documentos en la tabla histórica de HANA
        /// </summary>
        Task<bool> InsertDocumentAsync(
            SapDrivinTable transfers,
            //OdbcConnection connection,
            //OdbcTransaction transaction,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtiene Transacciones pendientes de procesamiento desde SAP
        /// </summary>
        Task<IEnumerable<SapDrivinTable>> GetPendingDocumentsTypeAsync(
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtiene documentos pendientes de procesamiento en HANA
        /// </summary>
        /// 
        Task<IEnumerable<SapDrivinTable>> GetPendingDocumentsAsync<TResult>(
            CancellationToken cancellationToken = default) where TResult : class, new();

        /// <summary>
        /// Marca un documento como procesado
        /// </summary>
        Task<bool> MarkStatusDocumentAsAsync(
            SapDrivinTable document,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Verifica si un documento ya existe en tabla HANA
        /// </summary>
        Task<bool> ExistsAsync(
            int transaction, int docEntry, int docNum,
            CancellationToken cancellationToken = default);


        /// <summary>
        /// Verifica si un documento ya existe en tabla HANA
        /// </summary>
        Task<bool> VerifyHookAsync(
            int transaction, int docEntry, int docNum, string statusOrder,
            CancellationToken cancellationToken = default);


        /// <summary>
        /// Obtiene hooks pendientes de procesamiento en SAP
        /// </summary>
        /// 
        Task<IEnumerable<SapDrivinTable>> GetPendingHooksAsync<TResult>(
            CancellationToken cancellationToken = default) where TResult : class, new();


        /// <summary>
        /// Cambiar estado en documento SAP en base al HOOK
        /// </summary>
        Task<bool> ChangeStatusDocumenSAP(
           int transaction, int docEntry, int docNum, string statusOrder,
            OdbcConnection connection,
            OdbcTransaction odbcTransaction,
            CancellationToken cancellationToken = default);


        /// <summary>
        /// Marca un documento como procesado Hook
        /// </summary>
        Task<bool> MarkHookStatusDocumentAsAsync(
            SapDrivinTable document,
            OdbcConnection connection,
            OdbcTransaction? odbcTransaction,
            CancellationToken cancellationToken = default);

    }
}

