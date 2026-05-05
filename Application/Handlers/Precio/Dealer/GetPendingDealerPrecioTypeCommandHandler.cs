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
    public class GetPendingDealerPrecioTypeCommandHandler : ICommandHandler<GetPendingDealerPrecioTypeCommand, IEnumerable<SapItemQueeDTO>>
    {
        private readonly IHanaRepository _hanaRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<GetPendingDealerPrecioTypeCommandHandler> _logger;

        public GetPendingDealerPrecioTypeCommandHandler(
            IHanaRepository hanaRepository,
            IMapper mapper,
            ILogger<GetPendingDealerPrecioTypeCommandHandler> logger)
        {
            _hanaRepository = hanaRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<IEnumerable<SapItemQueeDTO>> HandleAsync(GetPendingDealerPrecioTypeCommand command)
        {
            _logger.LogInformation("Obteniendo precio pendientes de DEALER");
            var entities = await _hanaRepository.GetUpdateDealerPrecioTypeAsync(); //GetPendingClienteItemsAsync
            var entityList = entities.ToList();

            _logger.LogInformation("Se encontraron {Count} precio pendientes de DEALER", entityList.Count);
            return _mapper.Map<List<SapItemQueeDTO>>(entityList);

            //_logger.LogInformation("Obteniendo articulos pendientes de CLIENTE desde HANA -> API");
            //return await _hanaRepository.GetPendingClienteItemsAsync<SapItemsTable>();
        }
    }
}
