using Application.Abstractions;
using Application.Commands.Items.Cliente;
using Application.Commands.Order.Cliente;
using Application.DTO;
using Application.Handlers.Items.Cliente;
using Application.Interfaces.API;
using Application.Interfaces.HANA;
using MapsterMapper;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Handlers.Order.Cliente
{
    public class GetClienteOrdersToUpdateWooHandler : ICommandHandler<GetClienteOrdersToUpdateWooCommand, IEnumerable<SapItemQueeDTO>>
    {
        private readonly IHanaRepository _hanaRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<GetClienteOrdersToUpdateWooHandler> _logger;

        public GetClienteOrdersToUpdateWooHandler(
            IHanaRepository hanaRepository,
            IMapper mapper,
            ILogger<GetClienteOrdersToUpdateWooHandler> logger)
        {
            _hanaRepository = hanaRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<IEnumerable<SapItemQueeDTO>> HandleAsync(GetClienteOrdersToUpdateWooCommand command)
        {
            _logger.LogInformation("Obteniendo ordenes pendientes de CLIENTE desde COLA para posible creacion en SAP");
            return await _hanaRepository.GetPendingClienteOrdersAsync<SapItemQueeDTO>();
        }
    }
}
