using Application.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.UseCases.Precio.Cliente
{
    public interface IGetClientePrecioSapUseCase
    {
        Task<ObtenerItemsResult> ExecuteAsync(CancellationToken cancellationToken = default);
    }
}
