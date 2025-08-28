using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

namespace retail_app_tester.Services
{
    public class QueueStorageService
    {
        private readonly QueueClient _ordersQueueClient;
        private readonly QueueClient _customersQueueClient;
        private readonly QueueClient _productsQueueClient;

        private readonly string _connectionString;


        public QueueStorageService(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("AzureStorage");

            _ordersQueueClient = new QueueClient(_connectionString, "orders-queue");
            _customersQueueClient = new QueueClient(_connectionString, "customers-queue");
            _productsQueueClient = new QueueClient(_connectionString, "products-queue");

            _ordersQueueClient.CreateIfNotExistsAsync().Wait();
            _customersQueueClient.CreateIfNotExistsAsync().Wait();
            _productsQueueClient.CreateIfNotExistsAsync().Wait();
        }

        public async Task SendOrderNotificationAsync(string orderId, string message)
        {
            try
            {
                string fullMessage = $"ORDER #{orderId} - {message}";
                await _ordersQueueClient.SendMessageAsync(fullMessage);
            }
            catch (Exception ex)
            {
                
            }
        }

        public async Task SendCustomerNotificationAsync(string customerId, string message)
        {
            try
            {
                string fullMessage = $"CUSTOMER #{customerId} - {message}";
                await _customersQueueClient.SendMessageAsync(fullMessage);
            }
            catch (Exception ex)
            {
               
            }
        }

        public async Task SendProductNotificationAsync(string productId, string message)
        {
            try
            {
                string fullMessage = $"PRODUCT #{productId} - {message}";
                await _productsQueueClient.SendMessageAsync(fullMessage);
            }
            catch (Exception ex)
            {
                
            }
        }

        public async Task SendLowStockAlertAsync(string productId, string productName, int currentStock, int lowStockThreshold)
        {
            try
            {
                string message = $"LOW STOCK ALERT: {productName} (ID: {productId}) - Current: {currentStock}, Threshold: {lowStockThreshold}";
                await _productsQueueClient.SendMessageAsync(message);
            }
            catch (Exception ex)
            {
                
            }
        }

        
        //public async Task<string> PeekNextOrderMessageAsync()
        //{
        //    return await PeekNextMessageAsync(_ordersQueueClient);
        //}

        //public async Task<string> PeekNextCustomerMessageAsync()
        //{
        //    return await PeekNextMessageAsync(_customersQueueClient);
        //}

        //public async Task<string> PeekNextProductMessageAsync()
        //{
        //    return await PeekNextMessageAsync(_productsQueueClient);
        //}

        //private async Task<string> PeekNextMessageAsync(QueueClient queueClient)
        //{
        //    try
        //    {
        //        PeekedMessage[] messages = await queueClient.PeekMessagesAsync(maxMessages: 1);
        //        if (messages.Length > 0)
        //        {
        //            return messages[0].MessageText;
        //        }
        //        return null;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Error peeking queue message: {ex.Message}");
        //        return null;
        //    }
        //}

        //public async Task<int> GetOrderMessageCountAsync()
        //{
        //    return await GetMessageCountAsync(_ordersQueueClient);
        //}

        //public async Task<int> GetCustomerMessageCountAsync()
        //{
        //    return await GetMessageCountAsync(_customersQueueClient);
        //}

        //public async Task<int> GetProductMessageCountAsync()
        //{
        //    return await GetMessageCountAsync(_productsQueueClient);
        //}

        //private async Task<int> GetMessageCountAsync(QueueClient queueClient)
        //{
        //    try
        //    {
        //        QueueProperties properties = await queueClient.GetPropertiesAsync();
        //        return properties.ApproximateMessagesCount;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Error getting queue properties: {ex.Message}");
        //        return -1;
        //    }
        //}
    }
}