using Application.DTO;
using Application.Interfaces.SAP;
using Microsoft.Extensions.Logging;
using SAPbobsCOM;
using System.Runtime.InteropServices;

namespace Infrastructure.SAP
{
    public sealed class SapBusinessPartnerDiApiService : ISapBusinessPartnerDiApiService
    {
        private readonly ILogger<SapBusinessPartnerDiApiService> _logger;

        public SapBusinessPartnerDiApiService(ILogger<SapBusinessPartnerDiApiService> logger)
        {
            _logger = logger;
        }

        public Task<SapBusinessPartnerLookupDTO?> GetCustomerByIdentificationAsync(
            Company company,
            string identification,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(identification))
                return Task.FromResult<SapBusinessPartnerLookupDTO?>(null);

            Recordset? rs = null;
            try
            {
                rs = (Recordset)company.GetBusinessObject(BoObjectTypes.BoRecordset);
                var id = identification.Trim().Replace("'", "''");
                rs.DoQuery(
                    $"SELECT TOP 1 \"CardCode\", \"CardName\", \"LicTradNum\" " +
                    $"FROM \"OCRD\" WHERE \"LicTradNum\" = '{id}' AND \"CardType\" = 'C' " +
                    $"ORDER BY \"CardCode\"");

                if (rs.EoF)
                    return Task.FromResult<SapBusinessPartnerLookupDTO?>(null);

                var cardCode = rs.Fields.Item("CardCode").Value?.ToString() ?? string.Empty;
                var cardName = rs.Fields.Item("CardName").Value?.ToString() ?? string.Empty;
                var licTradNum = rs.Fields.Item("LicTradNum").Value?.ToString() ?? string.Empty;

                return Task.FromResult<SapBusinessPartnerLookupDTO?>(new SapBusinessPartnerLookupDTO
                {
                    CardCode = cardCode,
                    CardName = cardName,
                    LicTradNum = licTradNum
                });
            }
            finally
            {
                SafeRelease(rs);
            }
        }

        public Task<SapCreateBusinessPartnerResult> CreateCustomerAsync(
            Company company,
            SapBusinessPartnerCreateRequest request,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(request.CardCode) || string.IsNullOrWhiteSpace(request.Identification))
            {
                return Task.FromResult(new SapCreateBusinessPartnerResult
                {
                    IsSuccess = false,
                    CardCode = request.CardCode,
                    Message = "CardCode o identificación vacía para crear socio de negocio."
                });
            }

            BusinessPartners? bp = null;
            try
            {
                bp = (BusinessPartners)company.GetBusinessObject(BoObjectTypes.oBusinessPartners);

                bp.CardCode = request.CardCode.Trim();
                bp.CardName = string.IsNullOrWhiteSpace(request.CardName) ? request.CardCode.Trim() : request.CardName.Trim();
                bp.CardType = BoCardTypes.cCustomer;
                bp.FederalTaxID = request.Identification.Trim();

                if (!string.IsNullOrWhiteSpace(request.Phone))
                    bp.Phone1 = request.Phone.Trim();
                if (!string.IsNullOrWhiteSpace(request.Email))
                    bp.EmailAddress = request.Email.Trim();

                if (request.Addresses != null && request.Addresses.Count > 0)
                {
                    var idx = 0;
                    foreach (var address in request.Addresses)
                    {
                        if (idx > 0)
                            bp.Addresses.Add();

                        bp.Addresses.AddressName = address.AddressName ?? string.Empty;
                        bp.Addresses.AddressType = (BoAddressType)(address.AddressType?.Equals("bo_ShipTo", StringComparison.OrdinalIgnoreCase) == true
                            ? BoAddressType.bo_ShipTo
                            : BoAddressType.bo_BillTo);
                        bp.Addresses.Street = address.Street ?? string.Empty;
                        bp.Addresses.Block = address.Block ?? string.Empty;
                        bp.Addresses.City = address.City ?? string.Empty;
                        bp.Addresses.State = address.State ?? string.Empty;
                        bp.Addresses.ZipCode = address.ZipCode ?? string.Empty;
                        bp.Addresses.Country = address.Country ?? string.Empty;
                        idx++;
                    }
                }

                var rc = bp.Add();
                if (rc != 0)
                {
                    company.GetLastError(out var code, out var message);
                    _logger.LogWarning("Error creando BP por DI API. Code={Code}. Message={Message}", code, message);
                    return Task.FromResult(new SapCreateBusinessPartnerResult
                    {
                        IsSuccess = false,
                        CardCode = request.CardCode,
                        Message = $"DI API Add(BusinessPartner) falló. Code={code}. Message={message}"
                    });
                }

                _logger.LogInformation("BP creado en SAP por DI API. CardCode={CardCode}", request.CardCode);
                return Task.FromResult(new SapCreateBusinessPartnerResult
                {
                    IsSuccess = true,
                    CardCode = request.CardCode,
                    Message = "Socio de negocio creado correctamente por DI API."
                });
            }
            finally
            {
                SafeRelease(bp);
            }
        }

        private static void SafeRelease(object? comObject)
        {
            if (comObject == null) return;
            try
            {
                if (Marshal.IsComObject(comObject))
                    Marshal.FinalReleaseComObject(comObject);
            }
            catch
            {
                // Ignorar.
            }
        }
    }
}

