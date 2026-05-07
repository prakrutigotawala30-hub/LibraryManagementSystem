using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Controllers
{
    public class BorrowController : Controller
    {
        private readonly AppDbContext _context;

        public BorrowController(AppDbContext context)
        {
            _context = context;
        }

        // ================= INDEX =================
        public async Task<IActionResult> Index()
        {
            var records = await _context.BorrowRecords
                .AsNoTracking()
                .Include(b => b.Book)
                .Include(b => b.Member)
                .OrderByDescending(b => b.IssuedOn)
                .Take(50)
                .ToListAsync();

            return View(records);
        }

        // ================= ISSUE GET =================
        [HttpGet]
        public async Task<IActionResult> Issue()
        {
            // BOOKS
            ViewBag.Books = new SelectList(
                await _context.Books.ToListAsync(),
                "Id",
                "Title"
            );

            // ONLY MEMBERS WITH ACTIVE MEMBERSHIP
            var activeMembers = await _context.Members
                .Where(m => _context.Memberships.Any(ms =>
                    ms.MemberId == m.Id &&
                    ms.IsActive &&
                    ms.EndDate > DateTime.Now))
                .ToListAsync();

            ViewBag.Members = new SelectList(
                activeMembers,
                "Id",
                "Name"
            );

            return View();
        }

        // ================= ISSUE POST =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Issue(BorrowRecord Record)
        {
            if (ModelState.IsValid)
            {
                // CHECK MEMBERSHIP
                var membership = await _context.Memberships
                    .FirstOrDefaultAsync(m =>
                        m.MemberId == Record.MemberId &&
                        m.IsActive);

                // NO MEMBERSHIP
                if (membership == null)
                {
                    TempData["Error"] =
                        "❌ Membership required before issuing books.";

                    return RedirectToAction(nameof(Issue));
                }

                // MEMBERSHIP EXPIRED
                if (membership.EndDate < DateTime.Now)
                {
                    TempData["Error"] =
                        "❌ Membership expired. Renew membership first.";

                    return RedirectToAction(nameof(Issue));
                }

                // CHECK BOOK
                var book = await _context.Books
                    .FirstOrDefaultAsync(b => b.Id == Record.BookId);

                if (book == null)
                {
                    TempData["Error"] = "Book not found.";
                    return RedirectToAction(nameof(Issue));
                }

                // CHECK AVAILABLE QUANTITY
                if (book.AvailableCopies <= 0)
                {
                    TempData["Error"] =
                        "❌ Book not available in stock.";

                    return RedirectToAction(nameof(Issue));
                }

                // ISSUE BOOK
                Record.IssuedOn = DateTime.Now;

                if (Record.DueDate == DateTime.MinValue)
                {
                    Record.DueDate = DateTime.Now.AddDays(7);
                }

                Record.Status = "Issued";
                Record.FineAmount = 0;

                // REDUCE STOCK
                book.AvailableCopies -= 1;

                _context.BorrowRecords.Add(Record);

                _context.Update(book);

                await _context.SaveChangesAsync();

                TempData["Success"] =
                    "✅ Book issued successfully!";

                return RedirectToAction(nameof(Index));
            }

            // RELOAD DROPDOWNS
            ViewBag.Books = new SelectList(
                await _context.Books.ToListAsync(),
                "Id",
                "Title",
                Record.BookId
            );

            var activeMembers = await _context.Members
                .Where(m => _context.Memberships.Any(ms =>
                    ms.MemberId == m.Id &&
                    ms.IsActive &&
                    ms.EndDate > DateTime.Now))
                .ToListAsync();

            ViewBag.Members = new SelectList(
                activeMembers,
                "Id",
                "Name",
                Record.MemberId
            );

            return View(Record);
        }

        // ================= RETURN GET =================
        [HttpGet]
        public async Task<IActionResult> Return(int id)
        {
            var record = await _context.BorrowRecords
                .Include(b => b.Book)
                .Include(b => b.Member)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (record == null)
                return NotFound();

            // FINE CALCULATION
            if (record.ReturnedOn == null &&
                DateTime.Now > record.DueDate)
            {
                var daysLate =
                    (DateTime.Now - record.DueDate).Days;

                record.FineAmount = daysLate * 10;
            }

            return View(record);
        }

        // ================= RETURN POST =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReturnConfirmed(int id)
        {
            var record = await _context.BorrowRecords
                .Include(b => b.Book)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (record == null)
                return NotFound();

            // RETURN DATE
            record.ReturnedOn = DateTime.Now;

            // FINE
            if (record.ReturnedOn > record.DueDate)
            {
                var daysLate =
                    (record.ReturnedOn.Value - record.DueDate).Days;

                record.FineAmount = daysLate * 10;
            }

            record.Status = "Returned";

            // INCREASE STOCK
            if (record.Book != null)
            {
                record.Book.AvailableCopies += 1;
            }

            _context.Update(record);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "✅ Book returned successfully!";

            return RedirectToAction(nameof(Index));
        }

        // ================= HISTORY =================
        public async Task<IActionResult> History(int memberId)
        {
            var records = await _context.BorrowRecords
                .Include(b => b.Book)
                .Include(b => b.Member)
                .Where(b => b.MemberId == memberId)
                .OrderByDescending(b => b.IssuedOn)
                .ToListAsync();

            ViewBag.MemberName =
                records.FirstOrDefault()?.Member?.Name;

            return View(records);
        }
    }
}