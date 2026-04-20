using Application.Abstractions;
using Application.Commands;
using Application.DTO;
using Application.Interfaces.HANA;
using Domain.SAP;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.Logging;
using SAPbobsCOM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Handlers
{
    public class GetPendingDocumentsTypeCommandHandler 
        : ICommandHandler<GetPendingDocumentsTypeCommand, IEnumerable<SapDrivinTableDTO>>
    {
        private readonly IHanaRepository _hanaRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<GetPendingDocumentsTypeCommandHandler> _logger;

        public GetPendingDocumentsTypeCommandHandler(
            IHanaRepository hanaRepository,
            IMapper mapper,
            ILogger<GetPendingDocumentsTypeCommandHandler> logger)
        {
            _hanaRepository = hanaRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<IEnumerable<SapDrivinTableDTO>> HandleAsync(
            GetPendingDocumentsTypeCommand command)
        {
            _logger.LogInformation(
                "Obteniendo documentos pendientes");

            // Obtener entidades de dominio desde el Repository
            var entities = await _hanaRepository.GetPendingDocumentsTypeAsync();

            var entityList = entities.ToList();
            
            _logger.LogInformation(
                $"Se encontraron {entityList.Count} documentos pendientes");

            // Convertir entidades a DTOs usando Mapster
            var dtos = _mapper.Map<List<SapDrivinTableDTO>>(entityList);
            
            return dtos;
        }
    }
}

