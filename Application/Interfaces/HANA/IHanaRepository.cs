using Application.DTO;
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
        Task<bool> InsertItemAsync(
            SapItemsTable items,
            //OdbcConnection connection,
            //OdbcTransaction transaction,
            CancellationToken cancellationToken = default);
                
        /// <summary>
        /// Obtiene Transacciones pendientes de procesamiento desde SAP
        /// </summary>
        Task<IEnumerable<SapItemsTable>> GetPendingItemsTypeAsync(
            string transactionType,
            CancellationToken cancellationToken = default);

        Task<IEnumerable<SapItemsTable>> GetPendingClienteItemsTypeAsync(
            CancellationToken cancellationToken = default);

        Task<IEnumerable<SapItemsTable>> GetPendingDealerItemsTypeAsync(
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtiene items pendientes de procesamiento en HANA
        /// </summary>
        /// 
        Task<IEnumerable<SapItemsTable>> GetPendingItemsAsync<TResult>(
            string transactionType,
            CancellationToken cancellationToken = default) where TResult : class, new();

        Task<IEnumerable<SapItemsTable>> GetPendingClienteItemsAsync<TResult>(
            CancellationToken cancellationToken = default) where TResult : class, new();

        Task<IEnumerable<SapItemsTable>> GetPendingDealerItemsAsync<TResult>(
            CancellationToken cancellationToken = default) where TResult : class, new();

        /// <summary>
        /// Obtiene el detalle del artículo en SAP/HANA a partir del ItemCode.
        /// </summary>
        Task<SapItemDetailDTO?> GetItemDetailByItemCodeAsync(
            string itemCode,
            CancellationToken cancellationToken = default);
                
        /// <summary>
        /// Marca un artículo como procesado
        /// </summary>
        Task<bool> MarkStatusItemAsAsync(
            SapItemsTable item,
            CancellationToken cancellationToken = default);

        Task<bool> ExistsItemAsync(
            int transaction, string docEntry, string docNum,
            CancellationToken cancellationToken = default);

    }
}

