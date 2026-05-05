using Application.Abstractions;
using Application.Commands.Items.Dealer;
using Application.Commands.Stock.Cliente;
using Application.DTO;
using Application.Interfaces.HANA;
using MapsterMapper;
using Microsoft.Extensions.Logging;

namespace Application.Handlers.Items.Dealer;

public class GetPendingDealerStockTypeCommandHandler
    : ICommandHandler<GetPendingDealerStockTypeCommand, IEnumerable<SapItemQueeDTO>>
{
    private readonly IHanaRepository _hanaRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetPendingDealerStockTypeCommandHandler> _logger;

    public GetPendingDealerStockTypeCommandHandler(
        IHanaRepository hanaRepository,
        IMapper mapper,
        ILogger<GetPendingDealerStockTypeCommandHandler> logger)
    {
        _hanaRepository = hanaRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IEnumerable<SapItemQueeDTO>> HandleAsync(GetPendingDealerStockTypeCommand command)
    {
        _logger.LogInformation("Obteniendo stock pendientes de DEALER");
        var entities = await _hanaRepository.GetUpdateDealerItemsTypeAsync();
        var entityList = entities.ToList();

        _logger.LogInformation("Se encontraron {Count} stock pendientes de DEALER", entityList.Count);
        return _mapper.Map<List<SapItemQueeDTO>>(entityList);
    }
}
