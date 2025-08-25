using Microsoft.AspNetCore.Mvc;
using retail_app_tester.Models;
using retail_app_tester.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace retail_app_tester.Controllers
{
    public class ContractsController : Controller
    {
        private readonly TableStorageService _tableStorageService;
        private readonly FileShareService _fileShareService;

        public ContractsController(TableStorageService tableStorageService, FileShareService fileShareService)
        {
            _tableStorageService = tableStorageService;
            _fileShareService = fileShareService;
        }

        // GET: Contracts
        public async Task<IActionResult> Index()
        {
            try
            {
                // Get all orders
                var orders = await _tableStorageService.GetAllOrdersAsync();

                // Debug: Check what orders we have
                Console.WriteLine($"DEBUG: Found {orders.Count} total orders");
                foreach (var order in orders)
                {
                    Console.WriteLine($"DEBUG: Order {order.RowKey} - ContractFileName: '{order.ContractFileName}', CustomerRowKey: '{order.CustomerRowKey}', PaymentMethod: '{order.PaymentMethod}'");
                }

                // Filter orders that should have contracts (credit payments)
                var creditOrders = orders
                    .Where(o => o.PaymentMethod == "Credit" &&
                               !string.IsNullOrEmpty(o.CustomerRowKey))
                    .ToList();

                Console.WriteLine($"DEBUG: Found {creditOrders.Count} credit orders");

                var contracts = new List<ContractViewModel>();

                foreach (var order in creditOrders)
                {
                    // Get customer details
                    var customer = await _tableStorageService.GetCustomerAsync("CUSTOMER", order.CustomerRowKey);

                    // Get order items
                    var orderItems = await _tableStorageService.GetOrderItemsAsync(order.RowKey);
                    var itemSummary = string.Join(", ", orderItems.Take(3).Select(i => i.ProductName));
                    if (orderItems.Count > 3) itemSummary += "...";

                    // Check if contract file actually exists in file share
                    bool contractExists = false;
                    string actualFileName = order.ContractFileName;

                    if (!string.IsNullOrEmpty(order.ContractFileName))
                    {
                        try
                        {
                            // Verify the file actually exists in Azure File Share
                            var files = await _fileShareService.ListCustomerContractsAsync(order.CustomerRowKey);
                            contractExists = files.Any(f => f.StartsWith(order.RowKey + "-contract"));

                            if (!contractExists)
                            {
                                Console.WriteLine($"DEBUG: Contract file not found in file share for order {order.RowKey}");
                                // Try to find the actual file name
                                var allFiles = await _fileShareService.ListCustomerContractsAsync(order.CustomerRowKey);
                                var matchingFile = allFiles.FirstOrDefault(f => f.Contains(order.RowKey));
                                if (matchingFile != null)
                                {
                                    actualFileName = matchingFile;
                                    contractExists = true;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"DEBUG: Error checking file share: {ex.Message}");
                        }
                    }

                    contracts.Add(new ContractViewModel
                    {
                        CustomerId = order.CustomerRowKey,
                        CustomerName = customer?.CustomerName ?? "Unknown Customer",
                        CustomerEmail = customer?.CustomerEmail ?? "",
                        OrderId = order.RowKey,
                        OrderTotal = order.OrderTotal,
                        OrderDate = order.OrderDate,
                        UploadDate = order.OrderDate,
                        FileName = actualFileName,
                        ItemsSummary = itemSummary,
                        ItemCount = orderItems.Count,
                        ContractExists = contractExists
                    });
                }

                // Sort by most recent first
                contracts = contracts.OrderByDescending(c => c.UploadDate).ToList();

                Console.WriteLine($"DEBUG: Returning {contracts.Count} contracts to view");
                return View(contracts);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in Contracts Index: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                return View(new List<ContractViewModel>());
            }
        }



    }
} 