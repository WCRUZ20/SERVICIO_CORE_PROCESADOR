using Application.Abstractions;
using Application.Commands.Items;
using Application.Commands.Order;
using Application.Interfaces.HANA;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Handlers.Order
{
    public class MarkStatusOrdenAsCommandHandler : ICommandHandler<MarkStatusOrderAsCommand, bool>
    {
        private readonly IHanaRepository _hanaRepository;
        private readonly ILogger<MarkStatusOrdenAsCommandHandler> _logger;

        public MarkStatusOrdenAsCommandHandler(
            IHanaRepository hanaRepository,
            ILogger<MarkStatusOrdenAsCommandHandler> logger)
        {
            _hanaRepository = hanaRepository;
            _logger = logger;
        }

        public async Task<bool> HandleAsync(MarkStatusOrderAsCommand command)
        {
            var ordenQuery = $"SapDocEntry: {command.Orden.SapDocEntry} SapDocNum: {command.Orden.SapDocNum} Transaction: {command.Orden.Transaction}";
            _logger.LogDebug($"Actualizando orden {ordenQuery} en HANA");

            var updated = await _hanaRepository.MarkStatusOrderAsAsync(command.Orden);

            if (updated)
                _logger.LogDebug($"Orden {ordenQuery} actualizado correctamente");
            else
                _logger.LogWarning($"No se pudo actualizar la orden {ordenQuery}");

            return updated;
        }
    }
}
