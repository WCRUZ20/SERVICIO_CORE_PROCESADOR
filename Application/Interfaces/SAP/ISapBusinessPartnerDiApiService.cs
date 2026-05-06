using Application.DTO;
using SAPbobsCOM;

namespace Application.Interfaces.SAP
{
    public interface ISapBusinessPartnerDiApiService
    {
        Task<SapBusinessPartnerLookupDTO?> GetCustomerByIdentificationAsync(
            Company company,
            string identification,
            CancellationToken cancellationToken = default);

        Task<SapCreateBusinessPartnerResult> CreateCustomerAsync(
            Company company,
            SapBusinessPartnerCreateRequest request,
            CancellationToken cancellationToken = default);
    }
}

