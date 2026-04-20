using Application.Abstractions;
using Domain.SAP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Commands
{
    public record SendItemsToApiCommand(SapItemsTable Item)
        : ICommand<(bool IsSuccess, string? Message)>;
}
