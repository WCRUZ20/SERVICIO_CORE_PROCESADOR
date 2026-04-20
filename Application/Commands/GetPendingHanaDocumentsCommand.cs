using Application.Abstractions;
using Domain.SAP;
using System;
using System.Collections.Generic;

namespace Application.Commands
{
    public record GetPendingHanaDocumentsCommand() 
        : ICommand<IEnumerable<SapDrivinTable>>;
}

