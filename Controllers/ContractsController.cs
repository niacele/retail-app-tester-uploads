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
                var orders = await _tableStorageService.GetAllOrdersAsync();

                var creditOrders = orders
                    .Where(o => o.PaymentMethod == "Credit" &&
                               !string.IsNullOrEmpty(o.CustomerRowKey))
                    .ToList();

                Console.WriteLine($"DEBUG: Found {creditOrders.Count} credit orders");

                var contracts = new List<ContractViewModel>();

                foreach (var order in creditOrders)
                {
                    var customer = await _tableStorageService.GetCustomerAsync("CUSTOMER", order.CustomerRowKey);

                    var orderItems = await _tableStorageService.GetOrderItemsAsync(order.RowKey);
                    var itemSummary = string.Join(", ", orderItems.Take(3).Select(i => i.ProductName));
                    if (orderItems.Count > 3) itemSummary += "...";

                    bool contractExists = false;
                    string actualFileName = order.ContractFileName;

                    if (!string.IsNullOrEmpty(order.ContractFileName))
                    {
                        try
                        {
                            var files = await _fileShareService.ListCustomerContractsAsync(order.CustomerRowKey);
                            contractExists = files.Any(f => f.StartsWith(order.RowKey + "-contract"));

                            if (!contractExists)
                            {  
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

                contracts = contracts.OrderByDescending(c => c.UploadDate).ToList();

                return View(contracts);
            }
            catch (Exception ex)
            {

                return View(new List<ContractViewModel>());
            }
        }



    }
} 