using Application.Abstractions;
using Application.Commands;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Application.Handlers
{
    /// <summary>
    /// Handler para cambiar estado de documento en SAP.
    /// NOTA: Para operaciones transaccionales en hooks, usar directamente IHanaRepository desde el UseCase.
    /// Este handler se mantiene para compatibilidad pero no se recomienda su uso directo.
    /// </summary>
    public class ChangeStatusDocumentSAPCommandHandler 
        : ICommandHandler<ChangeStatusDocumentSAPCommand, bool>
    {
        private readonly ILogger<ChangeStatusDocumentSAPCommandHandler> _logger;

        public ChangeStatusDocumentSAPCommandHandler(
            ILogger<ChangeStatusDocumentSAPCommandHandler> logger)
        {
            _logger = logger;
        }

        public Task<bool> HandleAsync(ChangeStatusDocumentSAPCommand command)
        {
            var query = $"DocEntry: {command.docEntry}, DocNum: {command.docNum}, Transaction: {command.Transaction}";
            
            _logger.LogWarning(
                $"ChangeStatusDocumentSAPCommandHandler no debe usarse directamente. " +
                $"Use IHanaRepository.ChangeStatusDocumenSAP con UnitOfWork para documento: {query}");
            
            return Task.FromResult(false);
        }
    }
}

