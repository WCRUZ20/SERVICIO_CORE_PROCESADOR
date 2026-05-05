using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Application.DTO
{
    public class WooOrderDTO
    {
        #region "Estructura de respuesta prueba"
        //[JsonPropertyName("id")]
        //public int Id { get; set; }

        //[JsonPropertyName("Number")]
        //public string Number { get; set; } = string.Empty;

        //[JsonPropertyName("Status")]
        //public string Status { get; set; } = string.Empty;

        //[JsonPropertyName("Total")]
        //public decimal Total { get; set; }

        //[JsonPropertyName("Currency")]
        //public string Currency { get; set; } = string.Empty;

        //[JsonPropertyName("DateCreated")]
        //public DateTime DateCreated { get; set; }

        //[JsonPropertyName("CustomerId")]
        //public int CustomerId { get; set; }

        //[JsonPropertyName("CustomerName")]
        //public string CustomerName { get; set; } = string.Empty;

        //[JsonPropertyName("Email")]
        //public string Email { get; set; } = string.Empty;

        //[JsonPropertyName("PaymentMethod")]
        //public string PaymentMethod { get; set; } = string.Empty;
        #endregion
        #region "Estructura de respuesta real"
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("parent_id")]
        public int ParentId { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; }

        [JsonPropertyName("version")]
        public string Version { get; set; }

        [JsonPropertyName("prices_include_tax")]
        public bool PricesIncludeTax { get; set; }

        [JsonPropertyName("date_created")]
        public DateTime? DateCreated { get; set; }

        [JsonPropertyName("date_modified")]
        public DateTime? DateModified { get; set; }

        [JsonPropertyName("discount_total")]
        public string DiscountTotal { get; set; }

        [JsonPropertyName("discount_tax")]
        public string DiscountTax { get; set; }

        [JsonPropertyName("shipping_total")]
        public string ShippingTotal { get; set; }

        [JsonPropertyName("shipping_tax")]
        public string ShippingTax { get; set; }

        [JsonPropertyName("cart_tax")]
        public string CartTax { get; set; }

        [JsonPropertyName("total")]
        public string Total { get; set; }

        [JsonPropertyName("total_tax")]
        public string TotalTax { get; set; }

        [JsonPropertyName("customer_id")]
        public int CustomerId { get; set; }

        [JsonPropertyName("order_key")]
        public string OrderKey { get; set; }

        [JsonPropertyName("billing")]
        public WooBilling Billing { get; set; }

        [JsonPropertyName("shipping")]
        public WooShipping Shipping { get; set; }

        [JsonPropertyName("payment_method")]
        public string PaymentMethod { get; set; }

        [JsonPropertyName("payment_method_title")]
        public string PaymentMethodTitle { get; set; }

        [JsonPropertyName("transaction_id")]
        public string TransactionId { get; set; }

        [JsonPropertyName("customer_ip_address")]
        public string CustomerIpAddress { get; set; }

        [JsonPropertyName("customer_user_agent")]
        public string CustomerUserAgent { get; set; }

        [JsonPropertyName("created_via")]
        public string CreatedVia { get; set; }

        [JsonPropertyName("customer_note")]
        public string CustomerNote { get; set; }

        [JsonPropertyName("date_completed")]
        public DateTime? DateCompleted { get; set; }

        [JsonPropertyName("date_paid")]
        public DateTime? DatePaid { get; set; }

        [JsonPropertyName("cart_hash")]
        public string CartHash { get; set; }

        [JsonPropertyName("number")]
        public string Number { get; set; }

        [JsonPropertyName("meta_data")]
        public List<WooMetaData> MetaData { get; set; }

        [JsonPropertyName("line_items")]
        public List<WooLineItem> LineItems { get; set; }

        [JsonPropertyName("tax_lines")]
        public List<WooTaxLine> TaxLines { get; set; }

        [JsonPropertyName("shipping_lines")]
        public List<WooShippingLine> ShippingLines { get; set; }

        [JsonPropertyName("fee_lines")]
        public List<object> FeeLines { get; set; }

        [JsonPropertyName("coupon_lines")]
        public List<WooCouponLine> CouponLines { get; set; }

        [JsonPropertyName("refunds")]
        public List<object> Refunds { get; set; }

        [JsonPropertyName("payment_url")]
        public string PaymentUrl { get; set; }

        [JsonPropertyName("is_editable")]
        public bool IsEditable { get; set; }

        [JsonPropertyName("needs_payment")]
        public bool NeedsPayment { get; set; }

        [JsonPropertyName("needs_processing")]
        public bool NeedsProcessing { get; set; }

        [JsonPropertyName("currency_symbol")]
        public string CurrencySymbol { get; set; }
        #endregion
    }

    public class WooBilling
    {
        [JsonPropertyName("first_name")]
        public string FirstName { get; set; }

        [JsonPropertyName("last_name")]
        public string LastName { get; set; }

        [JsonPropertyName("company")]
        public string Company { get; set; }

        [JsonPropertyName("address_1")]
        public string Address1 { get; set; }

        [JsonPropertyName("address_2")]
        public string Address2 { get; set; }

        [JsonPropertyName("city")]
        public string City { get; set; }

        [JsonPropertyName("state")]
        public string State { get; set; }

        [JsonPropertyName("postcode")]
        public string Postcode { get; set; }

        [JsonPropertyName("country")]
        public string Country { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("phone")]
        public string Phone { get; set; }

        [JsonPropertyName("tipo_doc")]
        public string TipoDoc { get; set; }

        [JsonPropertyName("cedu")]
        public string Cedu { get; set; }

        [JsonPropertyName("pasaporte")]
        public string Pasaporte { get; set; }

        [JsonPropertyName("ruc")]
        public string Ruc { get; set; }
    }

    public class WooShipping
    {
        [JsonPropertyName("first_name")]
        public string FirstName { get; set; }

        [JsonPropertyName("last_name")]
        public string LastName { get; set; }

        [JsonPropertyName("company")]
        public string Company { get; set; }

        [JsonPropertyName("address_1")]
        public string Address1 { get; set; }

        [JsonPropertyName("address_2")]
        public string Address2 { get; set; }

        [JsonPropertyName("city")]
        public string City { get; set; }

        [JsonPropertyName("state")]
        public string State { get; set; }

        [JsonPropertyName("postcode")]
        public string Postcode { get; set; }

        [JsonPropertyName("country")]
        public string Country { get; set; }

        [JsonPropertyName("phone")]
        public string Phone { get; set; }
    }

    public class WooMetaData
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("key")]
        public string Key { get; set; }

        [JsonPropertyName("value")]
        public object Value { get; set; }

        [JsonPropertyName("display_key")]
        public string DisplayKey { get; set; }

        [JsonPropertyName("display_value")]
        public object DisplayValue { get; set; }
    }

    public class WooLineItem
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("product_id")]
        public int ProductId { get; set; }

        [JsonPropertyName("variation_id")]
        public int VariationId { get; set; }

        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }

        [JsonPropertyName("tax_class")]
        public string TaxClass { get; set; }

        [JsonPropertyName("subtotal")]
        public string Subtotal { get; set; }

        [JsonPropertyName("subtotal_tax")]
        public string SubtotalTax { get; set; }

        [JsonPropertyName("total")]
        public string Total { get; set; }

        [JsonPropertyName("total_tax")]
        public string TotalTax { get; set; }

        [JsonPropertyName("taxes")]
        public List<WooTax> Taxes { get; set; }

        [JsonPropertyName("meta_data")]
        public List<WooMetaData> MetaData { get; set; }

        [JsonPropertyName("sku")]
        public string Sku { get; set; }

        [JsonPropertyName("price")]
        public decimal Price { get; set; }

        [JsonPropertyName("image")]
        public WooImage Image { get; set; }

        [JsonPropertyName("parent_name")]
        public string ParentName { get; set; }
    }

    public class WooTax
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("total")]
        public string Total { get; set; }

        [JsonPropertyName("subtotal")]
        public string Subtotal { get; set; }
    }

    public class WooImage
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("src")]
        public string Src { get; set; }
    }

    public class WooTaxLine
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("rate_code")]
        public string RateCode { get; set; }

        [JsonPropertyName("rate_id")]
        public int RateId { get; set; }

        [JsonPropertyName("label")]
        public string Label { get; set; }

        [JsonPropertyName("compound")]
        public bool Compound { get; set; }

        [JsonPropertyName("tax_total")]
        public string TaxTotal { get; set; }

        [JsonPropertyName("shipping_tax_total")]
        public string ShippingTaxTotal { get; set; }

        [JsonPropertyName("rate_percent")]
        public decimal RatePercent { get; set; }

        [JsonPropertyName("meta_data")]
        public List<WooMetaData> MetaData { get; set; }
    }

    public class WooShippingLine
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("method_title")]
        public string MethodTitle { get; set; }

        [JsonPropertyName("method_id")]
        public string MethodId { get; set; }

        [JsonPropertyName("instance_id")]
        public string InstanceId { get; set; }

        [JsonPropertyName("total")]
        public string Total { get; set; }

        [JsonPropertyName("total_tax")]
        public string TotalTax { get; set; }

        [JsonPropertyName("taxes")]
        public List<WooTax> Taxes { get; set; }

        [JsonPropertyName("meta_data")]
        public List<WooMetaData> MetaData { get; set; }
    }

    public class WooCouponLine
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("code")]
        public string Code { get; set; }

        [JsonPropertyName("discount")]
        public string Discount { get; set; }

        [JsonPropertyName("discount_tax")]
        public string DiscountTax { get; set; }

        [JsonPropertyName("meta_data")]
        public List<WooMetaData> MetaData { get; set; }

        [JsonPropertyName("discount_type")]
        public string DiscountType { get; set; }

        [JsonPropertyName("nominal_amount")]
        public decimal NominalAmount { get; set; }

        [JsonPropertyName("free_shipping")]
        public bool FreeShipping { get; set; }
    }
}
