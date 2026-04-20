using Application.Abstractions;
using Application.Commands;
using Application.Interfaces.API;
using Domain.SAP;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Application.Handlers
{
    public class SendStockDocumentsToApiCommandHandler 
        : ICommandHandler<SendDocumentsToApiCommand, (bool IsSuccess, string? Message)>
    {
        private readonly IApiClient _apiClient;
        private readonly ILogger<SendStockDocumentsToApiCommandHandler> _logger;

        public SendStockDocumentsToApiCommandHandler(
            IApiClient apiClient,
            ILogger<SendStockDocumentsToApiCommandHandler> logger)
        {
            _apiClient = apiClient;
            _logger = logger;
        }

        public async Task<(bool IsSuccess, string? Message)> HandleAsync(
            SendDocumentsToApiCommand command)
        {
            _logger.LogDebug(
                "Enviando documento DocEntry: {DocEntry} al API", 
                command.Document.SapDocEntry);

            var result = await _apiClient.SendDocumentAsync(
                command.Document);

            if (result.IsSuccess)
            {
                _logger.LogDebug(
                    "documento DocEntry: {DocEntry} enviada exitosamente al API", 
                    command.Document.SapDocEntry);
            }
            else
            {
                _logger.LogWarning(
                    "Error al enviar documento DocEntry: {DocEntry} al API. Error: {Error}", 
                    command.Document.SapDocEntry,
                    result.Message);
            }

            return result;
        }
    }
}

