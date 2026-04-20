using Application.Abstractions;
using Application.DTO;
using Domain.SAP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Commands
{
    public record GetPendingItemsTypeCommand : ICommand<IEnumerable<SapItemQueeDTO>>;

}
