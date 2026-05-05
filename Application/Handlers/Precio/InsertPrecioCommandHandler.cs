using Application.Abstractions;
using Application.Commands.Items;
using Application.Commands.Precio;
using Application.DTO;
using Application.Handlers.Items;
using Application.Interfaces;
using Application.Interfaces.HANA;
using MapsterMapper;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Handlers.Precio
{
    public class InsertPrecioCommandHandler : ICommandHandler<InsertPrecioCommand, bool>
    {
        private readonly IHanaRepository _hanaRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<InsertPrecioCommandHandler> _logger;

        public InsertPrecioCommandHandler(
            IHanaRepository hanaRepository,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<InsertPrecioCommandHandler> logger)
        {
            _hanaRepository = hanaRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<bool> HandleAsync(InsertPrecioCommand command)
        {
            var itemQuery = $"ItemCode: {command.item.SapDocEntry} DocNum: {command.item.SapDocNum} Transaction: {command.item.Transaction}";

            _logger.LogDebug(
                $"Insertando precio {itemQuery}  en HANA");

            // Convertir DTO a entidad de dominio usando Mapster
            var entity = _mapper.Map<SapItemQueeDTO>(command.item);

            var inserted = await _hanaRepository.InsertPrecioAsync(entity);

            if (inserted)
            {
                _logger.LogDebug($"Precio {itemQuery} insertado correctamente");
            }
            else
            {
                _logger.LogWarning($"No se pudo insertar el precio: {itemQuery}");
            }

            return inserted;
        }
    }
}
