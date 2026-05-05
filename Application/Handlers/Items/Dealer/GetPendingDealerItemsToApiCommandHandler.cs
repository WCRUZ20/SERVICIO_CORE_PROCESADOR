using Application.Abstractions;
using Application.Commands.Items.Dealer;
using Application.DTO;
using Application.Interfaces.HANA;
using MapsterMapper;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Handlers.Items.Dealer
{
    public class GetPendingDealerItemsToApiCommandHandler
    : ICommandHandler<GetPendingDealerItemsToApiCommand, IEnumerable<SapItemQueeDTO>>
    {
        private readonly IHanaRepository _hanaRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<GetPendingDealerItemsToApiCommandHandler> _logger;

        public GetPendingDealerItemsToApiCommandHandler(
            IHanaRepository hanaRepository,
            IMapper mapper,
            ILogger<GetPendingDealerItemsToApiCommandHandler> logger)
        {
            _hanaRepository = hanaRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<IEnumerable<SapItemQueeDTO>> HandleAsync(GetPendingDealerItemsToApiCommand command)
        {
            _logger.LogInformation("Obteniendo articulos pendientes de DEALER desde HANA -> API");
            return await _hanaRepository.GetPendingDealerItemsAsync<SapItemQueeDTO>();
        }
    }
}
