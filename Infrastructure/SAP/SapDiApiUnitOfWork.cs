using Application.Interfaces.SAP;
using Microsoft.Extensions.Logging;
using SAPbobsCOM;

namespace Infrastructure.SAP
{
    public sealed class SapDiApiUnitOfWork : ISapDiApiUnitOfWork
    {
        private readonly ISapDiApiCompanyFactory _companyFactory;
        private readonly ILogger<SapDiApiUnitOfWork> _logger;

        private ISapCompanySession? _session;
        private bool _transactionStarted;

        public SapDiApiUnitOfWork(ISapDiApiCompanyFactory companyFactory, ILogger<SapDiApiUnitOfWork> logger)
        {
            _companyFactory = companyFactory;
            _logger = logger;
        }

        public Company Company =>
            _session?.Company ?? throw new InvalidOperationException("DI API session no inicializada. Llame BeginAsync primero.");

        public async Task BeginAsync(CancellationToken cancellationToken = default)
        {
            if (_session != null)
                throw new InvalidOperationException("Ya existe una sesión DI API activa en este UnitOfWork.");

            _session = await _companyFactory.CreateSessionAsync(cancellationToken);

            if (!Company.InTransaction)
            {
                Company.StartTransaction();
                _transactionStarted = true;
                _logger.LogInformation("DI API StartTransaction iniciado.");
            }
        }

        public Task CommitAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_session == null)
                throw new InvalidOperationException("No hay sesión DI API activa para Commit.");

            if (_transactionStarted && Company.InTransaction)
            {
                Company.EndTransaction(BoWfTransOpt.wf_Commit);
                _logger.LogInformation("DI API Commit ejecutado.");
            }

            _transactionStarted = false;
            CleanupSession();
            return Task.CompletedTask;
        }

        public Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_session == null)
                throw new InvalidOperationException("No hay sesión DI API activa para Rollback.");

            if (_transactionStarted && Company.InTransaction)
            {
                Company.EndTransaction(BoWfTransOpt.wf_RollBack);
                _logger.LogInformation("DI API Rollback ejecutado.");
            }

            _transactionStarted = false;
            CleanupSession();
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            try
            {
                if (_transactionStarted && _session != null && Company.InTransaction)
                {
                    Company.EndTransaction(BoWfTransOpt.wf_RollBack);
                    _logger.LogWarning("DI API UnitOfWork disposed con transacción abierta. Se aplicó Rollback.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al hacer rollback en Dispose del DI API UnitOfWork.");
            }
            finally
            {
                _transactionStarted = false;
                CleanupSession();
            }
        }

        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }

        private void CleanupSession()
        {
            try
            {
                _session?.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al limpiar sesión DI API.");
            }
            finally
            {
                _session = null;
            }
        }
    }
}

