using Application.Abstractions;
using Application.Commands;
using Application.DTO;
using Application.Interfaces;
using Application.Interfaces.HANA;
using Domain.SAP;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Handlers
{
    public class InsertDocumentCommandHandler 
        : ICommandHandler<InsertDocumentCommand, bool>
    {
        private readonly IHanaRepository _hanaRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<InsertDocumentCommandHandler> _logger;

        public InsertDocumentCommandHandler(
            IHanaRepository hanaRepository,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<InsertDocumentCommandHandler> logger)
        {
            _hanaRepository = hanaRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<bool> HandleAsync(InsertDocumentCommand command)
        {
            var documentQuery = $"DocEntry: {command.document.SapDocEntry} DocNum: {command.document.SapDocNum} Transaction: {command.document.Transaction}";

            _logger.LogDebug(
                $"Insertando documento {documentQuery}  en HANA");

            // Convertir DTO a entidad de dominio usando Mapster
            var entity = _mapper.Map<SapDrivinTable>(command.document);

            var inserted = await _hanaRepository.InsertDocumentAsync(entity);

            if (inserted)
            {
                _logger.LogDebug($"Documento {documentQuery} insertado correctamente");
            }
            else
            {
                _logger.LogWarning($"No se pudo insertar el documento: {documentQuery}");
            }

            return inserted;
        }
    }
}

