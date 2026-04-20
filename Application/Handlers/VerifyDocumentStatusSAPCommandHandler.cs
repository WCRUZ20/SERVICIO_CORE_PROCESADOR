using Application.Abstractions;
using Application.Commands;
using Application.Interfaces;
using Application.Interfaces.HANA;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Application.Handlers
{
    public class VerifyDocumentStatusSAPCommandHandler
        : ICommandHandler<VerifyDocumentStatusSAPCommand, bool>
    {
        private readonly IHanaRepository _hanaRepository;
        private readonly IUnitOfWork? _unitOfWork;
        private readonly ILogger<VerifyDocumentStatusSAPCommandHandler> _logger;

        public VerifyDocumentStatusSAPCommandHandler(
            IHanaRepository hanaRepository,
            IUnitOfWork? unitOfWork,
            ILogger<VerifyDocumentStatusSAPCommandHandler> logger)
        {
            _hanaRepository = hanaRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<bool> HandleAsync(VerifyDocumentStatusSAPCommand command)
        {
            var transaction = command.transaction;
            var docEntry = command.docEntry;
            var docNum = command.docNum;
            var statusOrder = command.statusOrder;
            var query = $"DocEntry: {docEntry}| DocNum: {docNum}| Transaction: {transaction}| StatusOrder: {statusOrder}";

            _logger.LogInformation(
                $"Verificando {query}");

            bool exists;

            exists = await _hanaRepository.VerifyHookAsync(transaction,
                docEntry,
                docNum,
                statusOrder);
            if (exists)
            {
                _logger.LogInformation($"Documento en SAP {query} tiene el mismo estado.");

            }

            return exists;
        }
    }
}

