using Application.Abstractions;
using Application.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Commands.Precio.Dealer
{
    public record GetPendingDealerPrecioToApiCommand : ICommand<IEnumerable<SapItemQueeDTO>>;
}
