using Application.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.UseCases.Stock.Dealer
{
    public interface IGetDealerStockSapUseCase
    {
        Task<ObtenerItemsResult> ExecuteAsync(CancellationToken cancellationToken = default);
    }
}
