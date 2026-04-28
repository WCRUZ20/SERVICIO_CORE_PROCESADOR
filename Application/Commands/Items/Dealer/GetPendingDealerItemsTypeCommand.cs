using Application.Abstractions;
using Application.DTO;

namespace Application.Commands.Items.Dealer;

public record GetPendingDealerItemsTypeCommand : ICommand<IEnumerable<SapItemQueeDTO>>;
