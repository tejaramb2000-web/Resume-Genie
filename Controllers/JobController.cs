using Microsoft.AspNetCore.Mvc;
using ResumeTailorApp.Data;
using ResumeTailorApp.Models;

namespace ResumeTailorApp.Controllers
{
    public class JobController : Controller
    {
        private readonly AppDbContext _context;

        public JobController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Create(Job model)
        {
            _context.Jobs.Add(model);
            _context.SaveChanges();

            return RedirectToAction("List");
        }

        public IActionResult List()
        {
            var jobs = _context.Jobs.ToList();
            return View(jobs);
        }
    }
}