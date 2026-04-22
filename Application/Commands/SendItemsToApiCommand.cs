using Application.Abstractions;
using Domain.SAP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Commands
{
    public enum ItemDestinationType
    {
        Cliente = 0,
        Dealer = 1
    }

    public record SendItemsToApiCommand(
        SapItemsTable Item,
        ItemDestinationType DestinationType = ItemDestinationType.Cliente)
        : ICommand<(bool IsSuccess, string? Message)>;
}
