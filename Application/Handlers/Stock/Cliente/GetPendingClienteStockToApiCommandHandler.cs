using Application.Abstractions;
using Application.Commands.Items.Cliente;
using Application.Commands.Stock.Cliente;
using Application.DTO;
using Application.Interfaces.HANA;
using MapsterMapper;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Handlers.Stock.Cliente
{
    public class GetPendingClienteStockToApiCommandHandler
        : ICommandHandler<GetPendingClienteStockToApiCommand, IEnumerable<SapItemQueeDTO>>
    {
        private readonly IHanaRepository _hanaRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<GetPendingClienteStockToApiCommandHandler> _logger;

        public GetPendingClienteStockToApiCommandHandler(
            IHanaRepository hanaRepository,
            IMapper mapper,
            ILogger<GetPendingClienteStockToApiCommandHandler> logger)
        {
            _hanaRepository = hanaRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<IEnumerable<SapItemQueeDTO>> HandleAsync(GetPendingClienteStockToApiCommand command)
        {
            _logger.LogInformation("Obteniendo stock pendientes de CLIENTE desde HANA -> API");
            return await _hanaRepository.GetPendingClienteStockAsync<SapItemQueeDTO>();
        }
    }
}
