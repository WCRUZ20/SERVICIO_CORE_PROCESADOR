using Application.Abstractions;
using Application.Commands;
using Application.Interfaces;
using Application.Interfaces.HANA;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Application.Handlers
{
    public class CheckDocumentExistsCommandHandler
        : ICommandHandler<CheckDocumentExistsCommand, bool>
    {
        private readonly IHanaRepository _hanaRepository;
        //private readonly IUnitOfWork? _unitOfWork;
        private readonly ILogger<CheckDocumentExistsCommandHandler> _logger;

        

        public CheckDocumentExistsCommandHandler(
            IHanaRepository hanaRepository,
            //IUnitOfWork? unitOfWork,
            ILogger<CheckDocumentExistsCommandHandler> logger)
        {
            _hanaRepository = hanaRepository;
            //_unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<bool> HandleAsync(CheckDocumentExistsCommand command)
        {
            _logger.LogDebug(
                $"Verificando si existe documento con DocEntry: {command.docEntry} Transaction: {command.transaction}");

            bool exists;

            exists = await _hanaRepository.ExistsAsync(command.transaction, command.docEntry, command.docNum);
            if (exists)
            {
                _logger.LogDebug($"documento DocEntry {command.docEntry} Transaction: {command.transaction}");

            }

            return exists;
        }
    }
}

