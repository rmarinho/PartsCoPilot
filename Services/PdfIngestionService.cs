using PartsCopilot.Models;
using PartsCopilot.Services;
using UglyToad.PdfPig;

namespace PartsCopilot.Services;

public class PdfIngestionService : IPdfIngestionService
{
    public Task<IReadOnlyList<ManualPage>> ExtractPagesAsync(string filePath, string manualId, CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            var pages = new List<ManualPage>();

            using var document = PdfDocument.Open(filePath);

            foreach (var page in document.GetPages())
            {
                ct.ThrowIfCancellationRequested();

                var text = string.Join(" ", page.GetWords().Select(w => w.Text));
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
