using Application.Abstractions;
using Domain.SAP;

namespace Application.Commands.Dealer;

public record GetPendingDealerHanaItemsCommand : ICommand<IEnumerable<SapItemsTable>>;
