using Application.Abstractions;
using Application.Commands.Items;
using Application.Commands.Order;
using Application.Handlers.Items;
using Application.Interfaces.HANA;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Handlers.Order
{
    public class CheckOrderExistsCommandHandler : ICommandHandler<CheckOrderExistsCommand, bool>
    {
        private readonly IHanaRepository _hanaRepository;
        //private readonly IUnitOfWork? _unitOfWork;
        private readonly ILogger<CheckOrderExistsCommandHandler> _logger;



        public CheckOrderExistsCommandHandler(
            IHanaRepository hanaRepository,
            //IUnitOfWork? unitOfWork,
            ILogger<CheckOrderExistsCommandHandler> logger)
        {
            _hanaRepository = hanaRepository;
            //_unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<bool> HandleAsync(CheckOrderExistsCommand command)
        {
            _logger.LogDebug(
                $"Verificando si existe orden con IdWoo: {command.idWoo} Transaction: {command.transaction} Transaction Type: {command.transaction_type}");

            bool exists;

            exists = await _hanaRepository.ExistsOrderAsync(command.transaction, command.idWoo, command.transaction_type);
            if (exists)
            {
                _logger.LogDebug($"Orden idWoo {command.idWoo} Transaction: {command.transaction} Transaction Type: {command.transaction_type}");

            }

            return exists;
        }
    }
}
