using PartsCopilot.Models;
using PartsCopilot.Services;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace PartsCopilot.Services;

public class PdfIngestionService : IPdfIngestionService
{
    // Y-coordinate tolerance for grouping words into lines (PDF points)
    private const double LineGroupingTolerance = 3.0;

    public Task<IReadOnlyList<ManualPage>> ExtractPagesAsync(string filePath, string manualId, CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            var pages = new List<ManualPage>();

            using var document = PdfDocument.Open(filePath);

            foreach (var page in document.GetPages())
            {
                ct.ThrowIfCancellationRequested();

                var text = ExtractTextWithLines(page);
                if (string.IsNullOrWhiteSpace(text))
                    continue;

                var illustration = DetectIllustration(text);
                var pageType = ClassifyPage(text, illustration);

                pages.Add(new ManualPage
                {
                    ManualId = manualId,
                    PageNumber = page.Number,
                    RawText = text,
                    Illustration = illustration,
                    PageType = pageType
                });
            }

            return (IReadOnlyList<ManualPage>)pages;
        }, ct);
    }

    /// <summary>
    /// Reconstructs line-separated text from PdfPig word positions.
    /// Groups words by Y baseline into lines, sorts left-to-right within lines.
    /// </summary>
    private static string ExtractTextWithLines(UglyToad.PdfPig.Content.Page page)
    {
        var words = page.GetWords().ToList();
        if (words.Count == 0)
            return string.Empty;

        // Group words by Y baseline (bottom of bounding box) with tolerance.
        // PDF Y axis: 0 = bottom of page, increasing upward.
        var lines = new List<(double Y, List<Word> Words)>();

        foreach (var word in words)
        {
            var baseline = word.BoundingBox.Bottom;
            var matched = false;

            for (int i = 0; i < lines.Count; i++)
            {
                if (Math.Abs(lines[i].Y - baseline) <= LineGroupingTolerance)
                {
                    lines[i].Words.Add(word);
                    matched = true;
                    break;
                }
            }

            if (!matched)
                lines.Add((baseline, new List<Word> { word }));
        }

        // Sort lines top-to-bottom (highest Y first), words left-to-right within each line
        lines.Sort((a, b) => b.Y.CompareTo(a.Y));

        var sb = new System.Text.StringBuilder();
        foreach (var line in lines)
        {
            line.Words.Sort((a, b) => a.BoundingBox.Left.CompareTo(b.BoundingBox.Left));
            sb.AppendLine(string.Join(" ", line.Words.Select(w => w.Text)));
        }

        return sb.ToString();
    }

    private static string? DetectIllustration(string text)
    {
        // Look for pattern like "Illustration: 001-00" or "Illustration: 101-05"
        var match = System.Text.RegularExpressions.Regex.Match(
            text, @"Illustration:\s*(\d{3}-\d{2})", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value : null;
    }

    private static string ClassifyPage(string text, string? illustration)
    {
        var upper = text.ToUpperInvariant();

        if (upper.Contains("SUMMARY TYPES") || upper.Contains("SUMMARY ENGINES") || upper.Contains("SUMM.TRANSMISS"))
            return "summary";
        if (upper.Contains("LEGENDS") || upper.Contains("NOTICES"))
            return "legend";
        if (upper.Contains("COUNTRY-EQIPM") || upper.Contains("EQUIPMENT BY COUNTRY"))
            return "country";
        if (illustration is not null && upper.Contains("PART NUMBER") && upper.Contains("DESCRIPTION"))
            return "part_table";
        if (illustration is not null)
            return "illustration";

        return "other";
    }
}
