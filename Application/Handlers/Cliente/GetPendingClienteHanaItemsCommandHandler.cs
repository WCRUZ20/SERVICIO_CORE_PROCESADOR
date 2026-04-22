using Application.Abstractions;
using Application.Commands.Cliente;
using Application.Interfaces.HANA;
using Domain.SAP;
using Microsoft.Extensions.Logging;

namespace Application.Handlers.Cliente;

public class GetPendingClienteHanaItemsCommandHandler
    : ICommandHandler<GetPendingClienteHanaItemsCommand, IEnumerable<SapItemsTable>>
{
    private readonly IHanaRepository _hanaRepository;
    private readonly ILogger<GetPendingClienteHanaItemsCommandHandler> _logger;

    public GetPendingClienteHanaItemsCommandHandler(
        IHanaRepository hanaRepository,
        ILogger<GetPendingClienteHanaItemsCommandHandler> logger)
    {
        _hanaRepository = hanaRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<SapItemsTable>> HandleAsync(GetPendingClienteHanaItemsCommand command)
    {
        _logger.LogInformation("Obteniendo articulos pendientes de CLIENTE desde HANA -> API");
        return await _hanaRepository.GetPendingClienteItemsAsync<SapItemsTable>();
    }
}
