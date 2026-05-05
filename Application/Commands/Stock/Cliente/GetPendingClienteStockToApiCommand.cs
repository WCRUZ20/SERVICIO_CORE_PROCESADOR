using Application.Abstractions;
using Application.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Commands.Stock.Cliente
{
    public record GetPendingClienteStockToApiCommand : ICommand<IEnumerable<SapItemQueeDTO>>;
}
