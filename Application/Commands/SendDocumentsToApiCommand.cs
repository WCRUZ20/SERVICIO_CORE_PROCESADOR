using Application.Abstractions;
using Domain.SAP;

namespace Application.Commands
{
    public record SendDocumentsToApiCommand(SapDrivinTable Document) 
        : ICommand<(bool IsSuccess, string? Message)>;
}

