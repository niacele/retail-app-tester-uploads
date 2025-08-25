using Azure;
using Azure.Data.Tables;
using Microsoft.WindowsAzure.Storage.Table;
using System.ComponentModel.DataAnnotations;

namespace retail_app_tester.Models
{
    public class Customer : Azure.Data.Tables.ITableEntity
    {
        public string PartitionKey { get; set; } = "CUSTOMER";
        public string RowKey { get; set; } = Guid.NewGuid().ToString("N"); 
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
        [Required(ErrorMessage = "Customer name is required")]
        public string CustomerName { get; set; } = string.Empty;
        [Required, EmailAddress(ErrorMessage = "Please enter a valid email address")] 
        public string CustomerEmail { get; set; } = string.Empty;
        [Required(ErrorMessage = "Please enter a valid phone number")]
        public string PhoneNumber { get; set; } = string.Empty;
        public string ShippingAddress { get; set; } = string.Empty;
        public string CustomerPassword {  get; set; } = string.Empty;
    }
}
