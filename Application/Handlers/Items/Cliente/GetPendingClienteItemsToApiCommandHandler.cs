using Application.Abstractions;
using Application.Commands.Items.Cliente;
using Application.DTO;
using Application.Interfaces.HANA;
using MapsterMapper;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Handlers.Items.Cliente
{
    public class GetPendingClienteItemsToApiCommandHandler
        : ICommandHandler<GetPendingClienteItemsToApiCommand, IEnumerable<SapItemQueeDTO>>
    {
        private readonly IHanaRepository _hanaRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<GetPendingClienteItemsToApiCommandHandler> _logger;

        public GetPendingClienteItemsToApiCommandHandler(
            IHanaRepository hanaRepository,
            IMapper mapper,
            ILogger<GetPendingClienteItemsToApiCommandHandler> logger)
        {
            _hanaRepository = hanaRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<IEnumerable<SapItemQueeDTO>> HandleAsync(GetPendingClienteItemsToApiCommand command)
        {
            _logger.LogInformation("Obteniendo articulos pendientes de CLIENTE desde HANA -> API");
            return await _hanaRepository.GetPendingClienteItemsAsync<SapItemQueeDTO>();
        }
    }
}
