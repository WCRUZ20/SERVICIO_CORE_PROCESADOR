using Application.Abstractions;
using Application.Commands.Order.Cliente;
using Application.Commands.Order.Dealer;
using Application.DTO;
using Application.Handlers.Order.Cliente;
using Application.Interfaces.API;
using Application.Interfaces.HANA;
using Application.Interfaces.Security;
using MapsterMapper;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Handlers.Order.Dealer
{
    public class GetPendingsDealerOrdersHandler : ICommandHandler<GetPendingsDealerOrdersCommand, IEnumerable<WooOrderDTO>>
    {
        private readonly IHanaRepository _hanaRepository;
        private readonly IApiClient _apiClient;
        private readonly IApiTokenService _apiTokenService;
        private readonly IMapper _mapper;
        private readonly ILogger<GetPendingsDealerOrdersHandler> _logger;

        public GetPendingsDealerOrdersHandler(
            IHanaRepository hanaRepository,
            IApiClient apiClient,
            IApiTokenService apiTokenService,
            IMapper mapper,
            ILogger<GetPendingsDealerOrdersHandler> logger)
        {
            _hanaRepository = hanaRepository;
            _apiClient = apiClient;
            _apiTokenService = apiTokenService;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<IEnumerable<WooOrderDTO>> HandleAsync(GetPendingsDealerOrdersCommand command)
        {
            _logger.LogInformation("Obteniendo órdenes DEALER desde WooCommerce");

            var token = await _apiTokenService.GetAccessTokenAsync();
            var result = await _apiClient.GetDealerOrdersAsync(token);

            if (!result.IsSuccess)
            {
                _logger.LogWarning(
                    "No se pudieron obtener órdenes DEALER desde WooCommerce. Error: {Error}",
                    result.Message);

                return Enumerable.Empty<WooOrderDTO>();
            }

            var orders = result.Orders.ToList();

            _logger.LogInformation(
                "Se obtuvieron {Count} órdenes DEALER desde WooCommerce",
                orders.Count);

            foreach (var order in orders)
            {
                _logger.LogInformation(
                    "Orden WooCommerce Id={Id}, Number={Number}, Status={Status}, Total={Total}",
                    order.Id,
                    order.Number,
                    order.Status,
                    order.Total);
            }

            return orders;
        }
    }
}
