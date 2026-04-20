using Application.Abstractions;
using Application.Commands;
using Application.Interfaces.HANA;
using Domain.SAP;
using Microsoft.Extensions.Logging;
using SAPbobsCOM;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Handlers
{
    public class GetPendingHooksCommandHandler 
        : ICommandHandler<GetPendingHooksCommand, IEnumerable<SapDrivinTable>>
    {
        private readonly IHanaRepository _hanaRepository;
        private readonly ILogger<GetPendingHooksCommandHandler> _logger;

        public GetPendingHooksCommandHandler(
            IHanaRepository hanaRepository,
            ILogger<GetPendingHooksCommandHandler> logger)
        {
            _hanaRepository = hanaRepository;
            _logger = logger;
        }

        public async Task<IEnumerable<SapDrivinTable>> HandleAsync(
            GetPendingHooksCommand command)
        {
            _logger.LogInformation(
                "Obteniendo hooks pendientes desde HANA  -> SAP");

            var documents = await _hanaRepository.GetPendingHooksAsync<SapDrivinTableDTO>();

            return documents;
        }
    }
}

