using Application.Abstractions;
using Application.Commands;
using Application.Interfaces.HANA;
using Domain.SAP;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Application.Handlers
{
    /// <summary>
    /// Handler para marcar estado de documento.
    /// NOTA: Para operaciones transaccionales, usar directamente IHanaRepository desde el UseCase.
    /// </summary>
    public class MarkStatusDocumentAsCommandHandler 
        : ICommandHandler<MarkStatusDocuemntAsCommand, bool>
    {
        private readonly IHanaRepository _hanaRepository;
        private readonly ILogger<MarkStatusDocumentAsCommandHandler> _logger;

        public MarkStatusDocumentAsCommandHandler(
            IHanaRepository hanaRepository,
            ILogger<MarkStatusDocumentAsCommandHandler> logger)
        {
            _hanaRepository = hanaRepository;
            _logger = logger;
        }

        public async Task<bool> HandleAsync(MarkStatusDocuemntAsCommand command)
        {
            var documentQuery = $"DocEntry: {command.document.SapDocEntry} DocNum: {command.document.SapDocNum} Transaction: {command.document.Transaction}";
            _logger.LogDebug($"Actualizando documento {documentQuery} en HANA");

            // Usar método sin transacción externa
            var updated = await _hanaRepository.MarkStatusDocumentAsAsync(command.document);

            if (updated)
                _logger.LogDebug($"Documento {documentQuery} actualizado correctamente");
            else
                _logger.LogWarning($"No se pudo actualizar el documento {documentQuery}");

            return updated;
        }
    }
}

