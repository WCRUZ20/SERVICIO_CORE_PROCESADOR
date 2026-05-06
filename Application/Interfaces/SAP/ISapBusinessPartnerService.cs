using Application.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.SAP
{
    public interface ISapBusinessPartnerService
    {
        Task<SapSalesOrderLookupDTO?> GetSalesOrderByWooIdAsync(string wooId, CancellationToken cancellationToken = default);

        Task<SapBusinessPartnerLookupDTO?> GetCustomerByIdentificationAsync(string identification, CancellationToken cancellationToken = default);

        Task<SapCreateBusinessPartnerResult> CreateCustomerAsync(SapBusinessPartnerCreateRequest request, CancellationToken cancellationToken = default);
    }
}
