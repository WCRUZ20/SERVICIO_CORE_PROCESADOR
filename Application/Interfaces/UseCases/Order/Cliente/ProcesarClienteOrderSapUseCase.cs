using Application.Abstractions;
using Application.Commands.Order;
using Application.Commands.Order.Cliente;
using Application.DTO;
using Application.Interfaces.SAP;
using Domain.Configuration;
using Domain.Helper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Application.Interfaces.UseCases.Order.Cliente
{
    public class ProcesarClienteOrderSapUseCase : IProcesarClienteOrderSapUseCase
    {
        private readonly ICommandHandler<GetClienteOrdersToUpdateWoo, IEnumerable<SapItemQueeDTO>> _getOrdersHandler;
        private readonly ICommandHandler<MarkStatusOrderAsCommand, bool> _markStatusOrdenAsHandler;
        private readonly ISapBusinessPartnerService _sapBusinessPartnerService;
        private readonly ILogger<ProcesarClienteOrderSapUseCase> _logger;
        private readonly WorkerSettings _settings;
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public ProcesarClienteOrderSapUseCase(
            ICommandHandler<GetClienteOrdersToUpdateWoo, IEnumerable<SapItemQueeDTO>> getOrdersHandler,
            ICommandHandler<MarkStatusOrderAsCommand, bool> markStatusOrdenAsHandler,
            ISapBusinessPartnerService sapBusinessPartnerService,
            ILogger<ProcesarClienteOrderSapUseCase> logger,
            IOptions<WorkerSettings> settings)
        {
            _getOrdersHandler = getOrdersHandler;
            _markStatusOrdenAsHandler = markStatusOrdenAsHandler;
            _sapBusinessPartnerService = sapBusinessPartnerService;
            _logger = logger;
            _settings = settings.Value;
        }

        public async Task<ProcesarItemsResult> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                if (_settings.processOrderWooCliente?.IsEnableFlag != 1)
                {
                    return new ProcesarItemsResult { IsSuccess = true, Message = "Proceso CLIENTE creacion de ordenes en sap y actualizacion de estado INTEGRADO en Woo deshabilitado" };
                }

                var ordersList = (await _getOrdersHandler.HandleAsync(new GetClienteOrdersToUpdateWoo())).ToList();
                if (!ordersList.Any())
                {
                    return new ProcesarItemsResult { IsSuccess = true, Message = "No hay ordenes CLIENTE pendientes", ItemsProcessed = 0 };
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

                    var sapOrder = await _sapBusinessPartnerService.GetSalesOrderByWooIdAsync(wooId, cancellationToken);
                    if (sapOrder != null)
                    {
                        order.SapDocEntry = sapOrder.DocEntry;
                        order.SapDocNum = sapOrder.DocNum;
                        if (await MarkOrderAsync(order, StatusHanaDocumentLevel.Confirmed, $"Orden ya existe en SAP con U_WOO_ID={wooId}."))
                        {
                            sent++;
                        }
                        else
                        {
                            failed++;
                        }

                        continue;
                    }

                    var identification = GetBillingIdentification(wooOrder.Billing);
                    if (string.IsNullOrWhiteSpace(identification))
                    {
                        failed++;
                        await MarkOrderAsync(order, StatusHanaDocumentLevel.Error, $"No se encontró identificación en billing para la orden Woo {wooId}.");
                        continue;
                    }

                    var customer = await _sapBusinessPartnerService.GetCustomerByIdentificationAsync(identification, cancellationToken);
                    if (customer == null)
                    {
                        var createRequest = BuildBusinessPartnerRequest(wooOrder, identification);
                        var createResult = await _sapBusinessPartnerService.CreateCustomerAsync(createRequest, cancellationToken);
                        if (createResult.IsSuccess)
                        {
                            _logger.LogInformation($"Socio de negocio {createResult.CardCode} creado correctamente."); 

                            customer.CardCode = createResult.CardCode;
                            customer.LicTradNum = createRequest.Identification;
                            customer.CardName = createRequest.CardName;
                        }
                        else
                        {
                            failed++;
                            await MarkOrderAsync(order, StatusHanaDocumentLevel.Error, createResult.Message ?? $"No se pudo crear socio de negocio {createRequest.CardCode}.");
                            continue;
                        }
                                               
                    }

                    _logger.LogInformation($"Desde aqui se puede crear la orden con el cliente {customer.CardCode}");
                                        
                }

                return new ProcesarItemsResult { IsSuccess = true, ItemsProcessed = ordersList.Count, ItemsSent = sent, ItemsFailed = failed, Message = $"CLIENTE: {sent} procesados, {failed} fallidos" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en proceso CLIENTE HANA -> SAP");
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
    }
}
