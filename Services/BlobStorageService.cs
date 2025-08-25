using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;

namespace retail_app_tester.Services
{
    public class BlobStorageService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly string _connectionString;

        public BlobStorageService(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("AzureStorage");
            _blobServiceClient = new BlobServiceClient(_connectionString);
        }

        public async Task<string> UploadProductImageAsync(Stream fileStream, string fileName, string productId)
        {
            try
            {
                // Create container if it doesn't exist
                var containerClient = _blobServiceClient.GetBlobContainerClient("product-images");
                await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

                // Generate unique filename to avoid conflicts
                string fileExtension = Path.GetExtension(fileName);
                string uniqueFileName = $"{productId}_{Guid.NewGuid().ToString("N").Substring(0, 8)}{fileExtension}";

                // Get blob client and upload
                var blobClient = containerClient.GetBlobClient(uniqueFileName);
                await blobClient.UploadAsync(fileStream, overwrite: true);

                // Return the URL of the uploaded image
                return blobClient.Uri.ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error uploading product image: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> DeleteProductImageAsync(string imageUrl)
        {
            try
            {
                if (string.IsNullOrEmpty(imageUrl))
                    return false;

                var uri = new Uri(imageUrl);
                var containerClient = _blobServiceClient.GetBlobContainerClient("product-images");
                var blobName = uri.Segments[^1]; // Get the last segment (filename)

                var blobClient = containerClient.GetBlobClient(blobName);
                return await blobClient.DeleteIfExistsAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting product image: {ex.Message}");
                return false;
            }
        }

        public async Task<Stream> DownloadProductImageAsync(string imageUrl)
        {
            try
            {
                var uri = new Uri(imageUrl);
                var containerClient = _blobServiceClient.GetBlobContainerClient("product-images");
                var blobName = uri.Segments[^1];

                var blobClient = containerClient.GetBlobClient(blobName);
                var response = await blobClient.DownloadAsync();
                return response.Value.Content;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error downloading product image: {ex.Message}");
                throw;
            }
        }
    }
}

