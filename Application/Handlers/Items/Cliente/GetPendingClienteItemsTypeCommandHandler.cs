using Application.Abstractions;
using Application.Commands.Items.Cliente;
using Application.DTO;
using Application.Interfaces.HANA;
using Domain.SAP;
using MapsterMapper;
using Microsoft.Extensions.Logging;

namespace Application.Handlers.Items.Cliente;

public class GetPendingClienteItemsTypeCommandHandler
    : ICommandHandler<GetPendingClienteItemsTypeCommand, IEnumerable<SapItemQueeDTO>>
{
    private readonly IHanaRepository _hanaRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetPendingClienteItemsTypeCommandHandler> _logger;

    public GetPendingClienteItemsTypeCommandHandler(
        IHanaRepository hanaRepository,
        IMapper mapper,
        ILogger<GetPendingClienteItemsTypeCommandHandler> logger)
    {
        _hanaRepository = hanaRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IEnumerable<SapItemQueeDTO>> HandleAsync(GetPendingClienteItemsTypeCommand command)
    {
        _logger.LogInformation("Obteniendo articulos pendientes de CLIENTE");
        var entities = await _hanaRepository.GetPendingClienteItemsTypeAsync();
        var entityList = entities.ToList();

        _logger.LogInformation("Se encontraron {Count} articulos pendientes de CLIENTE", entityList.Count);
        return _mapper.Map<List<SapItemQueeDTO>>(entityList);

        //_logger.LogInformation("Obteniendo articulos pendientes de CLIENTE desde HANA -> API");
        //return await _hanaRepository.GetPendingClienteItemsAsync<SapItemsTable>();
    }
}
