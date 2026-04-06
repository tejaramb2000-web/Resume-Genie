using System;
using System.Collections.Generic;
using System.Linq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ResumeTailorApp.Services
{
    public class PdfExportService
    {
        public byte[] GenerateResumePdf(string resumeText, string title = "Tailored Resume")
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var lines = NormalizeLines(resumeText);

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.Letter);
                    page.Margin(40);

                    page.DefaultTextStyle(x => x
                        .FontFamily("Calibri")
                        .FontSize(10.5f)
                        .LineHeight(1.05f)
                        .FontColor(Colors.Black));

                    page.Content().Column(column =>
                    {
                        column.Spacing(4);

                        int i = 0;

                        while (i < lines.Count)
                        {
                            var line = lines[i];

                            if (string.IsNullOrWhiteSpace(line))
                            {
                                i++;
                                continue;
                            }

                            if (i == 0)
                            {
                                column.Item().Text(line.Trim())
                                    .FontFamily("Calibri")
                                    .FontSize(17)
                                    .Bold()
                                    .FontColor(Colors.Black);

                                i++;
                                continue;
                            }

                            if (IsSectionHeading(line))
                            {
                                column.Item()
                                    .PaddingTop(6)
                                    .PaddingBottom(2)
                                    .Text(line.Trim())
                                    .FontFamily("Calibri")
                                    .FontSize(13)
                                    .Bold()
                                    .FontColor(Colors.Black);

                                i++;
                                continue;
                            }

                            if (LooksLikeRoleTitle(line) && i + 1 < lines.Count && LooksLikeCompanyDateLine(lines[i + 1]))
                            {
                                var roleTitle = line.Trim();
                                var companyDateLine = lines[i + 1].Trim();
                                var split = SplitCompanyAndDate(companyDateLine);

                                column.Item().PaddingTop(4).Column(entry =>
                                {
                                    entry.Spacing(1);

                                    entry.Item().Text(roleTitle)
                                        .FontFamily("Calibri")
                                        .FontSize(10.5f)
                                        .Bold()
                                        .FontColor(Colors.Black);

                                    entry.Item().Row(row =>
                                    {
                                        row.RelativeItem().Text(split.company)
                                            .FontFamily("Calibri")
                                            .FontSize(10.5f)
                                            .Bold();

                                        row.ConstantItem(180).AlignRight().Text(split.dates ?? string.Empty);
                                    });
                                });

                                i += 2;
                                continue;
                            }

                            if (IsBullet(line))
                            {
                                column.Item().PaddingLeft(10).Text(text =>
                                {
                                    text.DefaultTextStyle(x => x
                                        .FontFamily("Calibri")
                                        .FontSize(10.5f)
                                        .LineHeight(1.05f));

                                    text.Span("• ");
                                    text.Span(TrimBullet(line));
                                });

                                i++;
                                continue;
                            }

                            if (line.Contains(":"))
                            {
                                var idx = line.IndexOf(':');
                                var label = line.Substring(0, idx + 1).Trim();
                                var value = line.Substring(idx + 1).Trim();

                                column.Item().Text(text =>
                                {
                                    text.DefaultTextStyle(x => x
                                        .FontFamily("Calibri")
                                        .FontSize(10.5f)
                                        .LineHeight(1.05f));

                                    text.Span(label).Bold();

                                    if (!string.IsNullOrWhiteSpace(value))
                                        text.Span(" " + value);
                                });

                                i++;
                                continue;
                            }

                            column.Item().Text(line.Trim())
                                .FontFamily("Calibri")
                                .FontSize(10.5f)
                                .LineHeight(1.05f);

                            i++;
                        }
                    });

                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span("Page ");
                        text.CurrentPageNumber();
                        text.Span(" of ");
                        text.TotalPages();
                    });
                });
            }).GeneratePdf();
        }

        private static List<string> NormalizeLines(string text)
        {
            return (text ?? string.Empty)
                .Replace("\r\n", "\n")
                .Replace("\r", "\n")
                .Split('\n')
                .Select(x => x?.TrimEnd() ?? string.Empty)
                .ToList();
        }

        private static bool IsSectionHeading(string line)
        {
            var headings = new[]
            {
                "PROFESSIONAL SUMMARY",
                "CORE COMPETENCIES",
                "TECHNICAL SKILLS",
                "PROFESSIONAL EXPERIENCE",
                "PROJECTS",
                "EDUCATION",
                "CERTIFICATIONS",
                "EXPOSURE / FAMILIAR WITH",
                "FAMILIAR WITH"
            };

            return headings.Contains(line.Trim(), StringComparer.OrdinalIgnoreCase);
        }

        private static bool IsBullet(string line)
        {
            var trimmed = line.TrimStart();
            return trimmed.StartsWith("- ") || trimmed.StartsWith("• ") || trimmed.StartsWith("* ");
        }

        private static string TrimBullet(string line)
        {
            var trimmed = line.TrimStart();

            if (trimmed.StartsWith("- ") || trimmed.StartsWith("• ") || trimmed.StartsWith("* "))
                return trimmed.Substring(2).Trim();

            return trimmed;
        }

        private static bool LooksLikeRoleTitle(string line)
        {
            var trimmed = line.Trim();

            if (string.IsNullOrWhiteSpace(trimmed))
                return false;

            if (IsSectionHeading(trimmed))
                return false;

            if (IsBullet(trimmed))
                return false;

            if (trimmed.Contains("|") || trimmed.Contains("@"))
                return false;

            if (trimmed.Length > 70)
                return false;

            return true;
        }

        private static bool LooksLikeCompanyDateLine(string line)
        {
            var trimmed = line.Trim();
            return trimmed.Contains("—") || trimmed.Contains("–") || trimmed.Contains("-");
        }

        private static (string company, string dates) SplitCompanyAndDate(string line)
        {
            var trimmed = line.Trim();

            string[] separators = { " — ", " – ", " - " };

            foreach (var sep in separators)
            {
                var idx = trimmed.LastIndexOf(sep, StringComparison.Ordinal);
                if (idx > 0)
                {
                    var company = trimmed.Substring(0, idx).Trim();
                    var dates = trimmed.Substring(idx + sep.Length).Trim();
                    return (company, dates);
                }
            }

            return (trimmed, string.Empty);
        }
    }
}