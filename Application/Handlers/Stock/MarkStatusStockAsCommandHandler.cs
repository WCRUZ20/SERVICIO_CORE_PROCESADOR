using Application.Abstractions;
using Application.Commands.Items;
using Application.Commands.Stock;
using Application.Interfaces.HANA;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Handlers.Stock
{
    public class MarkStatusStockAsCommandHandler
        : ICommandHandler<MarkStatusStockAsCommand, bool>
    {
        private readonly IHanaRepository _hanaRepository;
        private readonly ILogger<MarkStatusStockAsCommandHandler> _logger;

        public MarkStatusStockAsCommandHandler(
            IHanaRepository hanaRepository,
            ILogger<MarkStatusStockAsCommandHandler> logger)
        {
            _hanaRepository = hanaRepository;
            _logger = logger;
        }

        public async Task<bool> HandleAsync(MarkStatusStockAsCommand command)
        {
            var itemQuery = $"SapDocEntry: {command.Item.SapDocEntry} SapDocNum: {command.Item.SapDocNum} Transaction: {command.Item.Transaction}";
            _logger.LogDebug($"Actualizando stock {itemQuery} en HANA");

            var updated = await _hanaRepository.MarkStatusStockAsAsync(command.Item);

            if (updated)
                _logger.LogDebug($"Stock {itemQuery} actualizado correctamente");
            else
                _logger.LogWarning($"No se pudo actualizar el stock {itemQuery}");

            return updated;
        }
    }
}
