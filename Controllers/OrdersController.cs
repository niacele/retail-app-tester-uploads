using Azure;
using Azure.Data.Tables;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using retail_app_tester.Models;
using retail_app_tester.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace retail_app_tester.Controllers
{
    public class OrdersController : Controller
    {
        private readonly TableStorageService _tableStorageService;
        private readonly QueueStorageService _queueStorageService;
        private readonly FileShareService _fileShareService;

        private readonly Random _random = new Random();

        public OrdersController(TableStorageService tableStorageService, QueueStorageService queueStorageService, FileShareService fileShareService)
        {
            _tableStorageService = tableStorageService;
            _queueStorageService = queueStorageService;
            _fileShareService = fileShareService;
        }

        private async Task PopulateCustomerDropdown(string? selectedValue = null)
        {
            var customers = await _tableStorageService.GetAllCustomersAsync();
            ViewData["CustomerRowKey"] = new SelectList(customers, "RowKey", "CustomerEmail", selectedValue);
        }

        private DateTime GenerateEstimatedDeliveryDate(DateTime orderDate)
        {
            int daysToAdd = _random.Next(2, 6); // 2-5 days
            DateTime estimatedDate = orderDate.AddDays(daysToAdd);

            // Skip weekends (optional)
            while (estimatedDate.DayOfWeek == DayOfWeek.Saturday ||
                   estimatedDate.DayOfWeek == DayOfWeek.Sunday)
            {
                estimatedDate = estimatedDate.AddDays(1);
            }

            return estimatedDate;
        }

        private string GenerateTrackingNumber()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return $"1Z{new string(Enumerable.Repeat(chars, 5)
                .Select(s => s[_random.Next(s.Length)]).ToArray())}";
        }

        private (double subtotal, double vat, double total) CalculateOrderTotals(List<OrderItem> orderItems, double shippingFee)
        {
            const double vatRate = 0.15; // 15% VAT
            double subtotal = orderItems.Sum(item => item.Quantity * (double)item.UnitPrice);
            double vatAmount = subtotal * vatRate;
            double actualShippingFee = 200.0;
            double total = subtotal + vatAmount + actualShippingFee;
            return (subtotal, vatAmount, total);
        }


        // GET: Orders
        public async Task<IActionResult> Index()
        {
            var orders = await _tableStorageService.GetAllOrdersAsync();

            var customerNames = new Dictionary<string, string>();

            foreach (var order in orders)
            {
                if (!string.IsNullOrEmpty(order.CustomerRowKey) && !customerNames.ContainsKey(order.CustomerRowKey))
                {
                    var customer = await _tableStorageService.GetCustomerAsync("CUSTOMER", order.CustomerRowKey);
                    customerNames[order.CustomerRowKey] = customer?.CustomerName ?? $"Customer ID: {order.CustomerRowKey}";
                }
            }

            ViewBag.CustomerNames = customerNames;
            return View(orders);
        }

        // GET: Orders/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _tableStorageService.GetOrderAsync("ORDER", id);
            if (order == null)
            {
                return NotFound();
            }

            // Load customer information if available
            if (!string.IsNullOrEmpty(order.CustomerRowKey))
            {
                var customer = await _tableStorageService.GetCustomerAsync("CUSTOMER", order.CustomerRowKey);
                if (customer != null)
                {
                    order.CustomerName = customer.CustomerName;
                    order.CustomerEmail = customer.CustomerEmail;
                    ViewBag.CustomerAddress = customer.ShippingAddress;
                }
            }

            return View(order);
        }

        // GET: Orders/Create
        //public async Task<IActionResult> Create()
        //{
        //    //var customers = _customersTable.QueryAsync<Customer>();
        //    //ViewData["CustomerRowKey"] = new SelectList(await customers.ToListAsync(), "RowKey", "CustomerEmail");
        //    //return View();

        //    //var customers = _customersTable.QueryAsync<Customer>();
        //    //var customerList = await ToListAsync(customers);
        //    //ViewData["CustomerRowKey"] = new SelectList(customerList, "RowKey", "CustomerEmail");
        //    await PopulateCustomerDropdown();
        //    return View();

        //}

        [HttpPost]
        public async Task<IActionResult> AddProductToOrder(string productRowKey, int quantity = 1)
        {
            string orderId = TempData["CurrentOrderId"] as string;
            Order order;

            if (string.IsNullOrEmpty(orderId))
            {
                Console.WriteLine("DEBUG: Creating NEW order with shipping fee 200");

                // Create new order
                order = new Order
                {
                    PartitionKey = "ORDER",
                    RowKey = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper(),
                    OrderDate = DateTime.UtcNow,
                    ShippingFee = 200.0, // Use double literal
                    SubTotal = 0.0,
                    VATAmount = 0.0,
                    OrderTotal = 200.0
                };
                await _tableStorageService.AddOrderAsync(order);
                orderId = order.RowKey;
                TempData["CurrentOrderId"] = orderId;
                Console.WriteLine($"DEBUG: New order created with ID: {orderId}, ShippingFee: {order.ShippingFee}");
            }
            else
            {
                Console.WriteLine($"DEBUG: Retrieving EXISTING order: {orderId}");
                // Get existing order
                order = await _tableStorageService.GetOrderAsync("ORDER", orderId);
                if (order == null)
                {
                    Console.WriteLine("DEBUG: Order not found, redirecting to Index");
                    return RedirectToAction("Index");
                }
                Console.WriteLine($"DEBUG: Retrieved order - ShippingFee: {order.ShippingFee}");
            }

            // Get product details
            var product = await _tableStorageService.GetProductAsync("PRODUCT", productRowKey);
            if (product == null) return RedirectToAction("Index");

            // Calculate the full price from Rand and Cents
            double unitPrice = (double)(product.PriceRand + (product.PriceCents / 100m));

            // Add or update order item
            var existingItem = await _tableStorageService.GetOrderItemAsync(orderId, productRowKey);

            if (existingItem != null)
            {
                // Update existing item
                existingItem.Quantity += quantity;
                await _tableStorageService.UpdateOrderItemAsync(existingItem);
            }
            else
            {
                // Create new order item
                var orderItem = new OrderItem
                {
                    PartitionKey = orderId,
                    RowKey = productRowKey,
                    ProductRowKey = productRowKey,
                    ProductName = product.ProductName,
                    UnitPrice = unitPrice,
                    Quantity = quantity
                };
                await _tableStorageService.AddOrderItemAsync(orderItem);
            }

            // Update order totals
            await UpdateOrderTotals(orderId);

            return RedirectToAction("Edit", new { id = orderId });
        }



        private async Task UpdateOrderTotals(string orderId)
        {
            var orderItems = await _tableStorageService.GetOrderItemsAsync(orderId);
            var order = await _tableStorageService.GetOrderAsync("ORDER", orderId);

            if (order != null)
            {
                var totals = CalculateOrderTotals(orderItems, order.ShippingFee);
                order.SubTotal = totals.subtotal;
                order.VATAmount = totals.vat;
                order.OrderTotal = totals.total;

                await _tableStorageService.UpdateOrderAsync(order);
            }
        }

        [HttpPost]
        public async Task<IActionResult> CompleteOrder(string orderId, string CustomerRowKey, string paymentMethod, IFormFile contractFile = null)
        {
            Console.WriteLine($"DEBUG: CompleteOrder called - orderId: {orderId}, CustomerRowKey: {CustomerRowKey}, paymentMethod: {paymentMethod}");

            var order = await _tableStorageService.GetOrderAsync("ORDER", orderId);
            if (order == null)
            {
                Console.WriteLine("DEBUG: Order not found");
                return RedirectToAction("Index");
            }

            Console.WriteLine($"DEBUG: Found order - Current CustomerRowKey: {order.CustomerRowKey}");

            // Handle credit payment requirement - ONLY if payment method is Credit
            if (paymentMethod == "Credit")
            {
                if (contractFile == null || contractFile.Length == 0)
                {
                    Console.WriteLine("DEBUG: Credit payment selected but no contract file provided");
                    ModelState.AddModelError("", "Contract file is required for credit payments");

                    // Reload everything for the view
                    await PopulateCustomerDropdown(order.CustomerRowKey);
                    var orderItems = await _tableStorageService.GetOrderItemsAsync(orderId);
                    ViewBag.OrderItems = orderItems;

                    return View("Edit", order);
                }

                // Validate file type and size for credit payments
                var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png" };
                var fileExtension = Path.GetExtension(contractFile.FileName).ToLower();

                if (!allowedExtensions.Contains(fileExtension))
                {
                    ModelState.AddModelError("", "Invalid file type. Please upload PDF, Word, or image files.");
                    await PopulateCustomerDropdown(order.CustomerRowKey);
                    var orderItems = await _tableStorageService.GetOrderItemsAsync(orderId);
                    ViewBag.OrderItems = orderItems;
                    return View("Edit", order);
                }

                if (contractFile.Length > 5 * 1024 * 1024) // 5MB limit
                {
                    ModelState.AddModelError("", "File size too large. Maximum size is 5MB.");
                    await PopulateCustomerDropdown(order.CustomerRowKey);
                    var orderItems = await _tableStorageService.GetOrderItemsAsync(orderId);
                    ViewBag.OrderItems = orderItems;
                    return View("Edit", order);
                }
            }

            // ✅ Update the order with the selected customer
            order.CustomerRowKey = CustomerRowKey;
            Console.WriteLine($"DEBUG: Set CustomerRowKey to: {order.CustomerRowKey}");

            // Upload contract if provided AND payment method is Credit
            if (paymentMethod == "Credit" && contractFile != null && contractFile.Length > 0)
            {
                try
                {
                    Console.WriteLine("DEBUG: Uploading contract file...");
                    using var stream = contractFile.OpenReadStream();
                    var contractFileName = await _fileShareService.UploadContractAsync(
                        order.CustomerRowKey, orderId, stream, contractFile.FileName);

                    order.ContractFileName = contractFileName;
                    Console.WriteLine($"DEBUG: Contract uploaded - FileName: {order.ContractFileName}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"DEBUG: Contract upload failed: {ex.Message}");
                    ModelState.AddModelError("", $"Error uploading contract: {ex.Message}");
                    await PopulateCustomerDropdown(order.CustomerRowKey);
                    var orderItems = await _tableStorageService.GetOrderItemsAsync(orderId);
                    ViewBag.OrderItems = orderItems;
                    return View("Edit", order);
                }
            }

            // Set final order details
            order.PaymentMethod = paymentMethod;
            order.TrackingNumber = GenerateTrackingNumber();
            order.EstimatedDeliveryDate = GenerateEstimatedDeliveryDate(order.OrderDate);

            Console.WriteLine($"DEBUG: Saving order with - PaymentMethod: {order.PaymentMethod}, ContractFileName: {order.ContractFileName}");

            // ✅ Save the updated order
            await _tableStorageService.UpdateOrderAsync(order);
            TempData.Remove("CurrentOrderId");

            Console.WriteLine("DEBUG: Order saved successfully");

            // Send queue notification
            if (_queueStorageService != null)
            {
                await _queueStorageService.SendOrderNotificationAsync(orderId,
                    $"Order placed with {paymentMethod}. Total: {order.OrderTotal:C}");
            }

            return RedirectToAction("OrderConfirmation", new { id = orderId });
        }

        public async Task<IActionResult> OrderConfirmation(string id)
        {
            var order = await _tableStorageService.GetOrderAsync("ORDER", id);
            if (order != null)
            {
                var orderItems = await _tableStorageService.GetOrderItemsAsync(id);
                ViewBag.OrderItems = orderItems;

                // Load customer information if available
                if (!string.IsNullOrEmpty(order.CustomerRowKey))
                {
                    var customer = await _tableStorageService.GetCustomerAsync("CUSTOMER", order.CustomerRowKey);
                    if (customer != null)
                    {
                        // Create a view model or use ViewBag to pass customer info
                        ViewBag.CustomerName = customer.CustomerName;
                        ViewBag.CustomerEmail = customer.CustomerEmail;
                        ViewBag.CustomerAddress = customer.ShippingAddress;
                    }
                }
            }
            return View(order);
        }

        // GET: Orders/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null) return NotFound();

            var order = await _tableStorageService.GetOrderAsync("ORDER", id);
            if (order == null) return NotFound();

            // Load customer information if available
            if (!string.IsNullOrEmpty(order.CustomerRowKey))
            {
                var customer = await _tableStorageService.GetCustomerAsync("CUSTOMER", order.CustomerRowKey);
                if (customer != null)
                {
                    order.CustomerName = customer.CustomerName;
                    order.CustomerEmail = customer.CustomerEmail;
                }
            }

            var orderItems = await _tableStorageService.GetOrderItemsAsync(id);
            ViewBag.OrderItems = orderItems;

            await PopulateCustomerDropdown(order.CustomerRowKey);
            return View(order);
        }

        // POST: Orders/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("RowKey,CustomerRowKey,PaymentMethod")] Order order)
        {
            if (id != order.RowKey) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var existingOrder = await _tableStorageService.GetOrderAsync("ORDER", id);
                    if (existingOrder == null) return NotFound();

                    // Only update allowed fields
                    existingOrder.CustomerRowKey = order.CustomerRowKey;
                    existingOrder.PaymentMethod = order.PaymentMethod;

                    await _tableStorageService.UpdateOrderAsync(existingOrder);
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception)
                {
                    ModelState.AddModelError("", "This record was modified by another user.");
                }
            }

            await PopulateCustomerDropdown(order.CustomerRowKey);
            return View(order);
        }

        // GET: Orders/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null) return NotFound();

            var order = await _tableStorageService.GetOrderAsync("ORDER", id);
            if (order == null) return NotFound();

            return View(order);
        }

        // POST: Orders/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            // Delete all order items first
    await _tableStorageService.DeleteAllOrderItemsAsync(id);
            // Then delete the order
            await _tableStorageService.DeleteOrderAsync("ORDER", id);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> UpdateQuantity(string orderId, string productRowKey, int quantity)
        {
            if (quantity < 1) return await RemoveItem(orderId, productRowKey);

            var orderItem = await _tableStorageService.GetOrderItemAsync(orderId, productRowKey);
            if (orderItem != null)
            {
                orderItem.Quantity = quantity;
                await _tableStorageService.UpdateOrderItemAsync(orderItem);
                await UpdateOrderTotals(orderId);
            }

            return RedirectToAction("Edit", new { id = orderId });
        }

        [HttpPost]
        public async Task<IActionResult> RemoveItem(string orderId, string productRowKey)
        {
            await _tableStorageService.DeleteOrderItemAsync(orderId, productRowKey);
            await UpdateOrderTotals(orderId);

            return RedirectToAction("Edit", new { id = orderId });
        }





        // POST: Orders/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Create([Bind("CustomerRowKey,OrderDate,ShippingFee")] Order order)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        order.PartitionKey = "ORDER";
        //        order.RowKey = Guid.NewGuid().ToString("N");
        //        order.EstimatedDeliveryDate = GenerateEstimatedDeliveryDate(order.OrderDate);
        //        order.TrackingNumber = GenerateTrackingNumber();

        //        order.SubTotal = 0;
        //        order.VATAmount = 0;
        //        order.OrderTotal = order.ShippingFee;

        //        await _ordersTable.AddEntityAsync(order);
        //        return RedirectToAction(nameof(Index));
        //    }
        //    //var customers = _customersTable.QueryAsync<Customer>();
        //    //var customerList = await ToListAsync(customers);
        //    //ViewData["CustomerRowKey"] = new SelectList(customerList, "RowKey", "CustomerEmail");
        //    await PopulateCustomerDropdown();
        //    return View(order);

        //    //var customers = _customersTable.QueryAsync<Customer>();
        //    //ViewData["CustomerRowKey"] = new SelectList(await customers.ToListAsync(), "RowKey", "CustomerEmail", order.CustomerRowKey); return View(order);
        //}

        // GET: Orders/Edit/5


        // POST: Orders/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.







        //private bool OrderExists(int id)
        //{
        //    return _context.Order.Any(e => e.OrderID == id);
        //}
    }
}