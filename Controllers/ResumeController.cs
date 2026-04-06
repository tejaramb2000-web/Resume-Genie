using Microsoft.AspNetCore.Mvc;
using ResumeTailorApp.Data;
using ResumeTailorApp.Models;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Text;
using DocumentFormat.OpenXml.Packaging;
using UglyToad.PdfPig;

namespace ResumeTailorApp.Controllers
{
    public class ResumeController : Controller
    {
        private readonly AppDbContext _context;

        public ResumeController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Create()
        {
            return View();
        }

       [HttpPost]
public async Task<IActionResult> Create(Resume model, IFormFile file)
{
    if (file != null && file.Length > 0)
    {
        var extension = Path.GetExtension(file.FileName).ToLower();

        if (extension == ".txt")
        {
            using var reader = new StreamReader(file.OpenReadStream());
            model.Content = await reader.ReadToEndAsync();
            model.FileName = file.FileName;
        }
        else if (extension == ".docx")
        {
            using var stream = file.OpenReadStream();
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            using var wordDoc = DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Open(memoryStream, false);
            var body = wordDoc.MainDocumentPart?.Document?.Body;

            if (body != null)
            {
                model.Content = body?.InnerText ?? "";
            }

            model.FileName = file.FileName;
        }
        else if (extension == ".pdf")
        {
            using var stream = file.OpenReadStream();
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            using var pdf = PdfDocument.Open(memoryStream);
            var text = new StringBuilder();

            foreach (var page in pdf.GetPages())
            {
                text.AppendLine(page.Text);
            }

            model.Content = text.ToString();
            model.FileName = file.FileName;
        }
        else
        {
            ModelState.AddModelError("", "Only .txt, .docx, and .pdf files are supported right now.");
            return View(model);
        }

        if (string.IsNullOrWhiteSpace(model.Title))
        {
            model.Title = Path.GetFileNameWithoutExtension(file.FileName);
        }
    }

    if (string.IsNullOrWhiteSpace(model.Title))
    {
        ModelState.AddModelError("", "Please enter a title or upload a file.");
        return View(model);
    }

    _context.Resumes.Add(model);
    _context.SaveChanges();

    return RedirectToAction("List");
}

        public IActionResult List()
        {
            var resumes = _context.Resumes.ToList();
            return View(resumes);
        }
    }
}