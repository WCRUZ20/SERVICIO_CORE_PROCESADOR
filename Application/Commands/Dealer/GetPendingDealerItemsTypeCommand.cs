using Application.Abstractions;
using Application.DTO;

namespace Application.Commands.Dealer;

public record GetPendingDealerItemsTypeCommand : ICommand<IEnumerable<SapItemQueeDTO>>;
