using KlipboardAssessment.Data;
using KlipboardAssessment.Models;
using Microsoft.AspNetCore.Mvc;
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

        // GET: Transactions
        public async Task<IActionResult> Index(int id)
        {
            if (id == null) return NotFound();

            // Getting the current customer's Transactions
            // (Where TransactionId == CustomerId)
            var customer = await _context.Transactions.Where(transaction => transaction.CustomerId == id)
                .ToListAsync();

            return View(await _context.Transactions.ToListAsync());
        }

        // GET: Transactions/AddTransaction
        public async Task<IActionResult> AddTransaction()
        {
            ViewBag.AccountNumbers = await _context.Customers.Select(customer => customer.AccountNumber)
                .ToListAsync();

            return View();
        }

        // POST: Transactions/AddTransaction
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTransaction(Transaction transaction)
        {
            // The model state will be invalid at this point because CustomerId is 0 or null.
            // That's okay, because we're going to fix it.

            // 1. Find the customer based on the submitted AccountNumber.
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.AccountNumber == transaction.AccountNumber);

            // 2. Check if a customer was found. If not, add an error.
            if (customer == null)
            {
                ModelState.AddModelError("AccountNumber", "Invalid Account Number.");
                return View(transaction); // Return the view to show the error.
            }

            // 3. Set the CustomerId on the transaction object.
            // This is the key step that fixes the validation issue.
            transaction.CustomerId = customer.Id;

            // 4. Now that the model has the correct CustomerId, you can add it to the database.
            // At this point, the model would be valid.
            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            // 5. Redirect the user.
            return RedirectToAction(nameof(Index));
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
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
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

        // GET: Transactions/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var transaction = await _context.Transactions
                .FirstOrDefaultAsync(m => m.Id == id);
            if (transaction == null)
            {
                return NotFound();
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
