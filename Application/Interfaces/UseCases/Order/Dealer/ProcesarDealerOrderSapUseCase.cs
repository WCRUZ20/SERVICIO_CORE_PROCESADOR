using Application.Abstractions;
using Application.Commands.Order;
using Application.Commands.Order.Cliente;
using Application.Commands.Order.Dealer;
using Application.DTO;
using Application.Interfaces.SAP;
using Application.Interfaces.UseCases.Order.Cliente;
using Domain.Configuration;
using Domain.Helper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SAPbobsCOM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Application.Interfaces.UseCases.Order.Dealer
{
    public class ProcesarDealerOrderSapUseCase : IProcesarDealerOrderSapUseCase
    {
        private readonly ICommandHandler<GetDealerOrdersToUpdateWoo, IEnumerable<SapItemQueeDTO>> _getOrdersHandler;
        //private readonly ICommandHandler<SendItemsToApiCommand, (bool IsSuccess, string? Message)> _sendToApiHandler;
        private readonly ICommandHandler<MarkStatusOrderAsCommand, bool> _markStatusOrdenAsHandler;
        private readonly ISapDiApiUnitOfWork _sapUow;
        private readonly ISapBusinessPartnerDiApiService _sapBusinessPartnerDiApiService;
        private readonly ISapSalesOrderService _sapSalesOrderService;
        private readonly ILogger<ProcesarDealerOrderSapUseCase> _logger;
        private readonly WorkerSettings _settings;
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public ProcesarDealerOrderSapUseCase(
        ICommandHandler<GetDealerOrdersToUpdateWoo, IEnumerable<SapItemQueeDTO>> getOrdersHandler,
        //ICommandHandler<SendItemsToApiCommand, (bool IsSuccess, string? Message)> sendToApiHandler,
        ICommandHandler<MarkStatusOrderAsCommand, bool> markStatusOrdenAsHandler,
        ISapDiApiUnitOfWork sapUow,
        ISapBusinessPartnerDiApiService sapBusinessPartnerDiApiService,
        ISapSalesOrderService sapSalesOrderService,
        ILogger<ProcesarDealerOrderSapUseCase> logger,
        IOptions<WorkerSettings> settings)
        {
            _getOrdersHandler = getOrdersHandler;
            //_sendToApiHandler = sendToApiHandler;
            _markStatusOrdenAsHandler = markStatusOrdenAsHandler;
            _sapUow = sapUow;
            _sapBusinessPartnerDiApiService = sapBusinessPartnerDiApiService;
            _sapSalesOrderService = sapSalesOrderService;
            _logger = logger;
            _settings = settings.Value;
        }

        public async Task<ProcesarItemsResult> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                if (_settings.processOrderWooDealer?.IsEnableFlag != 1)
                {
                    return new ProcesarItemsResult { IsSuccess = true, Message = "Proceso DEALER creacion de ordenes en sap y actualizacion de estado INTEGRADO en Woo deshabilitado" };
                }

                var ordersList = (await _getOrdersHandler.HandleAsync(new GetDealerOrdersToUpdateWoo())).ToList();
                if (!ordersList.Any())
                {
                    return new ProcesarItemsResult { IsSuccess = true, Message = "No hay ordenes DEALER pendientes", ItemsProcessed = 0 };
                }

                var sent = 0;
                var failed = 0;

                foreach (var order in ordersList)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var wooOrder = DeserializeOrder(order);
                    if (wooOrder == null)
                    {
                        failed++;
                        await MarkOrderAsync(order, StatusHanaDocumentLevel.Error, "No se pudo deserializar el JSON de la orden Woo.");
                        continue;
                    }

                    var wooId = GetWooId(order, wooOrder);
                    if (string.IsNullOrWhiteSpace(wooId))
                    {
                        failed++;
                        await MarkOrderAsync(order, StatusHanaDocumentLevel.Error, "No se encontró idWoo en la cola ni id en el JSON de Woo.");
                        continue;
                    }

                    order.idWoo = wooId;

                    var identification = GetBillingIdentification(wooOrder.Billing);
                    if (string.IsNullOrWhiteSpace(identification))
                    {
                        failed++;
                        await MarkOrderAsync(order, StatusHanaDocumentLevel.Error, $"No se encontró identificación en billing para la orden Woo {wooId}.");
                        continue;
                    }

                    try
                    {
                        await _sapUow.BeginAsync(cancellationToken);

                        var existingOrder = GetSalesOrderByWooId(_sapUow.Company, wooId);
                        if (existingOrder != null)
                        {
                            order.SapDocEntry = existingOrder.DocEntry;
                            order.SapDocNum = existingOrder.DocNum;

                            await _sapUow.CommitAsync(cancellationToken);

                            if (await MarkOrderAsync(order, StatusHanaDocumentLevel.Confirmed, $"Orden ya existe en SAP con U_ID_WOO={wooId}."))
                                sent++;
                            else
                                failed++;

                            continue;
                        }

                        var customer = await _sapBusinessPartnerDiApiService.GetCustomerByIdentificationAsync(
                            _sapUow.Company,
                            identification,
                            cancellationToken);

                        if (customer == null)
                        {
                            var createRequest = BuildBusinessPartnerRequest(wooOrder, identification);
                            var createResult = await _sapBusinessPartnerDiApiService.CreateCustomerAsync(
                                _sapUow.Company,
                                createRequest,
                                cancellationToken);

                            if (!createResult.IsSuccess)
                            {
                                await _sapUow.RollbackAsync(cancellationToken);
                                failed++;
                                await MarkOrderAsync(order, StatusHanaDocumentLevel.Error, createResult.Message ?? $"No se pudo crear socio de negocio {createRequest.CardCode}.");
                                continue;
                            }

                            customer = new SapBusinessPartnerLookupDTO
                            {
                                CardCode = createResult.CardCode,
                                CardName = createRequest.CardName,
                                LicTradNum = createRequest.Identification
                            };
                        }

                        var createOrderResult = await _sapSalesOrderService.CreateSalesOrderFromWooAsync(
                            _sapUow.Company,
                            wooId,
                            wooOrder,
                            customer.CardCode,
                            cancellationToken);

                        if (!createOrderResult.IsSuccess)
                        {
                            await _sapUow.RollbackAsync(cancellationToken);
                            failed++;
                            await MarkOrderAsync(order, StatusHanaDocumentLevel.Error, createOrderResult.Message ?? "Error creando orden en SAP por DI API.");
                            continue;
                        }

                        await _sapUow.CommitAsync(cancellationToken);

                        order.SapDocEntry = createOrderResult.DocEntry ?? string.Empty;
                        order.SapDocNum = createOrderResult.DocNum ?? string.Empty;
                        if (await MarkOrderAsync(order, StatusHanaDocumentLevel.Confirmed, $"Orden creada en SAP (DocEntry={order.SapDocEntry}, DocNum={order.SapDocNum})."))
                            sent++;
                        else
                            failed++;
                    }
                    catch (Exception ex)
                    {
                        try { await _sapUow.RollbackAsync(cancellationToken); } catch { }
                        failed++;
                        await MarkOrderAsync(order, StatusHanaDocumentLevel.Error, $"Excepción DI API: {ex.Message}");
                    }

                    //var sendResult = await _sendToApiHandler.HandleAsync(
                    //    new SendItemsToApiCommand(item, ItemDestinationType.Cliente));
                    //item.Status = sendResult.IsSuccess ? (int)StatusHanaDocumentLevel.Confirmed : (int)StatusHanaDocumentLevel.Error;
                    //item.Json = sendResult.Message ?? string.Empty;
                    //if (sendResult.IsSuccess && !string.IsNullOrWhiteSpace(sendResult.Message))
                    //{
                    //    var response = JsonSerializer.Deserialize<WooProductResponse>(
                    //        sendResult.Message,
                    //        new JsonSerializerOptions
                    //        {
                    //            PropertyNameCaseInsensitive = true
                    //        });

                    //    item.idWoo = response?.Id.ToString();
                    //}

                    //if (sendResult.IsSuccess) sent++; else failed++;
                    //await _markStatusItemAsHandler.HandleAsync(new MarkStatusItemAsCommand(item));
                }

                return new ProcesarItemsResult { IsSuccess = true, ItemsProcessed = ordersList.Count, ItemsSent = sent, ItemsFailed = failed, Message = $"DEALER: {sent} enviados, {failed} fallidos" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en proceso DEALER HANA -> API");
                return new ProcesarItemsResult { IsSuccess = false, ErrorMessage = ex.Message };
            }
        }

        private WooOrderDTO? DeserializeOrder(SapItemQueeDTO order)
        {
            if (string.IsNullOrWhiteSpace(order.Json))
            {
                return null;
            }

            return JsonSerializer.Deserialize<WooOrderDTO>(order.Json, _jsonOptions);
        }

        private static string GetWooId(SapItemQueeDTO queueOrder, WooOrderDTO wooOrder)
        {
            if (!string.IsNullOrWhiteSpace(queueOrder.idWoo))
            {
                return queueOrder.idWoo.Trim();
            }

            return wooOrder.Id > 0 ? wooOrder.Id.ToString() : string.Empty;
        }

        private static string GetBillingIdentification(WooBilling? billing)
        {
            if (billing == null)
            {
                return string.Empty;
            }

            var tipoDoc = billing.TipoDoc?.Trim().ToLowerInvariant();
            return tipoDoc switch
            {
                "cedula" or "cédula" => billing.Cedu?.Trim() ?? string.Empty,
                "pasaporte" => billing.Pasaporte?.Trim() ?? string.Empty,
                "ruc" => billing.Ruc?.Trim() ?? string.Empty,
                _ => FirstNotEmpty(billing.Cedu, billing.Pasaporte, billing.Ruc)
            };
        }

        private static SapBusinessPartnerCreateRequest BuildBusinessPartnerRequest(WooOrderDTO wooOrder, string identification)
        {
            var billing = wooOrder.Billing;
            var shipping = wooOrder.Shipping;
            var firstName = billing?.FirstName?.Trim() ?? string.Empty;
            var lastName = billing?.LastName?.Trim() ?? string.Empty;
            var cardName = FirstNotEmpty($"{firstName} {lastName}".Trim(), billing?.Company, identification);

            return new SapBusinessPartnerCreateRequest
            {
                CardCode = $"C{identification}",
                CardName = cardName,
                Identification = identification,
                Phone = FirstNotEmpty(billing?.Phone, shipping?.Phone),
                Email = billing?.Email?.Trim() ?? string.Empty,
                Addresses = new List<SapBusinessPartnerAddressDTO>
                {
                    BuildAddress("Facturacion", "bo_BillTo", billing?.Address1, billing?.Address2, billing?.City, billing?.State, billing?.Postcode, billing?.Country),
                    BuildAddress("Envio", "bo_ShipTo", shipping?.Address1, shipping?.Address2, shipping?.City, shipping?.State, shipping?.Postcode, shipping?.Country)
                }
            };
        }

        private static SapBusinessPartnerAddressDTO BuildAddress(string name, string type, string? address1, string? address2, string? city, string? state, string? postcode, string? country)
        {
            return new SapBusinessPartnerAddressDTO
            {
                AddressName = name,
                AddressType = type,
                Street = address1?.Trim() ?? string.Empty,
                Block = address2?.Trim() ?? string.Empty,
                City = city?.Trim() ?? string.Empty,
                State = state?.Trim() ?? string.Empty,
                ZipCode = postcode?.Trim() ?? string.Empty,
                Country = country?.Trim() ?? string.Empty
            };
        }

        private async Task<bool> MarkOrderAsync(SapItemQueeDTO order, StatusHanaDocumentLevel status, string comment)
        {
            order.Status = (int)status;
            order.Comment = comment;
            return await _markStatusOrdenAsHandler.HandleAsync(new MarkStatusOrderAsCommand(order));
        }

        private static string FirstNotEmpty(params string?[] values)
        {
            return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim() ?? string.Empty;
        }

        private static SapSalesOrderLookupDTO? GetSalesOrderByWooId(Company company, string wooId)
        {
            Recordset? rs = null;
            try
            {
                rs = (Recordset)company.GetBusinessObject(BoObjectTypes.BoRecordset);
                var id = (wooId ?? string.Empty).Trim().Replace("'", "''");
                rs.DoQuery(
                    $"SELECT TOP 1 \"DocEntry\", \"DocNum\" FROM \"ORDR\" WHERE \"U_ID_WOO\" = '{id}' ORDER BY \"DocEntry\" DESC");

                if (rs.EoF) return null;

                return new SapSalesOrderLookupDTO
                {
                    DocEntry = rs.Fields.Item("DocEntry").Value?.ToString() ?? string.Empty,
                    DocNum = rs.Fields.Item("DocNum").Value?.ToString() ?? string.Empty
                };
            }
            finally
            {
                if (rs != null && Marshal.IsComObject(rs))
                {
                    try { Marshal.FinalReleaseComObject(rs); } catch { }
                }
            }
        }
    }
}
