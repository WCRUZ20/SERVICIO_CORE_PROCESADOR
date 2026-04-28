using Application.Abstractions;
using Application.Commands.Items;
using Application.DTO;
using Application.Interfaces;
using Application.Interfaces.HANA;
using Domain.SAP;
using MapsterMapper;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Handlers.Items
{
    public class InsertStockCommandHandler : ICommandHandler<InsertStockCommand, bool>
    {
        private readonly IHanaRepository _hanaRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<InsertStockCommandHandler> _logger;

        public InsertStockCommandHandler(
            IHanaRepository hanaRepository,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<InsertStockCommandHandler> logger)
        {
            _hanaRepository = hanaRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<bool> HandleAsync(InsertStockCommand command)
        {
            var itemQuery = $"ItemCode: {command.item.SapDocEntry} DocNum: {command.item.SapDocNum} Transaction: {command.item.Transaction}";

            _logger.LogDebug(
                $"Insertando articulo {itemQuery}  en HANA");

            // Convertir DTO a entidad de dominio usando Mapster
            var entity = _mapper.Map<SapItemQueeDTO>(command.item);

            var inserted = await _hanaRepository.UpdateStockAsync(entity);

            if (inserted)
            {
                _logger.LogDebug($"Articulo {itemQuery} insertado correctamente");
            }
            else
            {
                _logger.LogWarning($"No se pudo insertar el articulo: {itemQuery}");
            }

            return inserted;
        }
    }
}
