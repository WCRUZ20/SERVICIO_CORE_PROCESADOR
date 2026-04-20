using Application.Abstractions;
using Application.Commands;
using Application.Interfaces.HANA;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Handlers
{
    public class MarkStatusItemAsCommandHandler
        : ICommandHandler<MarkStatusItemAsCommand, bool>
    {
        private readonly IHanaRepository _hanaRepository;
        private readonly ILogger<MarkStatusItemAsCommandHandler> _logger;

        public MarkStatusItemAsCommandHandler(
            IHanaRepository hanaRepository,
            ILogger<MarkStatusItemAsCommandHandler> logger)
        {
            _hanaRepository = hanaRepository;
            _logger = logger;
        }

        public async Task<bool> HandleAsync(MarkStatusItemAsCommand command)
        {
            var itemQuery = $"SapDocEntry: {command.Item.SapDocEntry} SapDocNum: {command.Item.SapDocNum} Transaction: {command.Item.Transaction}";
            _logger.LogDebug($"Actualizando articulo {itemQuery} en HANA");

            var updated = await _hanaRepository.MarkStatusItemAsAsync(command.Item);

            if (updated)
                _logger.LogDebug($"Articulo {itemQuery} actualizado correctamente");
            else
                _logger.LogWarning($"No se pudo actualizar el articulo {itemQuery}");

            return updated;
        }
    }

}
