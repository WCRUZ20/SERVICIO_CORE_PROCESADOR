using Application.Abstractions;

namespace Application.Commands
{
    public record CheckDocumentExistsCommand(int transaction,int docEntry,int docNum) 
        : ICommand<bool>;
}

