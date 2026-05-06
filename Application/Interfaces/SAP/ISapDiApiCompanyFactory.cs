using SAPbobsCOM;

namespace Application.Interfaces.SAP
{
    public interface ISapDiApiCompanyFactory
    {
        Task<ISapCompanySession> CreateSessionAsync(CancellationToken cancellationToken = default);
    }

    public interface ISapCompanySession : IAsyncDisposable, IDisposable
    {
        Company Company { get; }
    }
}

