using Application.Abstractions;

namespace Application.Commands
{
    public record ChangeStatusDocumentSAPCommand(
        int Transaction, 
        int docEntry, 
        int docNum, 
        string statusOrder) 
        : ICommand<bool>;
}

