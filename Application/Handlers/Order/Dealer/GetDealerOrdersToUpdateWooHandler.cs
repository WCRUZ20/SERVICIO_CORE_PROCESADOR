using Application.Abstractions;
using Application.Commands.Order.Cliente;
using Application.Commands.Order.Dealer;
using Application.DTO;
using Application.Handlers.Order.Cliente;
using Application.Interfaces.HANA;
using MapsterMapper;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Handlers.Order.Dealer
{
    public class GetDealerOrdersToUpdateWooHandler : ICommandHandler<GetDealerOrdersToUpdateWoo, IEnumerable<SapItemQueeDTO>>
    {
        private readonly IHanaRepository _hanaRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<GetDealerOrdersToUpdateWooHandler> _logger;

        public GetDealerOrdersToUpdateWooHandler(
            IHanaRepository hanaRepository,
            IMapper mapper,
            ILogger<GetDealerOrdersToUpdateWooHandler> logger)
        {
            _hanaRepository = hanaRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<IEnumerable<SapItemQueeDTO>> HandleAsync(GetDealerOrdersToUpdateWoo command)
        {
            _logger.LogInformation("Obteniendo ordenes pendientes de CLIENTE desde COLA para posible creacion en SAP");
            return await _hanaRepository.GetPendingDealerOrdersAsync<SapItemQueeDTO>();
        }
    }
}
