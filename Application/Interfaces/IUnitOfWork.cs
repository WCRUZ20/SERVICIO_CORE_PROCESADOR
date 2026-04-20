using System;
using System.Data.Odbc;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    /// <summary>
    /// Interfaz para manejar transacciones de base de datos
    /// </summary>
    public interface IUnitOfWork : IDisposable
    {
        /// <summary>
        /// Inicia una transacción
        /// </summary>
        Task BeginTransactionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Confirma la transacción
        /// </summary>
        Task CommitAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Revierte la transacción
        /// </summary>
        Task RollbackAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtiene la conexión de la transacción actual
        /// </summary>
        OdbcConnection GetConnection();

        /// <summary>
        /// Obtiene la transacción actual
        /// </summary>
        OdbcTransaction? GetTransaction();
    }
}

