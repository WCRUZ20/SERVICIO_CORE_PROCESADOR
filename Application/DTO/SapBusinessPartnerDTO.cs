using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTO
{
    public class SapSalesOrderLookupDTO
    {
        public string DocEntry { get; set; } = string.Empty;
        public string DocNum { get; set; } = string.Empty;
    }

    public class SapBusinessPartnerLookupDTO
    {
        public string CardCode { get; set; } = string.Empty;
        public string CardName { get; set; } = string.Empty;
        public string LicTradNum { get; set; } = string.Empty;
    }

    public class SapBusinessPartnerCreateRequest
    {
        public string CardCode { get; set; } = string.Empty;
        public string CardName { get; set; } = string.Empty;
        public string Identification { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public List<SapBusinessPartnerAddressDTO> Addresses { get; set; } = new();
    }

    public class SapBusinessPartnerAddressDTO
    {
        public string AddressName { get; set; } = string.Empty;
        public string AddressType { get; set; } = string.Empty;
        public string Street { get; set; } = string.Empty;
        public string Block { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string ZipCode { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
    }

    public class SapCreateBusinessPartnerResult
    {
        public bool IsSuccess { get; set; }
        public string CardCode { get; set; } = string.Empty;
        public string? Message { get; set; }
    }

}
