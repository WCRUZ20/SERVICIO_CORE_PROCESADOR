using Application.Abstractions;
using Domain.SAP;

namespace Application.Commands
{
    public record MarkStatusDocuemntAsCommand(SapDrivinTable document) 
        : ICommand<bool>;
}

