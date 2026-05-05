using Application.Abstractions;
using Application.Commands.Items.Dealer;
using Application.Commands.Stock.Dealer;
using Application.DTO;
using Application.Interfaces.HANA;
using MapsterMapper;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Handlers.Stock.Dealer
{
    public class GetPendingDealerStockToApiCommandHandler
    : ICommandHandler<GetPendingDealerStockToApiCommand, IEnumerable<SapItemQueeDTO>>
    {
        private readonly IHanaRepository _hanaRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<GetPendingDealerStockToApiCommandHandler> _logger;

        public GetPendingDealerStockToApiCommandHandler(
            IHanaRepository hanaRepository,
            IMapper mapper,
            ILogger<GetPendingDealerStockToApiCommandHandler> logger)
        {
            _hanaRepository = hanaRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<IEnumerable<SapItemQueeDTO>> HandleAsync(GetPendingDealerStockToApiCommand command)
        {
            _logger.LogInformation("Obteniendo stock pendientes de DEALER desde HANA -> API");
            return await _hanaRepository.GetPendingDealerStockAsync<SapItemQueeDTO>();
        }
    }
}
