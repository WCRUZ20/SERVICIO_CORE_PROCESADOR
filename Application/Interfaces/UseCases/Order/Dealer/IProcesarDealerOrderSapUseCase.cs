using Application.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.UseCases.Order.Dealer
{
    public interface IProcesarDealerOrderSapUseCase
    {
        Task<ProcesarItemsResult> ExecuteAsync(CancellationToken cancellationToken = default);
    }
}
