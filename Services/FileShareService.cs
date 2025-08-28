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
                Console.WriteLine($"DEBUG: Starting contract upload for customer: {customerId}, order: {orderId}");

                string dirName = $"{customerId}-orders"; 

                ShareDirectoryClient directory = _shareClient.GetDirectoryClient(dirName);
                await directory.CreateIfNotExistsAsync();
                Console.WriteLine($"DEBUG: Directory created/verified: {dirName}");

                string fileExtension = Path.GetExtension(fileName);
                string uniqueFileName = $"{orderId}-contract{fileExtension}";

                ShareFileClient file = directory.GetFileClient(uniqueFileName);
                await file.CreateAsync(fileStream.Length);
                await file.UploadRangeAsync(
                    new Azure.HttpRange(0, fileStream.Length),
                    fileStream);

                Console.WriteLine($"DEBUG: Successfully uploaded contract to: {dirName}/{uniqueFileName}");
                return uniqueFileName; 
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR uploading contract: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                throw;
            }
        }

        public async Task<Stream> DownloadContractAsync(string customerId, string fileName)
        {
            try
            {
                string dirName = $"{customerId}-orders";
                ShareDirectoryClient directory = _shareClient.GetDirectoryClient(dirName);

                ShareFileClient file = directory.GetFileClient(fileName);
                var response = await file.DownloadAsync();
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
                string dirName = $"{customerId}-orders";
                ShareDirectoryClient directory = _shareClient.GetDirectoryClient(dirName);
                ShareFileClient file = directory.GetFileClient(fileName);

                await file.DeleteAsync();
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
                string dirName = $"{customerId}-orders";
                ShareDirectoryClient directory = _shareClient.GetDirectoryClient(dirName);

                if (await directory.ExistsAsync())
                {
                    await foreach (ShareFileItem item in directory.GetFilesAndDirectoriesAsync())
                    {
                        if (!item.IsDirectory)
                        {
                            contracts.Add(item.Name);
                        }
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
            return $"/Contracts/Download/{customerId}/{fileName}";
        }
    }
}
