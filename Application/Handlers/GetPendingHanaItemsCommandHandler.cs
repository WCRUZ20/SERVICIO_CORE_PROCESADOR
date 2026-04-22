using Application.Abstractions;
using Application.Commands;
using Application.Interfaces.HANA;
using Domain.SAP;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Handlers
{
    public class GetPendingHanaItemsCommandHandler
        : ICommandHandler<GetPendingHanaItemsCommand, IEnumerable<SapItemsTable>>
    {
        private readonly IHanaRepository _hanaRepository;
        private readonly ILogger<GetPendingHanaItemsCommandHandler> _logger;

        public GetPendingHanaItemsCommandHandler(
            IHanaRepository hanaRepository,
            ILogger<GetPendingHanaItemsCommandHandler> logger)
        {
            _hanaRepository = hanaRepository;
            _logger = logger;
        }

        public async Task<IEnumerable<SapItemsTable>> HandleAsync(GetPendingHanaItemsCommand command)
        {
            _logger.LogInformation(
                 "Obteniendo articulos pendientes desde HANA -> API para TransactionType={TransactionType}",
                 command.TransactionType);
            return await _hanaRepository.GetPendingItemsAsync<SapItemsTable>(command.TransactionType);
        }
    }
}
