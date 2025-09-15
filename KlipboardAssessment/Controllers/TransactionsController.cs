using KlipboardAssessment.Data;
using KlipboardAssessment.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace KlipboardAssessment.Controllers
{
    public class TransactionsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TransactionsController> _logger;

        public TransactionsController(ApplicationDbContext context, ILogger<TransactionsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Transactions/
        [HttpGet]
        public async Task<IActionResult> Index(int? id, string searchQuery, string sortField, string sortOrder)
        {
            // Start with all transactions or filter by customer
            var transactionsQuery = _context.Transactions.AsQueryable();

            if (id != null)
            {
                transactionsQuery = transactionsQuery.Where(t => t.CustomerId == id);

                // Also pass customer name + balance to ViewBag
                var customerName = await _context.Customers
                    .Where(c => c.Id == id)
                    .Select(c => c.Name)
                    .FirstOrDefaultAsync();
                ViewBag.CustomerName = customerName;

                var debitTotal = await _context.Transactions
                    .Where(t => t.CustomerId == id && t.Type == "D")
                    .SumAsync(t => (decimal?)t.Amount) ?? 0;
                var creditTotal = await _context.Transactions
                    .Where(t => t.CustomerId == id && t.Type == "C")
                    .SumAsync(t => (decimal?)t.Amount) ?? 0;
                var customerBalance = debitTotal - creditTotal;

                ViewBag.CustomerBalance = customerBalance;
                ViewBag.BalanceClass = customerBalance < 0 ? "danger" : "success";
            }

            // Search by reference or account number
            if (!string.IsNullOrEmpty(searchQuery))
            {
                transactionsQuery = transactionsQuery.Where(t =>
                    t.Reference.Contains(searchQuery) ||
                    t.AccountNumber.Contains(searchQuery));
            }

            // Sorting
            if (!string.IsNullOrEmpty(sortField) && !string.IsNullOrEmpty(sortOrder))
            {
                transactionsQuery = (sortField, sortOrder.ToLower()) switch
                {
                    ("Date", "asc") => transactionsQuery.OrderBy(t => t.Date),
                    ("Date", "desc") => transactionsQuery.OrderByDescending(t => t.Date),
                    ("Amount", "asc") => transactionsQuery.OrderBy(t => t.Amount),
                    ("Amount", "desc") => transactionsQuery.OrderByDescending(t => t.Amount),
                    ("Type", "asc") => transactionsQuery.OrderBy(t => t.Type),
                    ("Type", "desc") => transactionsQuery.OrderByDescending(t => t.Type),
                    _ => transactionsQuery
                };
            }
            else
            {
                // Default sort: by Date (descending = newest first)
                transactionsQuery = transactionsQuery.OrderByDescending(t => t.Date);
            }

            var transactionData = await transactionsQuery.ToListAsync();

            ViewBag.SearchQuery = searchQuery;
            ViewBag.SortField = sortField;
            ViewBag.SortOrder = sortOrder;
            ViewBag.CustomerId = id;

            return View(transactionData);
        }

        private async Task PopulateDropdownsAsync()
        {
            // Accounts
            var accounts = await _context.Customers
                .Select(c => c.AccountNumber)
                .ToListAsync();
            ViewBag.AccountNumbers = new SelectList(accounts);

            // Transaction types
            ViewBag.TransactionTypes = new List<SelectListItem>
            {
                new SelectListItem { Value = "C", Text = "C" },
                new SelectListItem { Value = "D", Text = "D" }
            };
        }

        [HttpGet]
        public async Task<IActionResult> AddTransaction()
        {
            await PopulateDropdownsAsync();

            var model = new TransactionBatchViewModel
            {
                Transactions = new List<Transaction> { new Transaction() }
            };

            return View(model);
        }

        // POST: Transactions/AddTransaction
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTransaction(TransactionBatchViewModel model)
        {
            await PopulateDropdownsAsync();

            if (model?.Transactions == null || !model.Transactions.Any())
            {
                ModelState.AddModelError("", "No transactions to save.");
                return View(model);
            }

            var savedCustomerIds = new HashSet<int>();
            // add model errors to specific rows
            for (int i = 0; i < model.Transactions.Count; i++)
            {
                var tx = model.Transactions[i];

                // skip fully empty rows (user added but didn't fill)
                var isEmpty = string.IsNullOrWhiteSpace(tx.AccountNumber)
                              && string.IsNullOrWhiteSpace(tx.Reference)
                              && (tx.Amount == 0 || tx.Amount == default);
                if (isEmpty) continue;

                if (string.IsNullOrWhiteSpace(tx.AccountNumber))
                {
                    ModelState.AddModelError($"Transactions[{i}].AccountNumber", "Account number is required.");
                    continue;
                }

                if (tx.Amount <= 0)
                {
                    ModelState.AddModelError($"Transactions[{i}].Amount", "Amount must be greater than 0.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(tx.Type) || (tx.Type != "C" && tx.Type != "D"))
                {
                    ModelState.AddModelError($"Transactions[{i}].Type", "Type must be C or D.");
                    continue;
                }

                // Get hold of customer obj
                var customer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.AccountNumber == tx.AccountNumber);

                if (customer == null)
                {
                    ModelState.AddModelError($"Transactions[{i}].AccountNumber", "Invalid account number.");
                    continue;
                }

                tx.CustomerId = customer.Id;

                // Adjust Customer Balance
                if (tx.Type == "D")
                    customer.Balance += tx.Amount;
                else if (tx.Type == "C")
                    customer.Balance -= tx.Amount;

                _context.Transactions.Add(tx);
                savedCustomerIds.Add(customer.Id);
            }

            // If any validation errors were added, return the view so user can fix
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (savedCustomerIds.Count > 0)
            {
                await _context.SaveChangesAsync();
            }

            // Iff only one customer was affected, goto THEIR transactions;
            // otherwise go to ALL transactions.
            if (savedCustomerIds.Count == 1)
            {
                var onlyId = savedCustomerIds.First();
                return RedirectToAction("Index", new { id = onlyId });
            }

            return RedirectToAction("Index"); // All transactions
        }

        // GET: Transactions/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var transaction = await _context.Transactions.FindAsync(id);
            if (transaction == null)
            {
                return NotFound();
            }
            return View(transaction);
        }

        // POST: Transactions/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,AccountNumber,Date,Reference,Amount,Type,CustomerId")] Transaction transaction)
        {
            if (id != transaction.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(transaction);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TransactionExists(transaction.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(transaction);
        }

        // POST: Transactions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var transaction = await _context.Transactions.FindAsync(id);
            if (transaction != null)
            {
                _context.Transactions.Remove(transaction);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool TransactionExists(int id)
        {
            return _context.Transactions.Any(e => e.Id == id);
        }
    }
}
