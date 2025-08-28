using Azure;
using Azure.Data.Tables;
using Microsoft.Extensions.Configuration;
using retail_app_tester.Models;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace retail_app_tester.Services
{
    public class TableStorageService
    {
        private readonly TableClient _customersTable;
        private readonly TableClient _productsTable;
        private readonly TableClient _ordersTable;
        private readonly TableClient _orderItemsTable;

        public TableStorageService(IConfiguration config)
        {
            var connectionString = config.GetConnectionString("AzureStorage");

            _customersTable = new TableClient(connectionString, "Customers");
            _productsTable = new TableClient(connectionString, "Products");
            _ordersTable = new TableClient(connectionString, "Orders");
            _orderItemsTable = new TableClient(connectionString, "OrderItems");

            
            _customersTable.CreateIfNotExistsAsync().Wait();
            _productsTable.CreateIfNotExistsAsync().Wait();
            _ordersTable.CreateIfNotExistsAsync().Wait();
            _orderItemsTable.CreateIfNotExistsAsync().Wait();
        }

        public async Task<List<Customer>> GetAllCustomersAsync()
        {
            var customers = new List<Customer>();
            await foreach (var customer in _customersTable.QueryAsync<Customer>())
            {
                customers.Add(customer);
            }
            return customers;
        }

        public async Task<Customer> GetCustomerAsync(string partitionKey, string rowKey)
        {
            try
            {
                var response = await _customersTable.GetEntityAsync<Customer>(partitionKey, rowKey);
                return response.Value;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return null;
            }
        }

        public async Task AddCustomerAsync(Customer customer)
        {
            await _customersTable.AddEntityAsync(customer);
        }

        public async Task UpdateCustomerAsync(Customer customer)
        {
            await _customersTable.UpdateEntityAsync(customer, ETag.All, TableUpdateMode.Replace);
        }

        public async Task DeleteCustomerAsync(string partitionKey, string rowKey)
        {
            await _customersTable.DeleteEntityAsync(partitionKey, rowKey);
        }

        public async Task<List<Product>> GetAllProductsAsync()
        {
            var products = new List<Product>();
            await foreach (var product in _productsTable.QueryAsync<Product>())
            {
                products.Add(product);
            }
            return products;
        }

        public async Task<Product> GetProductAsync(string partitionKey, string rowKey)
        {
            try
            {
                var response = await _productsTable.GetEntityAsync<Product>(partitionKey, rowKey);
                return response.Value;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return null;
            }
        }

        public async Task AddProductAsync(Product product)
        {
            await _productsTable.AddEntityAsync(product);
        }

        public async Task UpdateProductAsync(Product product)
        {
            await _productsTable.UpdateEntityAsync(product, ETag.All, TableUpdateMode.Replace);
        }

        public async Task UpdateProductStockAsync(string productRowKey, int quantityToDeduct)
        {
            var product = await GetProductAsync("PRODUCT", productRowKey);
            if (product != null)
            {
                product.StockQuantity -= quantityToDeduct;
                await UpdateProductAsync(product);
            }
        }

        public async Task<bool> IsProductInStockAsync(string productRowKey, int requestedQuantity)
        {
            var product = await GetProductAsync("PRODUCT", productRowKey);
            return product != null && product.StockQuantity >= requestedQuantity;
        }



public async Task DeleteProductAsync(string partitionKey, string rowKey)
        {
            await _productsTable.DeleteEntityAsync(partitionKey, rowKey);
        }

        public async Task<List<Order>> GetAllOrdersAsync()
        {
            var orders = new List<Order>();
            await foreach (var order in _ordersTable.QueryAsync<Order>())
            {
                orders.Add(order);
            }
            return orders;
        }

        public async Task<Order> GetOrderAsync(string partitionKey, string rowKey)
        {
            try
            {
                var response = await _ordersTable.GetEntityAsync<Order>(partitionKey, rowKey);
                return response.Value;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return null;
            }
        }

        public async Task AddOrderAsync(Order order)
        {
            await _ordersTable.AddEntityAsync(order);
        }

        public async Task UpdateOrderAsync(Order order)
        {
            await _ordersTable.UpdateEntityAsync(order, ETag.All, TableUpdateMode.Replace);
        }

        public async Task DeleteOrderAsync(string partitionKey, string rowKey)
        {
            await _ordersTable.DeleteEntityAsync(partitionKey, rowKey);
        }

        public async Task<List<Order>> GetOrdersByCustomerAsync(string customerRowKey)
        {
            var orders = new List<Order>();
            var filter = $"CustomerRowKey eq '{customerRowKey}'";
            await foreach (var order in _ordersTable.QueryAsync<Order>(filter))
            {
                orders.Add(order);
            }
            return orders;
        }

        public async Task<List<Product>> GetProductsByCategoryAsync(string category)
        {
            var products = new List<Product>();
            var filter = $"ProductCategory eq '{category}'";
            await foreach (var product in _productsTable.QueryAsync<Product>(filter))
            {
                products.Add(product);
            }
            return products;
        }

        public async Task<int> GetCustomerCountAsync()
        {
            var customers = await GetAllCustomersAsync();
            return customers.Count;
        }

        public async Task<int> GetProductCountAsync()
        {
            var products = await GetAllProductsAsync();
            return products.Count;
        }

        public async Task<int> GetOrderCountAsync()
        {
            var orders = await GetAllOrdersAsync();
            return orders.Count;
        }

        public async Task<List<OrderItem>> GetOrderItemsAsync(string orderId)
        {
            var items = new List<OrderItem>();
            var filter = $"PartitionKey eq '{orderId}'";
            await foreach (var item in _orderItemsTable.QueryAsync<OrderItem>(filter))
            {
                items.Add(item);
            }
            return items;
        }

        public async Task<OrderItem> GetOrderItemAsync(string orderId, string itemId)
        {
            try
            {
                var response = await _orderItemsTable.GetEntityAsync<OrderItem>(orderId, itemId);
                return response.Value;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return null;
            }
        }

        public async Task AddOrderItemAsync(OrderItem orderItem)
        {
            await _orderItemsTable.AddEntityAsync(orderItem);
        }

        public async Task UpdateOrderItemAsync(OrderItem orderItem)
        {
            await _orderItemsTable.UpdateEntityAsync(orderItem, ETag.All, TableUpdateMode.Replace);
        }

        public async Task DeleteOrderItemAsync(string orderId, string itemId)
        {
            await _orderItemsTable.DeleteEntityAsync(orderId, itemId);
        }

        public async Task DeleteAllOrderItemsAsync(string orderId)
        {
            var items = await GetOrderItemsAsync(orderId);
            foreach (var item in items)
            {
                await _orderItemsTable.DeleteEntityAsync(orderId, item.RowKey);
            }
        }


    }
}