using Application.Abstractions;
using Application.Commands.Dealer;
using Application.DTO;
using Application.Interfaces.HANA;
using MapsterMapper;
using Microsoft.Extensions.Logging;

namespace Application.Handlers.Dealer;

public class GetPendingDealerItemsTypeCommandHandler
    : ICommandHandler<GetPendingDealerItemsTypeCommand, IEnumerable<SapItemQueeDTO>>
{
    private readonly IHanaRepository _hanaRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetPendingDealerItemsTypeCommandHandler> _logger;

    public GetPendingDealerItemsTypeCommandHandler(
        IHanaRepository hanaRepository,
        IMapper mapper,
        ILogger<GetPendingDealerItemsTypeCommandHandler> logger)
    {
        _hanaRepository = hanaRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IEnumerable<SapItemQueeDTO>> HandleAsync(GetPendingDealerItemsTypeCommand command)
    {
        _logger.LogInformation("Obteniendo articulos pendientes de DEALER");
        var entities = await _hanaRepository.GetPendingDealerItemsTypeAsync();
        var entityList = entities.ToList();

        _logger.LogInformation("Se encontraron {Count} articulos pendientes de DEALER", entityList.Count);
        return _mapper.Map<List<SapItemQueeDTO>>(entityList);
    }
}
