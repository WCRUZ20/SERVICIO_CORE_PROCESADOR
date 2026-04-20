using Application.Abstractions;
using Application.DTO;
using Domain.SAP;

namespace Application.Commands
{
    public record InsertDocumentCommand(SapDrivinTableDTO document) 
        : ICommand<bool>;
}

