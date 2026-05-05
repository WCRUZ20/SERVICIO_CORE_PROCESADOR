using Application.Abstractions;
using Application.Commands.Items.Cliente;
using Application.Commands.Precio.Cliente;
using Application.DTO;
using Application.Handlers.Items.Cliente;
using Application.Interfaces.HANA;
using MapsterMapper;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Handlers.Precio.Cliente
{
    public class GetPendingClientePrecioToApiCommandHandler : ICommandHandler<GetPendingClientePrecioToApiCommand, IEnumerable<SapItemQueeDTO>>
    {
        private readonly IHanaRepository _hanaRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<GetPendingClientePrecioToApiCommandHandler> _logger;

        public GetPendingClientePrecioToApiCommandHandler(
            IHanaRepository hanaRepository,
            IMapper mapper,
            ILogger<GetPendingClientePrecioToApiCommandHandler> logger)
        {
            _hanaRepository = hanaRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<IEnumerable<SapItemQueeDTO>> HandleAsync(GetPendingClientePrecioToApiCommand command)
        {
            _logger.LogInformation("Obteniendo Precio pendientes de CLIENTE desde HANA -> API");
            return await _hanaRepository.GetPendingClientePrecioAsync<SapItemQueeDTO>();
        }
    }
}
