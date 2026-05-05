using Application.Commands.Items;
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

        Task<bool> InsertOrdenAsync(
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

        Task<IEnumerable<SapItemQueeDTO>> GetPendingClienteStockAsync<TResult>(
            CancellationToken cancellationToken = default) where TResult : class, new();

        Task<IEnumerable<SapItemQueeDTO>> GetPendingDealerStockAsync<TResult>(
            CancellationToken cancellationToken = default) where TResult : class, new();
        /// <summary>
        /// Obtiene el detalle del artículo en SAP/HANA a partir del ItemCode.
        /// </summary>
        Task<SapItemDetailDTO?> GetItemDetailByItemCodeAsync(
            ItemDestinationType destinationType, string itemCode, string bodega,
            CancellationToken cancellationToken = default);

        Task<SapItemDetailDTO?> GetStockDetailByItemCodeAsync(
            Application.Commands.Stock.ItemDestinationType destinationType, string itemCode, string bodega,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Marca un artículo como procesado
        /// </summary>
        Task<bool> MarkStatusItemAsAsync(
            SapItemQueeDTO item,
            CancellationToken cancellationToken = default);

        Task<bool> MarkStatusStockAsAsync(
            SapItemQueeDTO item,
            CancellationToken cancellationToken = default);

        //UpdateStockClienteAsync
        Task<bool> InsertStockAsync(
            SapItemQueeDTO item,
            CancellationToken cancellationToken = default);

        //Task<bool> UpdateStockDealerAsync(
        //    SapItemQueeDTO item,
        //    CancellationToken cancellationToken = default);

        Task<bool> ExistsItemAsync(
            int transaction, string docEntry, string docNum,
            CancellationToken cancellationToken = default);

        Task<bool> ExistsOrderAsync(
            int transaction, string docEntry, string docNum,
            CancellationToken cancellationToken = default);


        //PRECIO
        Task<bool> InsertPrecioAsync(SapItemQueeDTO item, CancellationToken cancellationToken = default);
        Task<IEnumerable<SapItemQueeDTO>> GetUpdateDealerPrecioTypeAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<SapItemQueeDTO>> GetUpdateClientePrecioTypeAsync(CancellationToken cancellationToken = default);
        Task<bool> MarkStatusPrecioAsAsync(SapItemQueeDTO item, CancellationToken cancellationToken = default);
        Task<SapItemDetailDTO?> GetPrecioDetailByItemCodeAsync(Application.Commands.Precio.ItemDestinationType destinationType, string itemCode, string bodega, CancellationToken cancellationToken = default);
        Task<IEnumerable<SapItemQueeDTO>> GetPendingClientePrecioAsync<TResult>(CancellationToken cancellationToken = default) where TResult : class, new();
        Task<IEnumerable<SapItemQueeDTO>> GetPendingDealerPrecioAsync<TResult>(CancellationToken cancellationToken = default) where TResult : class, new();

    }
}

