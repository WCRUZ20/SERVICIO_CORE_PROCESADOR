using Application.Abstractions;
using Application.Commands;
using Application.Interfaces.API;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Handlers
{
    public class SendItemsToApiCommandHandler
        : ICommandHandler<SendItemsToApiCommand, (bool IsSuccess, string? Message)>
    {
        private readonly IApiClient _apiClient;
        private readonly ILogger<SendItemsToApiCommandHandler> _logger;

        public SendItemsToApiCommandHandler(
            IApiClient apiClient,
            ILogger<SendItemsToApiCommandHandler> logger)
        {
            _apiClient = apiClient;
            _logger = logger;
        }

        public async Task<(bool IsSuccess, string? Message)> HandleAsync(SendItemsToApiCommand command)
        {
            _logger.LogDebug("Enviando articulo SapDocEntry: {SapDocEntry} al API", command.Item.SapDocEntry);
            var result = await _apiClient.SendItemAsync(command.Item);

            if (result.IsSuccess)
            {
                _logger.LogDebug("Articulo SapDocEntry: {SapDocEntry} enviado exitosamente al API", command.Item.SapDocEntry);
            }
            else
            {
                _logger.LogWarning(
                    "Error al enviar articulo SapDocEntry: {SapDocEntry} al API. Error: {Error}",
                    command.Item.SapDocEntry,
                    result.Message);
            }

            return result;
        }
    }

}
