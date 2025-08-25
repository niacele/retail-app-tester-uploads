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

        public ContractsController(TableStorageService tableStorageService)
        {
            _tableStorageService = tableStorageService;
        }

        // GET: Contracts
        public async Task<IActionResult> Index()
        {
            // Get all orders that have contracts
            var orders = await _tableStorageService.GetAllOrdersAsync();
            var ordersWithContracts = orders
                .Where(o => !string.IsNullOrEmpty(o.ContractFileName) &&
                           !string.IsNullOrEmpty(o.CustomerRowKey))
                .ToList();

            var contracts = new List<ContractViewModel>();

            foreach (var order in ordersWithContracts)
            {
                // Get customer details
                var customer = await _tableStorageService.GetCustomerAsync("CUSTOMER", order.CustomerRowKey);

                // Get order items to show what was ordered
                var orderItems = await _tableStorageService.GetOrderItemsAsync(order.RowKey);
                var itemSummary = string.Join(", ", orderItems.Take(3).Select(i => i.ProductName));
                if (orderItems.Count > 3) itemSummary += "...";

                contracts.Add(new ContractViewModel
                {
                    CustomerId = order.CustomerRowKey,
                    CustomerName = customer?.CustomerName ?? "Unknown Customer",
                    CustomerEmail = customer?.CustomerEmail ?? "",
                    OrderId = order.RowKey,
                    OrderTotal = order.OrderTotal,
                    OrderDate = order.OrderDate,
                    UploadDate = order.OrderDate, // Using order date as upload date for now
                    FileName = order.ContractFileName,
                    ItemsSummary = itemSummary,
                    ItemCount = orderItems.Count
                });
            }

            // Sort by most recent first
            contracts = contracts.OrderByDescending(c => c.UploadDate).ToList();

            return View(contracts);
        } 

        
        
    }
} 