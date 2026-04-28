using Application.Abstractions;
using Application.DTO;
using Domain.SAP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Commands.Items
{
    public enum ItemDestinationType
    {
        Cliente = 0,
        Dealer = 1
    }

    public record SendItemsToApiCommand(
        SapItemQueeDTO Item,
        ItemDestinationType DestinationType = ItemDestinationType.Cliente)
        : ICommand<(bool IsSuccess, string? Message)>;
}
