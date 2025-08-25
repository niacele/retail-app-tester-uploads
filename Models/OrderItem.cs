using Azure;
using Azure.Data.Tables;
using Microsoft.WindowsAzure.Storage.Table;
using System.ComponentModel.DataAnnotations;

namespace retail_app_tester.Models
{
    public class OrderItem : Azure.Data.Tables.ITableEntity
    {
        public string PartitionKey { get; set; } 
        public string RowKey { get; set; } = Guid.NewGuid().ToString("N"); // Unique line item ID
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
        public string ProductRowKey { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public double UnitPrice { get; set; } 



    }
}
