using Azure;
using Azure.Data.Tables;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace retail_app_tester.Models
{
    public class Order : ITableEntity
    {
        public string PartitionKey { get; set; } = "ORDER";
        public string RowKey { get; set; } = Guid.NewGuid().ToString("N");
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
        public string CustomerRowKey { get; set; }

        [Required]
        public DateTime OrderDate { get;  set; } = DateTime.UtcNow;
        public double ShippingFee { get;  set; } = 200;
        public double OrderTotal { get; set; }
        public double SubTotal { get;  set; }
        public double VATAmount { get;  set; }
        public string TrackingNumber { get; set; } = string.Empty;
        public DateTime? EstimatedDeliveryDate { get; set; }
        public string ContractFileName { get; set; }

        public string PaymentMethod { get; set; } = string.Empty;


        [IgnoreDataMember]
        public string DeliveryMessage => EstimatedDeliveryDate.HasValue
    ? $"Estimated delivery: {EstimatedDeliveryDate.Value:MMMM d, yyyy}"
    : "Processing delivery estimate";

        [IgnoreDataMember]
        public string VATDisplay => VATAmount.ToString("C") + " (15%)";

        [IgnoreDataMember] 
        public string CustomerName { get; set; }

        [IgnoreDataMember]
        public string CustomerEmail { get; set; }



    }
}
