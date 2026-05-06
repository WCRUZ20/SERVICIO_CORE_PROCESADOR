using Application.DTO;
using Application.Interfaces.SAP;
using Microsoft.Extensions.Logging;
using SAPbobsCOM;
using System.Globalization;
using System.Runtime.InteropServices;

namespace Infrastructure.SAP
{
    public sealed class SapSalesOrderDiApiService : ISapSalesOrderService
    {
        private readonly ILogger<SapSalesOrderDiApiService> _logger;

        public SapSalesOrderDiApiService(ILogger<SapSalesOrderDiApiService> logger)
        {
            _logger = logger;
        }

        public async Task<SapCreateSalesOrderResult> CreateSalesOrderFromWooAsync(
            Company company,
            string wooId,
            WooOrderDTO wooOrder,
            string cardCode,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(wooId))
                return Fail("wooId vacío para crear Orden de Venta.");
            if (wooOrder == null)
                return Fail("Orden Woo nula para crear Orden de Venta.");
            if (string.IsNullOrWhiteSpace(cardCode))
                return Fail("CardCode vacío para crear Orden de Venta.");
            if (company == null)
                return Fail("Company DI API nula para crear Orden de Venta.");

            if (wooOrder.LineItems == null || wooOrder.LineItems.Count == 0)
                return Fail($"Orden Woo {wooId} no tiene line_items.");

            Documents? order = null;
            Recordset? rs = null;
            try
            {
                order = (Documents)company.GetBusinessObject(BoObjectTypes.oOrders);

                order.CardCode = cardCode.Trim();
                order.DocDate = (wooOrder.DateCreated ?? DateTime.Now).Date;
                order.DocDueDate = (wooOrder.DateCreated ?? DateTime.Now).Date;
                order.TaxDate = (wooOrder.DateCreated ?? DateTime.Now).Date;
                order.NumAtCard = $"WOO-{wooId}";
                if (!string.IsNullOrWhiteSpace(wooOrder.CustomerNote))
                    order.Comments = wooOrder.CustomerNote;

                TrySetUserField(order, "U_ID_WOO", wooId);

                var lineIndex = 0;
                foreach (var line in wooOrder.LineItems)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var itemCode = (line.Sku ?? string.Empty).Trim();
                    if (string.IsNullOrWhiteSpace(itemCode))
                    {
                        return Fail($"Linea sin SKU en orden Woo {wooId}. No se puede mapear a ItemCode (línea #{lineIndex + 1}).");
                    }

                    if (lineIndex > 0)
                        order.Lines.Add();

                    order.Lines.ItemCode = itemCode;
                    order.Lines.Quantity = line.Quantity <= 0 ? 1 : line.Quantity;

                    // Precio básico: usa line.price si viene. Si no, intenta derivar de total/qty.
                    var price = line.Price;
                    if (price <= 0)
                    {
                        var qty = line.Quantity <= 0 ? 1 : line.Quantity;
                        if (TryParseDecimal(line.Total, out var total) && qty > 0)
                            price = total / qty;
                    }

                    if (price > 0)
                        order.Lines.UnitPrice = (double)price;

                    lineIndex++;
                }

                var rc = order.Add();
                if (rc != 0)
                {
                    company.GetLastError(out var code, out var message);
                    _logger.LogWarning("Error creando OV por DI API. Code={Code}. Message={Message}", code, message);
                    return new SapCreateSalesOrderResult
                    {
                        IsSuccess = false,
                        Message = $"DI API Add(Order) falló. Code={code}. Message={message}"
                    };
                }

                var docEntry = company.GetNewObjectKey();
                string? docNum = null;

                try
                {
                    rs = (Recordset)company.GetBusinessObject(BoObjectTypes.BoRecordset);
                    rs.DoQuery($"SELECT \"DocNum\" FROM \"ORDR\" WHERE \"DocEntry\" = {docEntry}");
                    if (!rs.EoF)
                        docNum = rs.Fields.Item(0).Value?.ToString();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "No se pudo obtener DocNum para DocEntry={DocEntry}", docEntry);
                }

                _logger.LogInformation("OV creada en SAP. WooId={WooId} DocEntry={DocEntry} DocNum={DocNum}", wooId, docEntry, docNum);
                return new SapCreateSalesOrderResult
                {
                    IsSuccess = true,
                    DocEntry = docEntry,
                    DocNum = docNum,
                    Message = "Orden de Venta creada correctamente por DI API."
                };
            }
            finally
            {
                SafeRelease(rs);
                SafeRelease(order);
            }
        }

        private static void TrySetUserField(Documents document, string fieldName, string value)
        {
            try
            {
                document.UserFields.Fields.Item(fieldName).Value = value;
            }
            catch
            {
                // Si el UDF no existe o no es accesible, no bloquea creación básica.
            }
        }

        private static bool TryParseDecimal(string? value, out decimal result)
        {
            return decimal.TryParse(
                (value ?? string.Empty).Trim(),
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out result);
        }

        private static SapCreateSalesOrderResult Fail(string message) =>
            new() { IsSuccess = false, Message = message };

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

