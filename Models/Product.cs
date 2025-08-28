using Azure;
using Azure.Data.Tables;
using Microsoft.WindowsAzure.Storage.Table;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace retail_app_tester.Models
{
    public class Product : Azure.Data.Tables.ITableEntity
    {
        public string PartitionKey { get; set; } = "PRODUCT";
        public string RowKey { get; set; } = Guid.NewGuid().ToString("N"); 
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
        
        [Required(ErrorMessage = "Product name is required")]
        public string ProductName { get; set; } = string.Empty;
        [Required(ErrorMessage = "Brand is required")]
        [StringLength(50)]
        public string Brand { get; set; } = string.Empty;
        [StringLength(500)]
        public string ProductDescription { get; set; } = string.Empty;
        [Required(ErrorMessage = "Category is required")]
        [StringLength(50)]
        public string ProductCategory { get; set; } = string.Empty;
        public int PriceRand { get; set; }
        public int PriceCents { get; set; }

        [Required(ErrorMessage = "Size is required")]
        [StringLength(20)]
        public string Size { get; set; } = string.Empty;
        public string KeyIngredients { get; set; } = string.Empty;

        [Required(ErrorMessage = "Stock quantity is required")]
        [Range(0, int.MaxValue)]
        public int StockQuantity { get; set; }

        public int LowStockThreshold { get; set; } = 10; 

        public string ImageURL { get; set; } = string.Empty;

        [IgnoreDataMember]
        public decimal ProductPrice => PriceRand + (PriceCents / 100m);

    }
}
