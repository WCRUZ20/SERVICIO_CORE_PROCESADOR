using Application.Abstractions;
using Application.Commands.Items;
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

namespace Application.Handlers.Items
{
    public class SendItemsToApiCommandHandler
        : ICommandHandler<SendItemsToApiCommand, (bool IsSuccess, string? Message)>
    {
        private readonly IApiClient _apiClient;
        private readonly IHanaRepository _hanaRepository;
        private readonly ILogger<SendItemsToApiCommandHandler> _logger;
        private readonly IApiTokenService _apiTokenService;

        public SendItemsToApiCommandHandler(
            IApiClient apiClient,
            IHanaRepository hanaRepository,
            IApiTokenService apiTokenService,
            ILogger<SendItemsToApiCommandHandler> logger)
        {
            _apiClient = apiClient;
            _hanaRepository = hanaRepository;
            _apiTokenService = apiTokenService;
            _logger = logger;
        }

        public async Task<(bool IsSuccess, string? Message)> HandleAsync(SendItemsToApiCommand command)
        {
            _logger.LogDebug("Enviando articulo SapDocEntry: {SapDocEntry} al API", command.Item.SapDocEntry);
            var itemCode = command.Item.SapDocEntry?.Trim();
            if (string.IsNullOrWhiteSpace(itemCode))
            {
                return (false, "SapDocEntry/ItemCode vacío en cola");
            }

            var detail = await _hanaRepository.GetItemDetailByItemCodeAsync(itemCode);
            if (detail == null)
            {
                return (false, $"No se encontró detalle de artículo para ItemCode={itemCode}");
            }

            var payload = BuildWooPayload(command, detail);
            var token = await _apiTokenService.GetAccessTokenAsync();
            var result = await _apiClient.SendItemAsync(payload, command.DestinationType, token);


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

        private static WooProductRequestDTO BuildWooPayload(
            SendItemsToApiCommand command,
            SapItemDetailDTO detail)
        {
            var price = detail.Price.GetValueOrDefault();
            var regularPrice = price > 0
                ? price.ToString("0.00", CultureInfo.InvariantCulture)
                : "0.00";

            return new WooProductRequestDTO
            {
                Name = (detail.ItemName ?? command.Item.Name ?? string.Empty).Trim(),
                SKU = (detail.ItemCode ?? command.Item.SapDocEntry ?? string.Empty).Trim(),
                Status = "draft",
                RegularPrice = (detail.regularPrice ?? "0.00").Trim(),
                StockQuantity = Math.Max(0, (int)Math.Round(command.Item.Stock, MidpointRounding.AwayFromZero)),
                Description = (detail.description ?? string.Empty).Trim(),
                ManageStock = detail.manageStock,
                ShortDescription = (detail.shortDescription ?? string.Empty).Trim()
            };
        }

    }

}
