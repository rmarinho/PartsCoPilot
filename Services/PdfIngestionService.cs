using PartsCopilot.Models;
using PartsCopilot.Services;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace PartsCopilot.Services;

public class PdfIngestionService : IPdfIngestionService
{
    private const double LineGroupingTolerance = 3.0;

    private readonly IPdfPageRenderer? _renderer;

    public PdfIngestionService() { }

    public PdfIngestionService(IPdfPageRenderer? renderer)
    {
        _renderer = renderer;
    }

    public Task<IReadOnlyList<ManualPage>> ExtractPagesAsync(string filePath, string manualId, CancellationToken ct = default)
    {
        return Task.Run(async () =>
        {
            var pages = new List<ManualPage>();

            PdfDocument document;
            try
            {
                document = PdfDocument.Open(filePath);
            }
            catch (FileNotFoundException)
            {
                throw new InvalidOperationException($"PDF file not found: {filePath}");
            }
            catch (UnauthorizedAccessException)
            {
                throw new InvalidOperationException($"Cannot access PDF file (permission denied): {filePath}");
            }
            catch (Exception ex) when (ex.Message.Contains("password") || ex.Message.Contains("encrypted"))
            {
                throw new InvalidOperationException("PDF file is password-protected. Please remove the password and try again.");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to open PDF file: {ex.Message}");
            }

            using (document)

                foreach (var page in document.GetPages())
                {
                    ct.ThrowIfCancellationRequested();

                    var text = ExtractTextWithLines(page);
                    if (string.IsNullOrWhiteSpace(text))
                        continue;

                    var illustration = DetectIllustration(text);
                    var pageType = ClassifyPage(text, illustration);

                    // Try to render the page to an image for visual display
                    byte[]? imageData = null;
                    if (_renderer is not null && _renderer.IsSupported)
                    {
                        imageData = await _renderer.RenderPageToImageAsync(filePath, page.Number, ct: ct);
                    }

                    pages.Add(new ManualPage
                    {
                        ManualId = manualId,
                        PageNumber = page.Number,
                        RawText = text,
                        Illustration = illustration,
                        PageType = pageType,
                        ImageData = imageData
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
