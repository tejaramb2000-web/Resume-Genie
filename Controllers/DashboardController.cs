using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ResumeTailorApp.Data;
using ResumeTailorApp.Models;

namespace ResumeTailorApp.Controllers
{
    public class DashboardController : Controller
    {
        private readonly AppDbContext _context;

        public DashboardController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
                return RedirectToAction("Login", "Account");

            var vm = new DashboardViewModel
            {
                UserName = HttpContext.Session.GetString("UserName") ?? "User",
                ResumeCount = await _context.Resumes.CountAsync(),
                JobCount = await _context.Jobs.CountAsync(),
                VersionCount = await _context.ResumeVersions.CountAsync(),
                LatestVersions = await _context.ResumeVersions
                    .OrderByDescending(x => x.Id)
                    .Take(5)
                    .ToListAsync()
            };

            return View(vm);
        }
    }
}