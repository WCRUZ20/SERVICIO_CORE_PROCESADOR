using Application.Abstractions;
using Application.DTO;
using Domain.SAP;
using System;
using System.Collections.Generic;

namespace Application.Commands
{
    public record GetPendingDocumentsTypeCommand() 
        : ICommand<IEnumerable<SapDrivinTableDTO>>;
}

