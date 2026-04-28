using Application.Abstractions;
using Application.DTO;

namespace Application.Commands.Items.Cliente;

public record GetPendingClienteItemsTypeCommand : ICommand<IEnumerable<SapItemQueeDTO>>;
