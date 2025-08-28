using Azure;
using Azure.Data.Tables;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using retail_app_tester.Models;
using retail_app_tester.Services;

namespace retail_app_tester.Controllers
{
    public class ProductsController : Controller
    {
        private readonly TableStorageService _tableStorageService;
        private readonly QueueStorageService _queueStorageService;
        private readonly BlobStorageService _blobStorageService;

        public ProductsController(TableStorageService tableStorageService, QueueStorageService queueStorageService, BlobStorageService blobStorageService)
        {
            _tableStorageService = tableStorageService;
            _queueStorageService = queueStorageService;
            _blobStorageService = blobStorageService;
        }

        // GET: Products
        public async Task<IActionResult> Index()
        {
            var products = await _tableStorageService.GetAllProductsAsync();
            return View(products);
        }

        // GET: Products/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _tableStorageService.GetProductAsync("PRODUCT", id);

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // GET: Products/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Products/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("RowKey,ProductName,Brand,ProductDescription,ProductCategory,PriceRand,PriceCents,Size,KeyIngredients,StockQuantity,LowStockThreshold")] Product product, IFormFile productImage)
        {
            if (ModelState.IsValid)
            {
                product.PartitionKey = "PRODUCT";
                product.RowKey = Guid.NewGuid().ToString("N");

                if (productImage != null && productImage.Length > 0)
                {
                    try
                    {
                        using var stream = productImage.OpenReadStream();
                        product.ImageURL = await _blobStorageService.UploadProductImageAsync(
                            stream, productImage.FileName, product.RowKey);
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError("", $"Error uploading image: {ex.Message}");
                        return View(product);
                    }
                }
                else
                {
                    
                }

                await _tableStorageService.AddProductAsync(product);

                if (_queueStorageService != null)
                {
                    double price = (double)(product.PriceRand + (product.PriceCents / 100m));
                    await _queueStorageService.SendProductNotificationAsync(product.RowKey,
                        $"New product added: {product.ProductName} - {price:C} | Category: {product.ProductCategory} | Stock: {product.StockQuantity}");

                    if (product.StockQuantity <= product.LowStockThreshold)
                    {
                        await _queueStorageService.SendLowStockAlertAsync(
                            product.RowKey,
                            product.ProductName,
                            product.StockQuantity,
                            product.LowStockThreshold);
                    }
                }

                return RedirectToAction(nameof(Index));
            }
            return View(product);

        }

        // GET: Products/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _tableStorageService.GetProductAsync("PRODUCT", id);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        // POST: Products/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("RowKey,ProductName,Brand,ProductDescription,ProductCategory,PriceRand,PriceCents,Size,KeyIngredients,StockQuantity,LowStockThreshold")] Product product, IFormFile productImage)
        {
            if (id != product.RowKey)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingProduct = await _tableStorageService.GetProductAsync("PRODUCT", id);
                    if (existingProduct == null)
                    {
                        return NotFound();
                    }

                    if (productImage != null && productImage.Length > 0)
                    {
                        try
                        {
                            if (!string.IsNullOrEmpty(existingProduct.ImageURL) &&
                                !existingProduct.ImageURL.Contains("default-product"))
                            {
                                await _blobStorageService.DeleteProductImageAsync(existingProduct.ImageURL);
                            }

                            using var stream = productImage.OpenReadStream();
                            existingProduct.ImageURL = await _blobStorageService.UploadProductImageAsync(
                                stream, productImage.FileName, existingProduct.RowKey);
                        }
                        catch (Exception ex)
                        {
                            ModelState.AddModelError("", $"Error uploading image: {ex.Message}");
                            return View(product);
                        }
                    }
                    else
                    {
                        existingProduct.ImageURL = product.ImageURL;
                    }

                    existingProduct.ProductName = product.ProductName;
                    existingProduct.Brand = product.Brand;
                    existingProduct.ProductDescription = product.ProductDescription;
                    existingProduct.ProductCategory = product.ProductCategory;
                    existingProduct.PriceRand = product.PriceRand;
                    existingProduct.PriceCents = product.PriceCents;
                    existingProduct.Size = product.Size;
                    existingProduct.KeyIngredients = product.KeyIngredients;
                    existingProduct.StockQuantity = product.StockQuantity;
                    existingProduct.LowStockThreshold = product.LowStockThreshold;

                    await _tableStorageService.UpdateProductAsync(existingProduct);

                    if (_queueStorageService != null && existingProduct.StockQuantity <= existingProduct.LowStockThreshold)
                    {
                        await _queueStorageService.SendLowStockAlertAsync(
                            existingProduct.RowKey,
                            existingProduct.ProductName,
                            existingProduct.StockQuantity,
                            existingProduct.LowStockThreshold);
                    }

                    return RedirectToAction(nameof(Index));
                }
                catch (RequestFailedException ex) when (ex.Status == 412)
                {
                    ModelState.AddModelError("", "Concurrency conflict - this record was modified by another user. Please refresh and try again.");
                }
                catch (Exception)
                {
                    ModelState.AddModelError("", "An error occurred while updating the product.");
                }
            }
            return View(product);
        }

        // GET: Products/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _tableStorageService.GetProductAsync("PRODUCT", id);

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // POST: Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var product = await _tableStorageService.GetProductAsync("PRODUCT", id);
            if (product != null)
            {
                if (!string.IsNullOrEmpty(product.ImageURL) &&
                    !product.ImageURL.Contains("default-product"))
                {
                    await _blobStorageService.DeleteProductImageAsync(product.ImageURL);
                }
            }

            await _tableStorageService.DeleteProductAsync("PRODUCT", id);
            return RedirectToAction(nameof(Index));
        }

        //private bool ProductExists(int id)
        //{
        //    return _context.Product.Any(e => e.ProductID == id);
        //}
    }
}