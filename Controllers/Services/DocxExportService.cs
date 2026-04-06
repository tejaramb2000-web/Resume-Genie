using System;
using System.Linq;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace ResumeTailorApp.Services
{
    public class DocxExportService
    {
        public byte[] GenerateResumeDocx(string resumeText)
        {
            using var stream = new MemoryStream();

            using (var wordDoc = WordprocessingDocument.Create(
                stream,
                WordprocessingDocumentType.Document,
                true))
            {
                var mainPart = wordDoc.AddMainDocumentPart();
                mainPart.Document = new Document();
                var body = new Body();

                AddSectionProperties(body);

                var lines = NormalizeLines(resumeText);

                int i = 0;

                while (i < lines.Length)
                {
                    var line = lines[i];

                    if (string.IsNullOrWhiteSpace(line))
                    {
                        body.Append(CreateParagraph(string.Empty, 10.5, false, 60));
                        i++;
                        continue;
                    }

                    // Name
                    if (i == 0)
                    {
                        body.Append(CreateParagraph(line.Trim(), 17, true, 120));
                        i++;
                        continue;
                    }

                    // Section heading
                    if (IsSectionHeading(line))
                    {
                        body.Append(CreateParagraph(line.Trim(), 13, true, 80));
                        i++;
                        continue;
                    }

                    // Role + company/date line
                    if (LooksLikeRoleTitle(line) && i + 1 < lines.Length && LooksLikeCompanyDateLine(lines[i + 1]))
                    {
                        var roleTitle = line.Trim();
                        var companyDateLine = lines[i + 1].Trim();
                        var split = SplitCompanyAndDate(companyDateLine);

                        body.Append(CreateParagraph(roleTitle, 10.5, true, 20));
                        body.Append(CreateCompanyDateTable(split.company, split.dates));

                        i += 2;
                        continue;
                    }

                    // Bullet
                    if (IsBullet(line))
                    {
                        body.Append(CreateBulletParagraph(TrimBullet(line)));
                        i++;
                        continue;
                    }

                    // Category / label line
                    if (line.Contains(":"))
                    {
                        body.Append(CreateLabelValueParagraph(line));
                        i++;
                        continue;
                    }

                    // General text
                    body.Append(CreateParagraph(line.Trim(), 10.5, false, 40));
                    i++;
                }

                mainPart.Document.Append(body);
                mainPart.Document.Save();
            }

            return stream.ToArray();
        }

        private static void AddSectionProperties(Body body)
        {
            body.Append(
                new SectionProperties(
                    new PageSize
                    {
                        Width = 12240,   // Letter
                        Height = 15840
                    },
                    new PageMargin
                    {
                        Top = 720,       // 0.5 inch
                        Right = 720,
                        Bottom = 720,
                        Left = 720,
                        Header = 360,
                        Footer = 360,
                        Gutter = 0
                    }
                )
            );
        }

        private static Paragraph CreateParagraph(string text, double fontSize, bool bold, int spacingAfter)
        {
            var paragraphProperties = new ParagraphProperties(
                new SpacingBetweenLines
                {
                    Line = "240", // close to 1.0–1.1
                    LineRule = LineSpacingRuleValues.Auto,
                    After = spacingAfter.ToString()
                }
            );

            var runProperties = new RunProperties(
                new RunFonts
                {
                    Ascii = "Calibri",
                    HighAnsi = "Calibri"
                },
                new FontSize
                {
                    Val = ((int)(fontSize * 2)).ToString()
                }
            );

            if (bold)
                runProperties.Append(new Bold());

            var run = new Run();
            run.Append(runProperties);
            run.Append(new Text(text ?? string.Empty)
            {
                Space = SpaceProcessingModeValues.Preserve
            });

            var paragraph = new Paragraph();
            paragraph.Append(paragraphProperties);
            paragraph.Append(run);

            return paragraph;
        }

        private static Paragraph CreateBulletParagraph(string text)
        {
            var paragraphProperties = new ParagraphProperties(
                new Indentation
                {
                    Left = "360",
                    Hanging = "180"
                },
                new SpacingBetweenLines
                {
                    Line = "240",
                    LineRule = LineSpacingRuleValues.Auto,
                    After = "35"
                }
            );

            var runProperties = new RunProperties(
                new RunFonts
                {
                    Ascii = "Calibri",
                    HighAnsi = "Calibri"
                },
                new FontSize
                {
                    Val = "21" // 10.5 * 2
                }
            );

            var bulletRun = new Run();
            bulletRun.Append(runProperties.CloneNode(true));
            bulletRun.Append(new Text("• ")
            {
                Space = SpaceProcessingModeValues.Preserve
            });

            var textRun = new Run();
            textRun.Append(runProperties.CloneNode(true));
            textRun.Append(new Text(text ?? string.Empty)
            {
                Space = SpaceProcessingModeValues.Preserve
            });

            var paragraph = new Paragraph();
            paragraph.Append(paragraphProperties);
            paragraph.Append(bulletRun);
            paragraph.Append(textRun);

            return paragraph;
        }

        private static Paragraph CreateLabelValueParagraph(string line)
        {
            int idx = line.IndexOf(':');
            string label = idx >= 0 ? line.Substring(0, idx + 1).Trim() : line.Trim();
            string value = idx >= 0 ? line.Substring(idx + 1).Trim() : string.Empty;

            var paragraphProperties = new ParagraphProperties(
                new SpacingBetweenLines
                {
                    Line = "240",
                    LineRule = LineSpacingRuleValues.Auto,
                    After = "35"
                }
            );

            var boldProps = new RunProperties(
                new RunFonts { Ascii = "Calibri", HighAnsi = "Calibri" },
                new FontSize { Val = "21" },
                new Bold()
            );

            var normalProps = new RunProperties(
                new RunFonts { Ascii = "Calibri", HighAnsi = "Calibri" },
                new FontSize { Val = "21" }
            );

            var paragraph = new Paragraph();
            paragraph.Append(paragraphProperties);

            var labelRun = new Run();
            labelRun.Append(boldProps);
            labelRun.Append(new Text(label)
            {
                Space = SpaceProcessingModeValues.Preserve
            });
            paragraph.Append(labelRun);

            if (!string.IsNullOrWhiteSpace(value))
            {
                var valueRun = new Run();
                valueRun.Append(normalProps);
                valueRun.Append(new Text(" " + value)
                {
                    Space = SpaceProcessingModeValues.Preserve
                });
                paragraph.Append(valueRun);
            }

            return paragraph;
        }

        private static Table CreateCompanyDateTable(string company, string dates)
        {
            var table = new Table();

            var props = new TableProperties(
                new TableWidth { Width = "100%", Type = TableWidthUnitValues.Pct },
                new TableBorders(
                    new TopBorder { Val = BorderValues.None },
                    new BottomBorder { Val = BorderValues.None },
                    new LeftBorder { Val = BorderValues.None },
                    new RightBorder { Val = BorderValues.None },
                    new InsideHorizontalBorder { Val = BorderValues.None },
                    new InsideVerticalBorder { Val = BorderValues.None }
                )
            );

            table.Append(props);

            var row = new TableRow();

            row.Append(
                CreateTextCell(company, true, JustificationValues.Left, "7000"),
                CreateTextCell(dates, false, JustificationValues.Right, "3000")
            );

            table.Append(row);
            return table;
        }

        private static TableCell CreateTextCell(string text, bool bold, JustificationValues justify, string width)
        {
            var paraProps = new ParagraphProperties(
                new Justification { Val = justify },
                new SpacingBetweenLines
                {
                    Line = "240",
                    LineRule = LineSpacingRuleValues.Auto,
                    After = "20"
                }
            );

            var runProps = new RunProperties(
                new RunFonts { Ascii = "Calibri", HighAnsi = "Calibri" },
                new FontSize { Val = "21" }
            );

            if (bold)
                runProps.Append(new Bold());

            var paragraph = new Paragraph();
            paragraph.Append(paraProps);

            var run = new Run();
            run.Append(runProps);
            run.Append(new Text(text ?? string.Empty)
            {
                Space = SpaceProcessingModeValues.Preserve
            });

            paragraph.Append(run);

            var cell = new TableCell(
                new TableCellProperties(
                    new TableCellWidth { Type = TableWidthUnitValues.Dxa, Width = width },
                    new TableCellBorders(
                        new TopBorder { Val = BorderValues.None },
                        new BottomBorder { Val = BorderValues.None },
                        new LeftBorder { Val = BorderValues.None },
                        new RightBorder { Val = BorderValues.None }
                    )
                ),
                paragraph
            );

            return cell;
        }

        private static string[] NormalizeLines(string text)
        {
            return (text ?? string.Empty)
                .Replace("\r\n", "\n")
                .Replace("\r", "\n")
                .Split('\n')
                .Select(x => x?.TrimEnd() ?? string.Empty)
                .ToArray();
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

            var match = Regex.Match(
                trimmed,
                @"^(.*?)(?:\s+[—–-]\s+)([A-Za-z]{3,9}\s+\d{4}\s+[–-]\s+(?:Present|[A-Za-z]{3,9}\s+\d{4}))$"
            );

            if (match.Success)
            {
                return (
                    match.Groups[1].Value.Trim(),
                    match.Groups[2].Value.Trim()
                );
            }

            string[] separators = { " — ", " – ", " - " };

            foreach (var sep in separators)
            {
                int idx = trimmed.LastIndexOf(sep, StringComparison.Ordinal);
                if (idx > 0)
                {
                    return (
                        trimmed.Substring(0, idx).Trim(),
                        trimmed.Substring(idx + sep.Length).Trim()
                    );
                }
            }

            return (trimmed, string.Empty);
        }
    }
}