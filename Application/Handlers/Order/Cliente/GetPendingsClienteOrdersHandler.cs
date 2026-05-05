using Application.Abstractions;
using Application.Commands.Order.Cliente;
using Application.DTO;
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

namespace Application.Handlers.Order.Cliente
{
    public class GetPendingsClienteOrdersHandler : ICommandHandler<GetPendingsClienteOrdersCommand, IEnumerable<WooOrderDTO>>
    {
        private readonly IHanaRepository _hanaRepository;
        private readonly IApiClient _apiClient;
        private readonly IApiTokenService _apiTokenService;
        private readonly IMapper _mapper;
        private readonly ILogger<GetPendingsClienteOrdersHandler> _logger;

        public GetPendingsClienteOrdersHandler(
            IHanaRepository hanaRepository,
            IApiClient apiClient,
            IApiTokenService apiTokenService,
            IMapper mapper,
            ILogger<GetPendingsClienteOrdersHandler> logger)
        {
            _hanaRepository = hanaRepository;
            _apiClient = apiClient;
            _apiTokenService = apiTokenService;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<IEnumerable<WooOrderDTO>> HandleAsync(GetPendingsClienteOrdersCommand command)
        {
            _logger.LogInformation("Obteniendo órdenes CLIENTE desde WooCommerce");

            var token = await _apiTokenService.GetAccessTokenAsync();
            var result = await _apiClient.GetClienteOrdersAsync(token);

            if (!result.IsSuccess)
            {
                _logger.LogWarning(
                    "No se pudieron obtener órdenes CLIENTE desde WooCommerce. Error: {Error}",
                    result.Message);

                return Enumerable.Empty<WooOrderDTO>();
            }

            var orders = result.Orders.ToList();

            _logger.LogInformation(
                "Se obtuvieron {Count} órdenes CLIENTE desde WooCommerce",
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
