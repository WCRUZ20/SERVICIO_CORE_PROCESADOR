using Application.Abstractions;
using Application.Commands.Items.Cliente;
using Application.Commands.Stock.Cliente;
using Application.DTO;
using Application.Interfaces.HANA;
using Domain.SAP;
using MapsterMapper;
using Microsoft.Extensions.Logging;

namespace Application.Handlers.Items.Cliente;

public class GetPendingClienteStockTypeCommandHandler
    : ICommandHandler<GetPendingClienteStockTypeCommand, IEnumerable<SapItemQueeDTO>>
{
    private readonly IHanaRepository _hanaRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetPendingClienteStockTypeCommandHandler> _logger;

    public GetPendingClienteStockTypeCommandHandler(
        IHanaRepository hanaRepository,
        IMapper mapper,
        ILogger<GetPendingClienteStockTypeCommandHandler> logger)
    {
        _hanaRepository = hanaRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IEnumerable<SapItemQueeDTO>> HandleAsync(GetPendingClienteStockTypeCommand command)
    {
        _logger.LogInformation("Obteniendo stock pendientes de CLIENTE");
        var entities = await _hanaRepository.GetUpdateClienteItemsTypeAsync();
        var entityList = entities.ToList();

        _logger.LogInformation("Se encontraron {Count} stock pendientes de CLIENTE", entityList.Count);
        return _mapper.Map<List<SapItemQueeDTO>>(entityList);

        //_logger.LogInformation("Obteniendo articulos pendientes de CLIENTE desde HANA -> API");
        //return await _hanaRepository.GetPendingClienteItemsAsync<SapItemsTable>();
    }
}
