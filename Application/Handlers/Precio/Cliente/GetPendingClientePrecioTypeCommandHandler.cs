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
    public class GetPendingClientePrecioTypeCommandHandler
    : ICommandHandler<GetPendingClientePrecioTypeCommand, IEnumerable<SapItemQueeDTO>>
    {
        private readonly IHanaRepository _hanaRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<GetPendingClientePrecioTypeCommandHandler> _logger;

        public GetPendingClientePrecioTypeCommandHandler(
            IHanaRepository hanaRepository,
            IMapper mapper,
            ILogger<GetPendingClientePrecioTypeCommandHandler> logger)
        {
            _hanaRepository = hanaRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<IEnumerable<SapItemQueeDTO>> HandleAsync(GetPendingClientePrecioTypeCommand command)
        {
            _logger.LogInformation("Obteniendo precio pendientes de CLIENTE");
            var entities = await _hanaRepository.GetUpdateClientePrecioTypeAsync(); //GetPendingClienteItemsAsync
            var entityList = entities.ToList();

            _logger.LogInformation("Se encontraron {Count} precio pendientes de CLIENTE", entityList.Count);
            return _mapper.Map<List<SapItemQueeDTO>>(entityList);

            //_logger.LogInformation("Obteniendo articulos pendientes de CLIENTE desde HANA -> API");
            //return await _hanaRepository.GetPendingClienteItemsAsync<SapItemsTable>();
        }
    }
}
