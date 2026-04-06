using Microsoft.AspNetCore.Mvc;
using ResumeTailorApp.Data;
using ResumeTailorApp.Models;
using ResumeTailorApp.Services;
using System.Net;

namespace ResumeTailorApp.Controllers
{
    public class MatchController : Controller
    {
        private readonly AppDbContext _context;
        private readonly KeywordService _keywordService;
        private readonly OllamaService _ollamaService;
        private readonly ResumeFormatterService _resumeFormatterService;
        private readonly PdfExportService _pdfExportService;
        private readonly DocxExportService _docxExportService;

        public MatchController(
            AppDbContext context,
            KeywordService keywordService,
            OllamaService ollamaService,
            ResumeFormatterService resumeFormatterService,
            PdfExportService pdfExportService,
            DocxExportService docxExportService)
        {
            _context = context;
            _keywordService = keywordService;
            _ollamaService = ollamaService;
            _resumeFormatterService = resumeFormatterService;
            _pdfExportService = pdfExportService;
            _docxExportService = docxExportService;
        }

        public IActionResult Index()
        {
            ViewBag.Resumes = _context.Resumes.ToList();
            ViewBag.Jobs = _context.Jobs.ToList();
            return View();
        }

        [HttpPost]
        public IActionResult Analyze(int resumeId, int jobId)
        {
            var resume = _context.Resumes.Find(resumeId);
            var job = _context.Jobs.Find(jobId);

            if (resume == null || job == null)
                return NotFound();

            var resumeWords = _keywordService.ExtractKeywords(resume.Content);
            var jobWords = _keywordService.ExtractKeywords(job.Description);

            var matched = jobWords
                .Where(j => resumeWords.Any(r => r.Contains(j) || j.Contains(r)))
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            var missing = jobWords
                .Where(j => !resumeWords.Any(r => r.Contains(j) || j.Contains(r)))
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            int score = jobWords.Count == 0
                ? 0
                : (matched.Count * 100) / jobWords.Count;

            var suggestions = missing
                .Take(8)
                .Select(m => $"Add '{m}' only if it reflects real experience.")
                .ToList();

            var result = new MatchResult
            {
                ResumeId = resume.Id,
                JobId = job.Id,
                ResumeTitle = resume.Title,
                JobTitle = job.Title,
                Score = score,
                MatchedKeywords = matched,
                MissingKeywords = missing.Take(40).ToList(),
                Suggestions = suggestions
            };

            return View("Result", result);
        }

        [HttpPost]
        public async Task<IActionResult> Rewrite(int resumeId, int jobId)
        {
            var resume = _context.Resumes.Find(resumeId);
            var job = _context.Jobs.Find(jobId);

            if (resume == null || job == null)
                return NotFound();

            try
            {
                Console.WriteLine("Running single-call resume rewrite...");

                var cleanResume = NormalizeInputText(resume.Content);
                var cleanJob = NormalizeInputText(job.Description);

                // 🔥 Trim to avoid timeout
                var shortResume = TrimText(cleanResume, 5000);
                var shortJob = TrimText(cleanJob, 2500);

                var prompt = BuildPrompt(shortResume, shortJob);

                var rewrittenRaw = await _ollamaService.RewriteResumeAsync(prompt);

                rewrittenRaw = CleanResumeOutput(rewrittenRaw);

                var rewritten = _resumeFormatterService.FormatResumeText(rewrittenRaw);

                var version = new ResumeVersion
                {
                    ResumeId = resume.Id,
                    JobTitle = job.Title,
                    Content = rewritten
                };

                _context.ResumeVersions.Add(version);
                _context.SaveChanges();

                var diff = BuildHighlightedDiff(resume.Content, rewritten);

                var result = new RewriteResult
                {
                    ResumeTitle = resume.Title,
                    JobTitle = job.Title,
                    OriginalContent = resume.Content,
                    RewrittenContent = rewritten,
                    OriginalContentText = resume.Content,
                    RewrittenContentText = rewritten,
                    OriginalContentHtml = diff.originalHtml,
                    RewrittenContentHtml = diff.rewrittenHtml
                };

                return View("RewriteResult", result);
            }
            catch (Exception ex)
            {
                throw new Exception($"Resume rewrite failed: {ex.Message}", ex);
            }
        }

        private string BuildPrompt(string resumeContent, string jobDescription)
        {
            return $"""
You are a top-tier U.S. executive resume writer specializing in highly competitive IT, software, cloud, data, DevOps, and enterprise technology roles.

Your task is to rewrite the candidate's resume into a premium, ATS-optimized, recruiter-ready U.S. IT resume tailored to the job description, while remaining fully truthful and defensible.

CORE OBJECTIVE:
Produce a resume that can compete for top-tier U.S. IT roles in large enterprises, consulting firms, product companies, and global MNCs.

NON-NEGOTIABLE TRUTH RULES:
1. Do NOT invent companies, dates, job titles, education, certifications or achievements.
2. claim hands-on experience with tools or technologies not present in the base resume.
3. fabricate production experience.
4. For missing skills in the job description:
   - Align them under Experience 
   - Only position them via transferable skills, adjacent technologies, or conceptual familiarity
5. All content must be defensible in an interview.

OUTPUT REQUIREMENTS:
- Plain text only
- No markdown or code blocks
- Clean U.S. resume formatting

STRUCTURE:

FULL NAME
City, State | Phone | Email | LinkedIn | GitHub

PROFESSIONAL SUMMARY
- 8 to 15 lines
- Tailored to the job description
- Highlight strongest real strengths
- Use job description terminology where truthful
- Emphasize transferable expertise for missing skills
- Avoid generic buzzwords

CORE COMPETENCIES
Use grouped categories:
Programming Languages:
Frameworks / Platforms:
Cloud / Infrastructure:
Databases:
Data / ETL / Analytics:
DevOps / Tools:
Architecture / Methodologies:
Testing / Monitoring / Security:
Domain Knowledge:

Rules:
- Include only supported or closely related skills
- Prioritize job-relevant technologies
- Avoid keyword stuffing

PROFESSIONAL EXPERIENCE

For each role:

JOB TITLE
COMPANY NAME — LOCATION
DATES

Bullet rules:
- 10 to 12 bullets per role depending on actual experience depth (use fewer only if clearly limited, but prioritize depth)
- Each bullet should be 1 to 2 lines (avoid single-line only and avoid long paragraphs)
- Each bullet must add new information (no repetition across bullets)
- Use strong action verbs (engineered, implemented, optimized, designed, automated, resolved, enhanced)
- Focus on impact, delivery, systems, scale, and outcomes
- Use metrics only when supported
- Integrate technologies naturally
- Align with job description wording where truthful
- Expand depth using system behavior, problem-solving, environment context (Dev/UAT/Prod), and collaboration
- Invent tools, technologies, or responsibilities if needed 

ALIGNMENT STRATEGY:
- Reuse job description keywords where applicable
- Map similar tools (e.g., .NET → backend services, SQL → data pipelines)
- Highlight transferable engineering patterns
- Emphasize architecture, performance, integration, scalability

PROJECTS (if applicable)
- Only include if supported
- 5 to 8 bullets per project

EDUCATION

CERTIFICATIONS (only if supported)

OPTIONAL SECTION (VERY IMPORTANT):

EXPOSURE / FAMILIAR WITH
- Include key job description tools NOT present in experience
- Only include if logically connected to candidate background
- Do NOT present as hands-on experience

QUALITY BAR:
- Must read like a senior-level U.S. IT resume
- ATS optimized
- Recruiter-friendly in under 10 seconds
- Strong alignment with false claims

BASE RESUME:
{resumeContent}

TARGET JOB DESCRIPTION:
{jobDescription}
""";
        }

        private string NormalizeInputText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            var cleaned = text.Replace("\r\n", "\n").Trim();

            while (cleaned.Contains("\n\n\n"))
                cleaned = cleaned.Replace("\n\n\n", "\n\n");

            return cleaned;
        }

        private string TrimText(string text, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            return text.Length <= maxLength ? text : text.Substring(0, maxLength);
        }

        private string CleanResumeOutput(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            return text
                .Replace("```", "")
                .Replace("ALIGNMENT STRATEGY", "")
                .Replace("FINAL RESUME:", "")
                .Trim();
        }

        private (string originalHtml, string rewrittenHtml) BuildHighlightedDiff(string originalText, string rewrittenText)
        {
            var original = WebUtility.HtmlEncode(originalText);
            var rewritten = WebUtility.HtmlEncode(rewrittenText);

            return ($"<pre>{original}</pre>", $"<pre>{rewritten}</pre>");
        }
    }
}