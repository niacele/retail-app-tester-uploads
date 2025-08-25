using Azure;
using Azure.Data.Tables;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using retail_app_tester.Models;
using retail_app_tester.Services;

namespace retail_app_tester.Controllers
{
    public class CustomersController : Controller
    {

        private readonly TableStorageService _tableStorageService;
        private readonly QueueStorageService _queueStorageService;

        public CustomersController(TableStorageService tableStorageService, QueueStorageService queueStorageService)
        {
            _tableStorageService = tableStorageService;
            _queueStorageService = queueStorageService;
        }


        // GET: Customers
        public async Task<IActionResult> Index()
        {
            var customers = await _tableStorageService.GetAllCustomersAsync();
            return View(customers);
        }

        // GET: Customers/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var customer = await _tableStorageService.GetCustomerAsync("CUSTOMER", id);

            if (customer == null)
            {
                return NotFound();
            }

            return View(customer);
        }

        // GET: Customers/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Customers/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("RowKey,CustomerName,CustomerEmail,PhoneNumber,ShippingAddress, CustomerPassword")] Customer customer)
        {
            if (ModelState.IsValid)
            {
                customer.PartitionKey = "CUSTOMER";
                customer.RowKey = Guid.NewGuid().ToString("N");
                await _tableStorageService.AddCustomerAsync(customer);
                if (_queueStorageService != null)
                {
                    await _queueStorageService.SendCustomerNotificationAsync(customer.RowKey,
                 $"New customer registered: {customer.CustomerName} ({customer.CustomerEmail}) | Phone: {customer.PhoneNumber}");
                }

                return RedirectToAction(nameof(Index));
            }
            return View(customer);
        }

        // GET: Customers/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var customer = await _tableStorageService.GetCustomerAsync("CUSTOMER", id);
            if (customer == null)
            {
                return NotFound();
            }
            return View(customer);
        }

        // POST: Customers/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("RowKey,CustomerName,CustomerEmail,PhoneNumber,ShippingAddress, CustomerPassword")] Customer customer)
        {
            if (id != customer.RowKey)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    customer.PartitionKey = "CUSTOMER";
                    await _tableStorageService.UpdateCustomerAsync(customer);
                }
                catch (Exception)
                {
                    ModelState.AddModelError("", "Concurrency conflict - record was modified by another user.");
                }
            }
            return View(customer);
        }

        // GET: Customers/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var customer = await _tableStorageService.GetCustomerAsync("CUSTOMER", id);

            if (customer == null)
            {
                return NotFound();
            }

            return View(customer);
        }

        // POST: Customers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            await _tableStorageService.DeleteCustomerAsync("CUSTOMER", id);
            return RedirectToAction(nameof(Index));
        }

        //private bool CustomerExists(int id)
        //{
        //    return _context.Customer.Any(e => e.CustomerID == id);
        //}
    }
}
