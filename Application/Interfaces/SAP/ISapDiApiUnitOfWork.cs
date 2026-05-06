using SAPbobsCOM;

namespace Application.Interfaces.SAP
{
    public interface ISapDiApiUnitOfWork : IAsyncDisposable, IDisposable
    {
        Company Company { get; }

        Task BeginAsync(CancellationToken cancellationToken = default);
        Task CommitAsync(CancellationToken cancellationToken = default);
        Task RollbackAsync(CancellationToken cancellationToken = default);
    }
}

