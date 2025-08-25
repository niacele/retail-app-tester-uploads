using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

namespace retail_app_tester.Services
{
    public class QueueStorageService
    {
        private readonly QueueClient _queueClient;

        public QueueStorageService(IConfiguration config)
        {
            var connectionString = config.GetConnectionString("AzureStorage");
            _queueClient = new QueueClient(connectionString, "orders-queue");

            // Create the queue if it doesn't exist
            _queueClient.CreateIfNotExistsAsync().Wait();
        }

        public async Task SendOrderNotificationAsync(string orderId, string message)
        {
            try
            {
                string fullMessage = $"Order #{orderId} - {message}";
                await _queueClient.SendMessageAsync(fullMessage);
            }
            catch (Exception ex)
            {
                // Log the error but don't throw - we don't want queue failures to break the order process
                Console.WriteLine($"Error sending queue message: {ex.Message}");
            }
        }

        public async Task<string> PeekNextMessageAsync()
        {
            try
            {
                PeekedMessage[] messages = await _queueClient.PeekMessagesAsync(maxMessages: 1);
                if (messages.Length > 0)
                {
                    return messages[0].MessageText;
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error peeking queue message: {ex.Message}");
                return null;
            }
        }

        public async Task<int> GetMessageCountAsync()
        {
            try
            {
                QueueProperties properties = await _queueClient.GetPropertiesAsync();
                return properties.ApproximateMessagesCount;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting queue properties: {ex.Message}");
                return -1;
            }
        }

        public async Task<bool> DeleteMessageAsync(string messageId, string popReceipt)
        {
            try
            {
                await _queueClient.DeleteMessageAsync(messageId, popReceipt);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting queue message: {ex.Message}");
                return false;
            }
        }
    }
}