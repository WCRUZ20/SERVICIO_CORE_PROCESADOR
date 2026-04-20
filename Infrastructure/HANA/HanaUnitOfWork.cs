using Application.Interfaces;
using Application.Interfaces.HANA;
using Microsoft.Extensions.Logging;
using System;
using System.Data.Odbc;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.HANA
{
    public class HanaUnitOfWork : IUnitOfWork
    {
        private readonly IHanaConnectionFactory _connectionFactory;
        private readonly ILogger<HanaUnitOfWork> _logger;
        private OdbcConnection? _connection;
        private OdbcTransaction? _transaction;
        private bool _disposed = false;

        public HanaUnitOfWork(
            IHanaConnectionFactory connectionFactory,
            ILogger<HanaUnitOfWork> logger)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
        }

        public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_transaction != null)
            {
                throw new InvalidOperationException("Ya existe una transacción activa");
            }

            _connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
            await _connection.OpenAsync(cancellationToken);
            _transaction = _connection.BeginTransaction();
            _logger.LogInformation("Transacción Begin");
        }

        public async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            if (_transaction == null)
            {
                throw new InvalidOperationException("No hay transacción activa para confirmar");
            }

            await Task.Run(() => _transaction.Commit(), cancellationToken);
            CleanupTransaction();
            _logger.LogInformation("Transacción commit");
        }

        public async Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            if (_transaction == null)
            {
                throw new InvalidOperationException("No hay transacción activa para revertir");
            }

            await Task.Run(() => _transaction.Rollback(), cancellationToken);
            CleanupTransaction();
            _logger.LogInformation("Transacción rollback");
        }

        public OdbcConnection GetConnection()
        {
            if (_connection == null)
            {
                throw new InvalidOperationException("La conexión no está inicializada. Llame a BeginTransactionAsync primero.");
            }
            return _connection;
        }

        public OdbcTransaction GetTransaction()
        {
            if (_transaction == null)
            {
                throw new InvalidOperationException("La conexión no está inicializada. Llame a BeginTransactionAsync primero.");
            }
            return _transaction;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _transaction?.Dispose();
                _connection?.Dispose();
                _disposed = true;
            }
        }

        // Método privado para limpiar recursos
        private void CleanupTransaction()
        {
            _transaction?.Dispose();
            _transaction = null;

            _connection?.Close();
            _connection?.Dispose();
            _connection = null;
        }
    }
}

