using Application.Abstractions;
using Application.Commands.Precio;
using Application.Commands.Stock;
using Application.DTO;
using Application.Handlers.Stock;
using Application.Interfaces.API;
using Application.Interfaces.HANA;
using Application.Interfaces.Security;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Handlers.Precio
{
    public class SendPrecioToApiCommandHandler : ICommandHandler<SendPrecioToApiCommand, (bool IsSuccess, string? Message)>
    {
        private readonly IApiClient _apiClient;
        private readonly IHanaRepository _hanaRepository;
        private readonly ILogger<SendPrecioToApiCommandHandler> _logger;
        private readonly IApiTokenService _apiTokenService;

        public SendPrecioToApiCommandHandler(
            IApiClient apiClient,
            IHanaRepository hanaRepository,
            IApiTokenService apiTokenService,
            ILogger<SendPrecioToApiCommandHandler> logger)
        {
            _apiClient = apiClient;
            _hanaRepository = hanaRepository;
            _apiTokenService = apiTokenService;
            _logger = logger;
        }

        public async Task<(bool IsSuccess, string? Message)> HandleAsync(SendPrecioToApiCommand command)
        {
            _logger.LogDebug("Enviando precio SapDocEntry: {SapDocEntry} al API", command.Item.SapDocEntry);
            var itemCode = command.Item.SapDocEntry?.Trim();
            var transaction_type = command.Item.TransactionType?.Trim();
            var bodega = command.Item.Bodega?.Trim();

            if (string.IsNullOrWhiteSpace(itemCode))
            {
                return (false, "SapDocEntry/ItemCode vacío en cola");
            }

            var detail = await _hanaRepository.GetPrecioDetailByItemCodeAsync(command.DestinationType, itemCode, bodega);
            if (detail == null)
            {
                return (false, $"No se encontró detalle de precio para ItemCode={itemCode}");
            }

            var payload = BuildWooPayload(command, detail);
            var token = await _apiTokenService.GetAccessTokenAsync();
            var result = await _apiClient.SendPrecioAsync(payload, command.DestinationType, token);


            if (result.IsSuccess)
            {
                _logger.LogDebug("Precio de SapDocEntry: {SapDocEntry} enviado exitosamente al API", command.Item.SapDocEntry);
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

        private static WooPrecioRequestDTO BuildWooPayload(
            SendPrecioToApiCommand command,
            SapItemDetailDTO detail)
        {
            return new WooPrecioRequestDTO
            {
                SKU = (detail.ItemCode ?? command.Item.SapDocEntry ?? string.Empty).Trim(),
                RegularPrice = (detail.regularPrice.ToString() ?? "0.00").Trim(),
            };
        }
    }
}
