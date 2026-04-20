using Application.Abstractions;
using Application.Commands;
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

namespace Application.Handlers
{
    public class InsertItemCommandHandler : ICommandHandler<InsertItemsCommand, bool>
    {
        private readonly IHanaRepository _hanaRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<InsertItemCommandHandler> _logger;

        public InsertItemCommandHandler(
            IHanaRepository hanaRepository,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<InsertItemCommandHandler> logger)
        {
            _hanaRepository = hanaRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<bool> HandleAsync(InsertItemsCommand command)
        {
            var itemQuery = $"ItemCode: {command.item.SapDocEntry} DocNum: {command.item.SapDocNum} Transaction: {command.item.Transaction}";

            _logger.LogDebug(
                $"Insertando articulo {itemQuery}  en HANA");

            // Convertir DTO a entidad de dominio usando Mapster
            var entity = _mapper.Map<SapItemsTable>(command.item);

            var inserted = await _hanaRepository.InsertItemAsync(entity);

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
