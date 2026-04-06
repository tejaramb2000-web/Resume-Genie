using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace ResumeTailorApp.Services
{
    public class ResumeFormatterService
    {
        public string FormatResumeText(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            var text = input
                .Replace("\r\n", "\n")
                .Replace("\r", "\n")
                .Trim();

            // Normalize spaces
            text = Regex.Replace(text, @"[ \t]+", " ");
            text = Regex.Replace(text, @"\n{3,}", "\n\n");

            // Remove common model leakage / unwanted labels
            text = RemoveNoise(text);

            // Normalize headings
            text = NormalizeHeadings(text);

            // Normalize bullets
            text = NormalizeBullets(text);

            // Normalize company/role/date lines
            text = NormalizeRoleLines(text);

            // Remove duplicate sections if model repeated them
            text = RemoveDuplicateSections(text);

            // Final cleanup
            text = Regex.Replace(text, @"[ \t]+\n", "\n");
            text = Regex.Replace(text, @"\n{3,}", "\n\n");

            return text.Trim();
        }

        private string RemoveNoise(string text)
        {
            string[] noisePatterns =
            {
                @"(?im)^here is the tailored resume:\s*$",
                @"(?im)^here is the rewritten resume:\s*$",
                @"(?im)^final resume:\s*$",
                @"(?im)^alignment strategy:?\s*$",
                @"(?im)^strict rules:?\s*$",
                @"(?im)^rules:?\s*$",
                @"(?im)^\(8-15 lines\)\s*$",
                @"(?im)^\(8 to 15 lines\)\s*$",
                @"(?im)^\(10-12 bullets\)\s*$",
                @"(?im)^\(10 to 12 bullets per role\)\s*$",
                @"(?im)^\(2 bullets\)\s*$"
            };

            foreach (var pattern in noisePatterns)
            {
                text = Regex.Replace(text, pattern, "");
            }

            text = text.Replace("```", "");
            return text;
        }

        private string NormalizeHeadings(string text)
        {
            string[] majorHeadings =
            {
                "PROFESSIONAL SUMMARY",
                "CORE COMPETENCIES",
                "TECHNICAL SKILLS",
                "PROFESSIONAL EXPERIENCE",
                "PROJECTS",
                "EDUCATION",
                "CERTIFICATIONS"
            };

            string[] skillLabels =
            {
                "Programming Languages:",
                "Frameworks / Platforms:",
                "Cloud / Infrastructure:",
                "Databases:",
                "Data / ETL / Analytics:",
                "DevOps / Tools:",
                "Architecture / Methodologies:",
                "Testing / Monitoring / Security:",
                "Domain Knowledge:"
            };

            foreach (var heading in majorHeadings.OrderByDescending(h => h.Length))
            {
                text = Regex.Replace(
                    text,
                    $@"(?im)^\s*{Regex.Escape(heading)}\s*$",
                    $"\n\n{heading}\n");
            }

            foreach (var label in skillLabels.OrderByDescending(h => h.Length))
            {
                text = Regex.Replace(
                    text,
                    $@"(?im)^\s*{Regex.Escape(label)}\s*",
                    $"\n{label} ");
            }

            // Handle cases where headings are stuck to content
            text = Regex.Replace(text, @"(?i)(PROFESSIONAL SUMMARY)(\S)", "$1\n$2");
            text = Regex.Replace(text, @"(?i)(TECHNICAL SKILLS)(\S)", "$1\n$2");
            text = Regex.Replace(text, @"(?i)(PROFESSIONAL EXPERIENCE)(\S)", "$1\n$2");
            text = Regex.Replace(text, @"(?i)(EDUCATION)(\S)", "$1\n$2");
            text = Regex.Replace(text, @"(?i)(CERTIFICATIONS)(\S)", "$1\n$2");

            return text;
        }

        private string NormalizeBullets(string text)
        {
            // Convert bullet symbols to standard dash bullets
            text = Regex.Replace(text, @"(?m)^\s*[•\*]\s+", "- ");
            text = Regex.Replace(text, @"(?<!\n)- ", "\n- ");

            // Prevent repeated blank lines between bullets
            text = Regex.Replace(text, @"\n{2,}- ", "\n- ");

            return text;
        }

        private string NormalizeRoleLines(string text)
        {
            // Company | Role | Dates
            text = Regex.Replace(
                text,
                @"(?m)^\s*(.+?)\s*\|\s*(.+?)\s*\|\s*(.+?)\s*$",
                m => $"{m.Groups[1].Value.Trim()} | {m.Groups[2].Value.Trim()} | {m.Groups[3].Value.Trim()}");

            // If model outputs "Company – Role, Dates" or "Company - Role, Dates"
            text = Regex.Replace(
                text,
                @"(?m)^\s*([A-Za-z0-9&.,()\/' \-]+)\s+[–—-]\s+([A-Za-z0-9&.,()\/' \-]+),\s*([A-Za-z]{3,9}\s+\d{4}\s*[–—-]\s*(?:Present|[A-Za-z]{3,9}\s+\d{4}))\s*$",
                "$1 | $2 | $3");

            return text;
        }

        private string RemoveDuplicateSections(string text)
        {
            string[] duplicatedSectionPatterns =
            {
                @"(?is)(PROFESSIONAL SUMMARY)\s*\1",
                @"(?is)(TECHNICAL SKILLS)\s*\1",
                @"(?is)(PROFESSIONAL EXPERIENCE)\s*\1",
                @"(?is)(EDUCATION)\s*\1",
                @"(?is)(CERTIFICATIONS)\s*\1"
            };

            foreach (var pattern in duplicatedSectionPatterns)
            {
                text = Regex.Replace(text, pattern, "$1");
            }

            return text;
        }
    }
}