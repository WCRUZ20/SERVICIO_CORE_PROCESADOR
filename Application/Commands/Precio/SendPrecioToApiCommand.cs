using Application.Abstractions;
using Application.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Commands.Precio
{
    public enum ItemDestinationType
    {
        Cliente = 2,
        Dealer = 1
    }

    public record SendPrecioToApiCommand(
        SapItemQueeDTO Item,
        ItemDestinationType DestinationType = ItemDestinationType.Cliente)
        : ICommand<(bool IsSuccess, string? Message)>;
}
