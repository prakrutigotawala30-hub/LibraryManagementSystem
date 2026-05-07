using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.ViewModels;

namespace LibraryManagementSystem.Controllers
{
    public class BooksController : Controller
    {
        private readonly AppDbContext _context;

        public BooksController(AppDbContext context)
        {
            _context = context;
        }

        // INDEX + SEARCH
        public async Task<IActionResult> Index(string? search)
        {

            var query = _context.Books
                .Include(b => b.Author)
                .Include(b => b.Category)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(b =>
                    b.Title.Contains(search) ||
                    b.ISBN.Contains(search) ||
                    b.Author!.Name.Contains(search) ||
                    b.Category!.Name.Contains(search)
                    );
            }

            var data = await query.ToListAsync();

            ViewBag.Search = search;
            return View(data);
        }

        // DETAILS 
        public async Task<IActionResult> Details(int id)
        {
            var book = await _context.Books
                .Include(b => b.Author)
                .Include(b => b.Category)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (book == null)
                return NotFound();

            var borrowHistory = await _context.BorrowRecords
                .Include(b => b.Member)
                .Where(b => b.BookId == id)
                .OrderByDescending(b => b.IssuedOn)
                .ToListAsync();
            var vm = new BookDetailsViewModel
            {
                Book = book,
                BorrowHistory = borrowHistory ?? new List<BorrowRecord>()
            };

            return View(vm);
        }

        // ADD BOOK
        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.CategoryList = new SelectList(_context.Categories, "Id", "Name");
            ViewBag.AuthorList = new SelectList(_context.Authors, "Id", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Book book)
        {
            if (book.CategoryId == 0)
                ModelState.AddModelError("CategoryId", "Select Category");

            if (book.AuthorId == 0)
                ModelState.AddModelError("AuthorId", "Select Author");

            if (book.TotalCopies <= 0)
                ModelState.AddModelError("TotalCopies", "Enter valid total copies");

            book.AvailableCopies = book.TotalCopies;

            if (ModelState.IsValid)
            {
                _context.Books.Add(book);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Book added successfully!";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.CategoryList = new SelectList(_context.Categories, "Id", "Name", book.CategoryId);
            ViewBag.AuthorList = new SelectList(_context.Authors, "Id", "Name", book.AuthorId);

            return View(book);
        }
        // EDIT BOOK

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var book = await _context.Books.FindAsync(id);

            if (book == null)
                return NotFound();

            ViewBag.CategoryList = new SelectList(_context.Categories, "Id", "Name", book.CategoryId);
            ViewBag.AuthorList = new SelectList(_context.Authors, "Id", "Name", book.AuthorId);

            return View(book);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Book book)
        {
            if (id != book.Id)
                return NotFound();

            if (book.CategoryId == 0)
                ModelState.AddModelError("CategoryId", "Select Category");

            if (book.AuthorId == 0)
                ModelState.AddModelError("AuthorId", "Select Author");

            if (book.TotalCopies <= 0)
                ModelState.AddModelError("TotalCopies", "Enter valid total copies");

            if (ModelState.IsValid)
            {
                _context.Update(book);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Book updated successfully!";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.CategoryList = new SelectList(_context.Categories, "Id", "Name", book.CategoryId);
            ViewBag.AuthorList = new SelectList(_context.Authors, "Id", "Name", book.AuthorId);

            return View(book);
        }

        // DELETE BOOK
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var book = await _context.Books
                .Include(b => b.Author)
                .Include(b => b.Category)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (book == null)
                return NotFound();

            return View(book);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Book model)
        {
            var book = await _context.Books
                .Include(b => b.BorrowRecords)
                .FirstOrDefaultAsync(b => b.Id == model.Id);

            if (book == null)
                return NotFound();

            if (book.BorrowRecords != null && book.BorrowRecords.Any())
            {
                TempData["Error"] = "Cannot delete book with borrow history!";
                return RedirectToAction(nameof(Index));
            }

            _context.Books.Remove(book);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Book deleted successfully!";
            return RedirectToAction(nameof(Index));
        }
    }


}