using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace LibraryManagementSystem.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext context;

        public HomeController(AppDbContext context)
        {
            this.context = context;
        }

        public async Task<IActionResult> Index()
        {
            var now = DateTime.Now;

            var model = new DashboardViewModel
            {
                TotalBooks = await context.Books.CountAsync(),
                IssuedBooks = await context.BorrowRecords.CountAsync(b => b.ReturnedOn == null),
                OverdueBooks = await context.BorrowRecords.CountAsync(b => b.ReturnedOn == null && b.DueDate < now),
                TotalMembers = await context.Members.CountAsync(),

                ActiveMemberships = await context.Memberships
                    .CountAsync(m => m.IsActive && m.EndDate > now),

                ExpiredMemberships = await context.Memberships
                    .CountAsync(m => m.EndDate <= now || !m.IsActive)
            };

            var recentBorrows = await context.BorrowRecords
                .AsNoTracking()
                .Include(b => b.Book)
                .Include(b => b.Member)
                .OrderByDescending(b => b.IssuedOn)
                .Take(5)
                .ToListAsync();

            ViewBag.RecentBorrows = recentBorrows;

            return View(model);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
        public IActionResult Contact()
        {
            return View();
        }
    }
}