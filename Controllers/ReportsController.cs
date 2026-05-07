using LibraryManagementSystem.Data;
using LibraryManagementSystem.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace LibraryManagementSystem.Controllers
{
    public class ReportsController : Controller
    {
        private readonly AppDbContext _context;

        public ReportsController(AppDbContext context)
        {
            _context = context;
        }

        // OVERDUE BOOKS REPORT
        public async Task<IActionResult> Overdue()
        {
            var overdueList = await _context.BorrowRecords
                .Include(b => b.Book)
                .Include(b => b.Member)
                .Where(b => b.ReturnedOn == null && b.DueDate < DateTime.Now)
                .OrderByDescending(b => b.DueDate)
                .ToListAsync();

            return View(overdueList);
        }

        // TOP BORROWERS REPORT
        public async Task<IActionResult> TopBorrowers()
        {
            var data = await _context.BorrowRecords
                .Include(b => b.Member)
                .GroupBy(b => new { b.MemberId, b.Member.Name })
                .Select(g => new TopBorrowerViewModel
                {
                    MemberName = g.Key.Name,
                    TotalBooks = g.Count()
                })
                .OrderByDescending(x => x.TotalBooks)
                .Take(10)
                .ToListAsync();

            return View(data);
        }
    }
}