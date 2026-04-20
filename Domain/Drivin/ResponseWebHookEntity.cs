using System;
using System.Collections.Generic;

namespace Domain.Drivin
{
    public class ResponseWebHookEntity
    {
        public string planned_date { get; set; }
        public string description { get; set; }
        public string scenario_token { get; set; }
        public string vehicle_code { get; set; }
        public string schema_code { get; set; }
        public string schema_name { get; set; }
        public object fleet_name { get; set; }
        public string organization_name { get; set; }
        public string organization_alt_name { get; set; }
        public object supplier_code { get; set; }
        public object employer_code { get; set; }
        public object employer_name { get; set; }

        public int? trip_number { get; set; }
        public object trip_code { get; set; }

        public int? odometer_start { get; set; }
        public int? odometer_end { get; set; }

        public int? route_id { get; set; }
        public object route_code { get; set; }

        public bool? route_is_approved { get; set; }
        public bool? route_is_started { get; set; }
        public bool? route_is_finished { get; set; }

        public DateTime? route_approved_at { get; set; }
        public DateTime? route_started_at { get; set; }
        public DateTime? route_finished_at { get; set; }

        public string driver_name { get; set; }
        public string driver_email { get; set; }
        public string driver_dni { get; set; }
        public string driver_license_number { get; set; }

        public string assistant_1_name { get; set; }
        public string assistant_1_email { get; set; }
        public string assistant_2_name { get; set; }
        public string assistant_2_email { get; set; }

        public object address_name { get; set; }
        public string address_code { get; set; }
        public string address_address_1 { get; set; }
        public object address_customer_name { get; set; }

        public double? address_lat { get; set; }
        public double? address_lng { get; set; }

        public object address_postal_code { get; set; }
        public string address_country { get; set; }
        public object sales_zone_code { get; set; }

        public int? planned_service_time { get; set; }

        public string eta { get; set; }
        public string eta_approved { get; set; }
        public string eta_started { get; set; }

        public TimeWindow time_window { get; set; }

        public object tracked_arrival { get; set; }
        public object tracked_leave { get; set; }
        public object tracked_service_time { get; set; }

        public DateTime? visit_arrival { get; set; }
        public DateTime? visit_leave { get; set; }

        public List<string> images { get; set; }
        public List<object> documents_with_names { get; set; }

        public object signature { get; set; }
        public string pdf_pod { get; set; }
        public string tracking_url { get; set; }
        public string comment { get; set; }

        public bool? revisit { get; set; }

        public List<CustomField> custom_fields { get; set; }
        public List<object> events { get; set; }
        public List<NotificationEvent> notification_events { get; set; }
        public List<Order> orders { get; set; }
    }

    public class TimeWindow
    {
        public string start { get; set; }
        public string end { get; set; }
    }

    public class CustomField
    {
        public string title { get; set; }
        public object value { get; set; }
        public string data_type { get; set; }
    }

    public class NotificationEvent
    {
        public string name { get; set; }
        public string code { get; set; }
        public double? lat { get; set; }
        public double? lng { get; set; }
        public DateTime? datetime { get; set; }
    }

    public class Order
    {
        public string code { get; set; }
        public string alt_code { get; set; }
        public string delivery_date { get; set; }
        public string description { get; set; }
        public string deploy_date { get; set; }
        public object billing_date { get; set; }
        public object address_type { get; set; }

        public DateTime? pod_arrival { get; set; }
        public double? pod_lat { get; set; }
        public double? pod_lng { get; set; }

        public string status { get; set; }
        public object status_code { get; set; }
        public string customer_status { get; set; }
        public string load_status { get; set; }
        public string reason { get; set; }
        public string reason_code { get; set; }

        public List<object> images { get; set; }
        public object comment { get; set; }
        public object supplier_code { get; set; }
        public object supplier_name { get; set; }
        public object client_code { get; set; }
        public object client_name { get; set; }
        public object order_type { get; set; }
        public object order_type_name { get; set; }

        public string category { get; set; }
        public object contact_name { get; set; }
        public object contact_phone { get; set; }
        public object contact_email { get; set; }

        public decimal? units_1 { get; set; }
        public decimal? units_2 { get; set; }
        public decimal? units_3 { get; set; }

        public object custom_1 { get; set; }
        public object custom_2 { get; set; }
        public object custom_3 { get; set; }
        public object custom_4 { get; set; }
        public object custom_5 { get; set; }
        public object custom_6 { get; set; }
        public object custom_7 { get; set; }
        public object custom_8 { get; set; }
        public object custom_9 { get; set; }
        public object custom_10 { get; set; }
        public object custom_11 { get; set; }
        public object custom_12 { get; set; }
        public object custom_13 { get; set; }
        public object custom_14 { get; set; }
        public object custom_15 { get; set; }
        public object custom_16 { get; set; }
        public object custom_17 { get; set; }
        public object custom_18 { get; set; }
        public object custom_19 { get; set; }
        public object custom_20 { get; set; }

        public object number_1 { get; set; }
        public decimal? number_2 { get; set; }
        public decimal? number_3 { get; set; }
        public object number_4 { get; set; }

        public decimal? num_retries { get; set; }
        public object num_retry_copy { get; set; }

        public List<object> documents_with_names { get; set; }

        public bool? is_otd { get; set; }
        public DateTime? created_at { get; set; }

        public BillingInformation billing_information { get; set; }

        public List<object> items { get; set; }
        public List<object> pickups { get; set; }
    }

    public class BillingInformation
    {
        public object business_name { get; set; }
        public object business_activity { get; set; }
        public object address { get; set; }
        public object city { get; set; }
        public object state { get; set; }
        public object phone { get; set; }
        public object email { get; set; }
        public object tax_id { get; set; }
        public object payment_option { get; set; }
        public object secondary_name { get; set; }
        public object due_date { get; set; }
        public object payment_term { get; set; }
        public object billing_zone { get; set; }
        public object purchase_order { get; set; }
        public object custom_1 { get; set; }
        public object custom_2 { get; set; }

        public string number_1 { get; set; }
        public string number_2 { get; set; }
        public string description { get; set; }
        public string folio { get; set; }
    }
}