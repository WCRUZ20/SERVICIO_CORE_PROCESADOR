using Application.Abstractions;
using Application.Commands.Items;
using Application.Interfaces.HANA;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Handlers.Items
{
    public class CheckItemExistsCommandHandler : ICommandHandler<CheckItemExistsCommand, bool>
    {
        private readonly IHanaRepository _hanaRepository;
        //private readonly IUnitOfWork? _unitOfWork;
        private readonly ILogger<CheckItemExistsCommandHandler> _logger;



        public CheckItemExistsCommandHandler(
            IHanaRepository hanaRepository,
            //IUnitOfWork? unitOfWork,
            ILogger<CheckItemExistsCommandHandler> logger)
        {
            _hanaRepository = hanaRepository;
            //_unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<bool> HandleAsync(CheckItemExistsCommand command)
        {
            _logger.LogDebug(
                $"Verificando si existe articulo con ItemcCode: {command.itemCode} Transaction: {command.transaction}");

            bool exists;

            exists = await _hanaRepository.ExistsItemAsync(command.transaction, command.itemCode, command.bodega);
            if (exists)
            {
                _logger.LogDebug($"articulo ItemCode {command.itemCode} Transaction: {command.transaction}");

            }

            return exists;
        }
    }
}
