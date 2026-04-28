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
    public record InsertStockCommand(SapItemQueeDTO item)
        : ICommand<bool>;
}
