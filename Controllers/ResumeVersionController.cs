using Microsoft.AspNetCore.Mvc;
using ResumeTailorApp.Data;
using ResumeTailorApp.Services;
using System.Text;

namespace ResumeTailorApp.Controllers
{
    public class ResumeVersionController : Controller
    {
        private readonly AppDbContext _context;
        private readonly PdfExportService _pdfExportService;
        private readonly DocxExportService _docxExportService;

        public ResumeVersionController(
            AppDbContext context,
            PdfExportService pdfExportService,
            DocxExportService docxExportService)
        {
            _context = context;
            _pdfExportService = pdfExportService;
            _docxExportService = docxExportService;
        }

        public IActionResult List()
        {
            var versions = _context.ResumeVersions
                .OrderByDescending(v => v.CreatedAt)
                .ToList();

            return View(versions);
        }

        public IActionResult DownloadTxt(int id)
        {
            var version = _context.ResumeVersions.FirstOrDefault(v => v.Id == id);

            if (version == null)
                return NotFound();

            var fileName = $"{MakeSafeFileName(version.JobTitle)}.txt";
            var content = version.Content ?? string.Empty;
            var bytes = Encoding.UTF8.GetBytes(content);

            return File(bytes, "text/plain", fileName);
        }

        public IActionResult DownloadDocx(int id)
        {
            var version = _context.ResumeVersions.FirstOrDefault(v => v.Id == id);

            if (version == null)
                return NotFound();

            var fileName = $"{MakeSafeFileName(version.JobTitle)}.docx";
            var content = version.Content ?? string.Empty;
            var bytes = _docxExportService.GenerateResumeDocx(content);

            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                fileName);
        }

        public IActionResult DownloadPdf(int id)
        {
            var version = _context.ResumeVersions.FirstOrDefault(v => v.Id == id);

            if (version == null)
                return NotFound();

            var fileName = $"{MakeSafeFileName(version.JobTitle)}.pdf";
            var content = version.Content ?? string.Empty;
            var title = string.IsNullOrWhiteSpace(version.JobTitle) ? "Resume" : version.JobTitle;
            var bytes = _pdfExportService.GenerateResumePdf(content, title);

            return File(bytes, "application/pdf", fileName);
        }

        private static string MakeSafeFileName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "Resume";

            foreach (var ch in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(ch, '_');
            }

            return name.Replace(' ', '_').Trim('_');
        }
    }
}