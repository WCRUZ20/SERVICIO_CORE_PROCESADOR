using Application.Abstractions;
using Application.Commands;
using Application.DTO;
using Application.Interfaces.API;
using Application.Interfaces.HANA;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Handlers
{
    public class SendItemsToApiCommandHandler
        : ICommandHandler<SendItemsToApiCommand, (bool IsSuccess, string? Message)>
    {
        private readonly IApiClient _apiClient;
        private readonly IHanaRepository _hanaRepository;
        private readonly ILogger<SendItemsToApiCommandHandler> _logger;

        public SendItemsToApiCommandHandler(
            IApiClient apiClient,
            IHanaRepository hanaRepository,
            ILogger<SendItemsToApiCommandHandler> logger)
        {
            _apiClient = apiClient;
            _hanaRepository = hanaRepository;
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
            var result = await _apiClient.SendItemAsync(payload, command.DestinationType);


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
                RegularPrice = regularPrice,
                StockQuantity = Math.Max(0, (int)Math.Round(command.Item.Stock, MidpointRounding.AwayFromZero)),
                Description = (detail.ItemName ?? command.Item.Name ?? string.Empty).Trim(),
                ManageStock = true,
                ShortDescription = (command.Item.Name ?? detail.ItemName ?? string.Empty).Trim()
            };
        }

    }

}
