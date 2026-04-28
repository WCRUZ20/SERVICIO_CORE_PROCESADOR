using Application.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Commands.Items
{
    public record CheckItemExistsCommand(int transaction, string itemCode)
        : ICommand<bool>;
}
