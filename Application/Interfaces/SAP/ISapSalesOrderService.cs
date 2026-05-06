using Application.DTO;
using SAPbobsCOM;

namespace Application.Interfaces.SAP
{
    public interface ISapSalesOrderService
    {
        Task<SapCreateSalesOrderResult> CreateSalesOrderFromWooAsync(
            Company company,
            string wooId,
            WooOrderDTO wooOrder,
            string cardCode,
            CancellationToken cancellationToken = default);
    }

    public class SapCreateSalesOrderResult
    {
        public bool IsSuccess { get; set; }
        public string? DocEntry { get; set; }
        public string? DocNum { get; set; }
        public string? Message { get; set; }
    }
}

