using Application.Abstractions;
using Application.Commands.Dealer;
using Application.Interfaces.HANA;
using Domain.SAP;
using Microsoft.Extensions.Logging;

namespace Application.Handlers.Dealer;

public class GetPendingDealerHanaItemsCommandHandler
    : ICommandHandler<GetPendingDealerHanaItemsCommand, IEnumerable<SapItemsTable>>
{
    private readonly IHanaRepository _hanaRepository;
    private readonly ILogger<GetPendingDealerHanaItemsCommandHandler> _logger;

    public GetPendingDealerHanaItemsCommandHandler(
        IHanaRepository hanaRepository,
        ILogger<GetPendingDealerHanaItemsCommandHandler> logger)
    {
        _hanaRepository = hanaRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<SapItemsTable>> HandleAsync(GetPendingDealerHanaItemsCommand command)
    {
        _logger.LogInformation("Obteniendo articulos pendientes de DEALER desde HANA -> API");
        return await _hanaRepository.GetPendingDealerItemsAsync<SapItemsTable>();
    }
}
