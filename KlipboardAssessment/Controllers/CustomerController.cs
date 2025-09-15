using KlipboardAssessment.Data;
using KlipboardAssessment.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KlipboardAssessment.Controllers
{
    public class CustomerController : Controller
    {
        private readonly ILogger<CustomerController> _logger;
        private readonly ApplicationDbContext _context;

        public CustomerController(ApplicationDbContext context, ILogger<CustomerController> logger)
        {
            // Constructor w Dependency Injection
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string searchQuery, string sortField, string sortOrder)
        {
            // generating query before actually executing it
            var customersQuery = _context.Customers.AsQueryable();

            if (!string.IsNullOrEmpty(searchQuery))
            {
                // Search query filer
                customersQuery = customersQuery.Where(c => c.Name.Contains(searchQuery) || c.AccountNumber.Contains(searchQuery));
            }

            if (!string.IsNullOrEmpty(sortOrder) && !string.IsNullOrEmpty(sortField))
            {
                // Sorting logic
                customersQuery = (sortField, sortOrder.ToLower()) switch
                {
                    ("Name", "asc") => customersQuery.OrderBy(c => c.Name),
                    ("Name", "desc") => customersQuery.OrderByDescending(c => c.Name),
                    ("AccountNumber", "asc") => customersQuery.OrderBy(c => c.AccountNumber),
                    ("AccountNumber", "desc") => customersQuery.OrderByDescending(c => c.AccountNumber),
                    ("Balance", "asc") => customersQuery.OrderBy(c => c.Balance),
                    ("Balance", "desc") => customersQuery.OrderByDescending(c => c.Balance),
                    _ => customersQuery
                };
            }
            else
            {
                // Default (edge case)
                customersQuery = customersQuery.OrderBy(c => c.Name);
            }

            // Finally getting the customer list
            var customerData = await customersQuery.ToListAsync();

            ViewBag.SearchQuery = searchQuery;
            ViewBag.SortField = sortField;
            ViewBag.SortOrder = sortOrder;

            return View(customerData);
        }

        [HttpGet]
        public IActionResult AddCustomer()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCustomer(Customer customer)
        {
            // Checking if the AccountNumber already exists
            bool accountExists = await _context.Customers.AnyAsync(c => c.AccountNumber == customer.AccountNumber);
            if (accountExists)
            {
                ModelState.AddModelError("AccountNumber", "An account with this number already exists.");
            }


            // Customer Object is automatically populated by model binder
            if (ModelState.IsValid && !accountExists)
            {
                await _context.Customers.AddAsync(customer);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index)); // Safer to use nameof() in case of renaming  
            }

            // The model was NOT  valid, return with previously filled in values
            return View(customer);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            if (id == 0)
            {
                return NotFound();
            }

            // Getting hold of customer obj
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null)
            {
                return NotFound();
            }

            // Passing the current DB details into the View
            return View(customer);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Customer customer)
        {
            if (ModelState.IsValid)
            {
                // Update Customer values
                _context.Customers.Update(customer);

                // Save changes
                await _context.SaveChangesAsync();

                // Redirect to All Customers
                return RedirectToAction(nameof(Index));
            }

            // Return view with previous values if its not valid
            return View(customer);

        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            // Getting hold of the Customer obj
            var customer = await _context.Customers.FindAsync(id);

            if (customer == null) return NotFound();

            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}
