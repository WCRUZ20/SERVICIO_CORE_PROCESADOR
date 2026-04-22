using Application.Abstractions;
using Application.DTO;

namespace Application.Commands.Cliente;

public record GetPendingClienteItemsTypeCommand : ICommand<IEnumerable<SapItemQueeDTO>>;
