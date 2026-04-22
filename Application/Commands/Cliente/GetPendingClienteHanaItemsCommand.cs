using Application.Abstractions;
using Domain.SAP;

namespace Application.Commands.Cliente;

public record GetPendingClienteHanaItemsCommand : ICommand<IEnumerable<SapItemsTable>>;
