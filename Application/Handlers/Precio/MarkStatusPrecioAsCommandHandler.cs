using Application.Abstractions;
using Application.Commands.Precio;
using Application.Commands.Stock;
using Application.Handlers.Stock;
using Application.Interfaces.HANA;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Handlers.Precio
{
    public class MarkStatusPrecioAsCommandHandler : ICommandHandler<MarkStatusPrecioAsCommand, bool>
    {
        private readonly IHanaRepository _hanaRepository;
        private readonly ILogger<MarkStatusPrecioAsCommandHandler> _logger;

        public MarkStatusPrecioAsCommandHandler(
            IHanaRepository hanaRepository,
            ILogger<MarkStatusPrecioAsCommandHandler> logger)
        {
            _hanaRepository = hanaRepository;
            _logger = logger;
        }

        public async Task<bool> HandleAsync(MarkStatusPrecioAsCommand command)
        {
            var itemQuery = $"SapDocEntry: {command.Item.SapDocEntry} SapDocNum: {command.Item.SapDocNum} Transaction: {command.Item.Transaction}";
            _logger.LogDebug($"Actualizando precio {itemQuery} en HANA");

            var updated = await _hanaRepository.MarkStatusPrecioAsAsync(command.Item);

            if (updated)
                _logger.LogDebug($"Precio {itemQuery} actualizado correctamente");
            else
                _logger.LogWarning($"No se pudo actualizar el precio {itemQuery}");

            return updated;
        }
    }
}
