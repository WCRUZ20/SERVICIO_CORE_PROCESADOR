using Application.Abstractions;
using Domain.SAP;

namespace Application.Commands
{
    public record VerifyDocumentStatusSAPCommand(int transaction, int docEntry, int docNum, string statusOrder) 
        : ICommand<bool>;
}

