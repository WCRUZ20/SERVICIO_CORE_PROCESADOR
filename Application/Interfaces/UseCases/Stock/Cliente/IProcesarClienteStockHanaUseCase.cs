using Application.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.UseCases.Stock.Cliente
{
    public interface IProcesarClienteStockHanaUseCase
    {
        Task<ProcesarItemsResult> ExecuteAsync(CancellationToken cancellationToken = default);
    }
}
