using Application.Abstractions;
using Application.Commands.Precio.Cliente;
using Application.Commands.Precio.Dealer;
using Application.DTO;
using Application.Handlers.Precio.Cliente;
using Application.Interfaces.HANA;
using MapsterMapper;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Handlers.Precio.Dealer
{
    public class GetPendingDealerPrecioToApiCommandHandler : ICommandHandler<GetPendingDealerPrecioToApiCommand, IEnumerable<SapItemQueeDTO>>
    {
        private readonly IHanaRepository _hanaRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<GetPendingDealerPrecioToApiCommandHandler> _logger;

        public GetPendingDealerPrecioToApiCommandHandler(
            IHanaRepository hanaRepository,
            IMapper mapper,
            ILogger<GetPendingDealerPrecioToApiCommandHandler> logger)
        {
            _hanaRepository = hanaRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<IEnumerable<SapItemQueeDTO>> HandleAsync(GetPendingDealerPrecioToApiCommand command)
        {
            _logger.LogInformation("Obteniendo precio pendientes de DEALER desde HANA -> API");
            return await _hanaRepository.GetPendingDealerPrecioAsync<SapItemQueeDTO>();
        }
    }
}
