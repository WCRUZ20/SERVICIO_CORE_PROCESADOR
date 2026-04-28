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
            SapItemQueeDTO items,
            //OdbcConnection connection,
            //OdbcTransaction transaction,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtiene Transacciones pendientes de procesamiento desde SAP
        /// </summary>
        //Task<IEnumerable<SapItemQueeDTO>> GetPendingItemsTypeAsync(
        //    string transactionType,
        //    CancellationToken cancellationToken = default);

        Task<IEnumerable<SapItemQueeDTO>> GetPendingClienteItemsTypeAsync(
            CancellationToken cancellationToken = default);

        Task<IEnumerable<SapItemQueeDTO>> GetPendingDealerItemsTypeAsync(
            CancellationToken cancellationToken = default);

        //GetUpdateDealerItemsTypeAsync
        Task<IEnumerable<SapItemQueeDTO>> GetUpdateDealerItemsTypeAsync(
            CancellationToken cancellationToken = default);

        Task<IEnumerable<SapItemQueeDTO>> GetUpdateClienteItemsTypeAsync(
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtiene items pendientes de procesamiento en HANA
        /// </summary>
        /// 
        //Task<IEnumerable<SapItemQueeDTO>> GetPendingItemsAsync<TResult>(
        //    string transactionType,
        //    CancellationToken cancellationToken = default) where TResult : class, new();

        Task<IEnumerable<SapItemQueeDTO>> GetPendingClienteItemsAsync<TResult>(
            CancellationToken cancellationToken = default) where TResult : class, new();

        Task<IEnumerable<SapItemQueeDTO>> GetPendingDealerItemsAsync<TResult>(
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
            SapItemQueeDTO item,
            CancellationToken cancellationToken = default);

        //UpdateStockClienteAsync
        Task<bool> UpdateStockAsync(
            SapItemQueeDTO item,
            CancellationToken cancellationToken = default);

        //Task<bool> UpdateStockDealerAsync(
        //    SapItemQueeDTO item,
        //    CancellationToken cancellationToken = default);

        Task<bool> UpdatePrecioClienteAsync(
            SapItemQueeDTO item,
            CancellationToken cancellationToken = default);

        Task<bool> UpdatePrecioDealerAsync(
            SapItemQueeDTO item,
            CancellationToken cancellationToken = default);

        Task<bool> ExistsItemAsync(
            int transaction, string docEntry, string docNum,
            CancellationToken cancellationToken = default);

    }
}

