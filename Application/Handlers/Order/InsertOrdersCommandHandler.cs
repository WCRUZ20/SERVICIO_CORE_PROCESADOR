using Application.Abstractions;
using Application.Commands.Items;
using Application.Commands.Order;
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

namespace Application.Handlers.Order
{
    public class InsertOrdersCommandHandler : ICommandHandler<InsertOrdersCommand, bool>
    {
        private readonly IHanaRepository _hanaRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<InsertOrdersCommandHandler> _logger;

        public InsertOrdersCommandHandler(
            IHanaRepository hanaRepository,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<InsertOrdersCommandHandler> logger)
        {
            _hanaRepository = hanaRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<bool> HandleAsync(InsertOrdersCommand command)
        {
            var OrdenQuery = $"Orden: {command.Orden.idWoo} Transaction: {command.Orden.Transaction}";

            _logger.LogDebug(
                $"Insertando Orden {OrdenQuery}  en HANA");

            // Convertir DTO a entidad de dominio usando Mapster
            var entity = _mapper.Map<SapItemQueeDTO>(command.Orden);

            var inserted = await _hanaRepository.InsertOrdenAsync(entity);

            if (inserted)
            {
                _logger.LogDebug($"Orden {OrdenQuery} insertado correctamente");
            }
            else
            {
                _logger.LogWarning($"No se pudo insertar el orden: {OrdenQuery}");
            }

            return inserted;
        }
    }
}
