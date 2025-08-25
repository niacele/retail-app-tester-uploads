using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;

namespace retail_app_tester.Services
{
    public class FileShareService
    {
        private readonly ShareClient _shareClient;
        private readonly string _connectionString;

        public FileShareService(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("AzureStorage");
            _shareClient = new ShareClient(_connectionString, "credit-contracts");

            // Create the file share if it doesn't exist
            _shareClient.CreateIfNotExistsAsync().Wait();
        }

        public async Task<string> UploadContractAsync(string customerId, string orderId, Stream fileStream, string fileName)
        {
            try
            {
                // Create directory structure: customerId/orders/
                string directoryPath = $"{customerId}/orders";
                var directoryClient = _shareClient.GetDirectoryClient(directoryPath);
                await directoryClient.CreateIfNotExistsAsync();

                // Generate unique filename: orderId_originalFileName
                string fileExtension = Path.GetExtension(fileName);
                string uniqueFileName = $"{orderId}_{DateTime.UtcNow:yyyyMMddHHmmss}{fileExtension}";

                // Get file client and upload
                var fileClient = directoryClient.GetFileClient(uniqueFileName);
                await fileClient.CreateAsync(fileStream.Length);
                await fileClient.UploadRangeAsync(
                    new Azure.HttpRange(0, fileStream.Length),
                    fileStream);

                return uniqueFileName;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error uploading contract: {ex.Message}");
                throw;
            }
        }

        public async Task<Stream> DownloadContractAsync(string customerId, string fileName)
        {
            try
            {
                string directoryPath = $"{customerId}/orders";
                var directoryClient = _shareClient.GetDirectoryClient(directoryPath);

                var fileClient = directoryClient.GetFileClient(fileName);
                var response = await fileClient.DownloadAsync();

                return response.Value.Content;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error downloading contract: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> DeleteContractAsync(string customerId, string fileName)
        {
            try
            {
                string directoryPath = $"{customerId}/orders";
                var directoryClient = _shareClient.GetDirectoryClient(directoryPath);

                var fileClient = directoryClient.GetFileClient(fileName);
                await fileClient.DeleteAsync();

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting contract: {ex.Message}");
                return false;
            }
        }

        public async Task<List<string>> ListCustomerContractsAsync(string customerId)
        {
            var contracts = new List<string>();
            try
            {
                string directoryPath = $"{customerId}/orders";
                var directoryClient = _shareClient.GetDirectoryClient(directoryPath);

                await foreach (ShareFileItem item in directoryClient.GetFilesAndDirectoriesAsync())
                {
                    if (!item.IsDirectory)
                    {
                        contracts.Add(item.Name);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error listing contracts: {ex.Message}");
            }

            return contracts;
        }

        public string GenerateContractDownloadUrl(string customerId, string fileName)
        {
            // This would typically use a SAS token for secure access
            // For simplicity, we'll just return the path info
            return $"/Contracts/Download/{customerId}/{fileName}";
        }
    }
}