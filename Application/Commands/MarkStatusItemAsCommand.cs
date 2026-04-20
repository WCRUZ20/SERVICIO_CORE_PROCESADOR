using Application.Abstractions;
using Domain.SAP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Commands
{
    public record MarkStatusItemAsCommand(SapItemsTable Item)
        : ICommand<bool>;
}
