using Application.Abstractions;
using Application.Commands;
using Application.DTO;
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
    public class GetPendingItemsTypeCommandHandler
        : ICommandHandler<GetPendingItemsTypeCommand, IEnumerable<SapItemQueeDTO>>
    {
        private readonly IHanaRepository _hanaRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<GetPendingItemsTypeCommandHandler> _logger;

        public GetPendingItemsTypeCommandHandler(
            IHanaRepository hanaRepository,
            IMapper mapper,
            ILogger<GetPendingItemsTypeCommandHandler> logger)
        {
            _hanaRepository = hanaRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<IEnumerable<SapItemQueeDTO>> HandleAsync(
            GetPendingItemsTypeCommand command)
        {
            _logger.LogInformation(
                "Obteniendo documentos pendientes");

            // Obtener entidades de dominio desde el Repository
            var entities = await _hanaRepository.GetPendingDocumentsTypeAsync();

            var entityList = entities.ToList();

            _logger.LogInformation(
                $"Se encontraron {entityList.Count} documentos pendientes");

            // Convertir entidades a DTOs usando Mapster
            var dtos = _mapper.Map<List<SapItemQueeDTO>>(entityList);

            return dtos;
        }
    }
}
