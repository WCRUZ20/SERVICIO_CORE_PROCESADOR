using Application.Abstractions;
using Application.Commands.Items;
using Application.Commands.Stock;
using Application.DTO;
using Application.Interfaces.API;
using Application.Interfaces.HANA;
using Application.Interfaces.Security;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Handlers.Stock
{
    public class SendStockToApiCommandHandler
        : ICommandHandler<SendStockToApiCommand, (bool IsSuccess, string? Message)>
    {
        private readonly IApiClient _apiClient;
        private readonly IHanaRepository _hanaRepository;
        private readonly ILogger<SendStockToApiCommandHandler> _logger;
        private readonly IApiTokenService _apiTokenService;

        public SendStockToApiCommandHandler(
            IApiClient apiClient,
            IHanaRepository hanaRepository,
            IApiTokenService apiTokenService,
            ILogger<SendStockToApiCommandHandler> logger)
        {
            _apiClient = apiClient;
            _hanaRepository = hanaRepository;
            _apiTokenService = apiTokenService;
            _logger = logger;
        }

        public async Task<(bool IsSuccess, string? Message)> HandleAsync(SendStockToApiCommand command)
        {
            _logger.LogDebug("Enviando stock SapDocEntry: {SapDocEntry} al API", command.Item.SapDocEntry);
            var itemCode = command.Item.SapDocEntry?.Trim();
            var transaction_type = command.Item.TransactionType?.Trim();
            var bodega = command.Item.Bodega?.Trim();

            if (string.IsNullOrWhiteSpace(itemCode))
            {
                return (false, "SapDocEntry/ItemCode vacío en cola");
            }

            var detail = await _hanaRepository.GetStockDetailByItemCodeAsync(command.DestinationType, itemCode, bodega);
            if (detail == null)
            {
                return (false, $"No se encontró detalle de stock para ItemCode={itemCode}");
            }

            var payload = BuildWooPayload(command, detail);
            var token = await _apiTokenService.GetAccessTokenAsync();
            var result = await _apiClient.SendStockAsync(payload, command.DestinationType, token);


            if (result.IsSuccess)
            {
                _logger.LogDebug("Stock de SapDocEntry: {SapDocEntry} enviado exitosamente al API", command.Item.SapDocEntry);
            }
            else
            {
                _logger.LogWarning(
                    "Error al enviar stock de SapDocEntry: {SapDocEntry} al API. Error: {Error}",
                    command.Item.SapDocEntry,
                    result.Message);
            }

            return result;
        }

        private static WooStockRequestDTO BuildWooPayload(
            SendStockToApiCommand command,
            SapItemDetailDTO detail)
        {
            return new WooStockRequestDTO
            {
                SKU = (detail.ItemCode ?? command.Item.SapDocEntry ?? string.Empty).Trim(),
                StockQuantity = Math.Max(0, (int)Math.Round(command.Item.Stock, MidpointRounding.AwayFromZero)),
            };
        }

    }
}
