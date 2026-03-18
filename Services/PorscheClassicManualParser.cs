using System.Text.RegularExpressions;
using PartsCopilot.Models;

namespace PartsCopilot.Services;

/// <summary>
/// Parser for Porsche classic parts manuals (911/912 format, 1965-1969 style).
/// Handles the specific column layout: Pos, Part Number, Description, Remark, Qty, Model.
/// </summary>
public partial class PorscheClassicManualParser : IManualParser
{
    public string ManualType => "porsche-classic";

    public bool CanParse(IReadOnlyList<ManualPage> samplePages)
    {
        // Look for Porsche-specific markers in the first few pages
        return samplePages.Take(10).Any(p =>
            p.RawText.Contains("911", StringComparison.OrdinalIgnoreCase) ||
            p.RawText.Contains("912", StringComparison.OrdinalIgnoreCase) ||
            p.RawText.Contains("Kat 573", StringComparison.OrdinalIgnoreCase));
    }

    public Task<IReadOnlyList<PartRecord>> ParseAsync(
        IReadOnlyList<ManualPage> pages, string manualId,
        IProgress<int>? progress = null, CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            var parts = new List<PartRecord>();
            var partTablePages = pages.Where(p => p.PageType == "part_table").ToList();
            var processed = 0;

            foreach (var page in partTablePages)
            {
                ct.ThrowIfCancellationRequested();

                var rows = ExtractPartRows(page.RawText, page.PageNumber, page.Illustration, manualId);
                parts.AddRange(rows);

                processed++;
                progress?.Report((int)(100.0 * processed / partTablePages.Count));
            }

            return (IReadOnlyList<PartRecord>)parts;
        }, ct);
    }

    private static List<PartRecord> ExtractPartRows(string text, int pageNumber, string? illustration, string manualId)
    {
        var parts = new List<PartRecord>();

        // Match Porsche part number patterns: "XXX XXX XXX XX" or "N XXX XXX XX"
        var matches = PartNumberRegex().Matches(text);

        foreach (Match match in matches)
        {
            var partNumber = match.Value.Trim();
            var normalized = partNumber.Replace(" ", "").Replace("-", "").ToUpperInvariant();

            // Try to extract context around the part number
            var startIdx = match.Index;
            var contextEnd = Math.Min(text.Length, startIdx + 200);
            var context = text[startIdx..contextEnd];

            var description = ExtractDescription(context, partNumber);
            var quantity = ExtractQuantity(context, partNumber);
            var model = ExtractModel(context);
            var remark = ExtractRemark(context);
            var position = ExtractPosition(text[Math.Max(0, startIdx - 50)..contextEnd]);

            if (string.IsNullOrWhiteSpace(description))
                description = "Unknown part";

            var searchText = $"{partNumber} {normalized} {description} {model} {remark}".ToLowerInvariant();

            parts.Add(new PartRecord
            {
                ManualId = manualId,
                Position = position,
                PartNumber = partNumber,
                PartNumberNormalized = normalized,
                Description = description,
                SearchText = searchText,
                Remark = remark,
                Quantity = quantity,
                Model = model,
                Illustration = illustration,
                PageNumber = pageNumber
            });
        }

        return parts;
    }

    private static string? ExtractDescription(string context, string partNumber)
    {
        // The description typically follows the part number
        var idx = context.IndexOf(partNumber, StringComparison.Ordinal);
        if (idx < 0) return null;

        var after = context[(idx + partNumber.Length)..].Trim();

        // Take text until we hit another part number or end of meaningful text
        var descMatch = Regex.Match(after, @"^([A-Za-z][A-Za-z\s\-/,()\.]+)");
        return descMatch.Success ? descMatch.Groups[1].Value.Trim() : null;
    }

    private static string? ExtractQuantity(string context, string partNumber)
    {
        // Skip past the part number itself to avoid picking up its digits
        var partIdx = context.IndexOf(partNumber, StringComparison.Ordinal);
        if (partIdx >= 0)
        {
            context = context[(partIdx + partNumber.Length)..];
        }

        // Try patterns in order of specificity:
        // 1. "Qty: 3" or "Qty 3" or "QTY:3"
        var match = Regex.Match(context, @"\bQty\s*[:.]?\s*(\d+)", RegexOptions.IgnoreCase);
        if (match.Success) return match.Groups[1].Value;

        // 2. Quantity with unit: "2 pcs", "1 set", "4 ea"
        match = Regex.Match(context, @"\b(\d+)\s*(?:pcs?|sets?|ea|each|pieces?|pair)\b", RegexOptions.IgnoreCase);
        if (match.Success) return match.Groups[1].Value;

        // 3. Standalone single/double digit between whitespace (common in tabular rows)
        //    Must not be part of a part number, year, or page number
        match = Regex.Match(context, @"(?<=\s)(\d{1,2})(?=\s+(?:911|912|[A-Z])|\s*$)");
        if (match.Success) return match.Groups[1].Value;

        // 4. Digit at end of line
        match = Regex.Match(context, @"\s(\d{1,2})\s*$");
        if (match.Success) return match.Groups[1].Value;

        return null;
    }

    private static string? ExtractModel(string context)
    {
        var match = Regex.Match(context, @"\b(911\s*[TELSB]*|912)\b", RegexOptions.IgnoreCase);
        return match.Success ? match.Value.Trim() : null;
    }

    private static string? ExtractRemark(string context)
    {
        // Look for year markers like "-68", "69", "(USA)", etc.
        var match = Regex.Match(context, @"(\-\d{2}|\b\d{2}\b|\(USA\)|\(CDN\)|\(J\))");
        return match.Success ? match.Value : null;
    }

    private static string? ExtractPosition(string context)
    {
        var match = Regex.Match(context, @"^\s*(\d{1,3})\s");
        return match.Success ? match.Groups[1].Value : null;
    }

    [GeneratedRegex(@"\b[A-Z0-9]{3}[\s\-]\d{3}[\s\-]\d{3}[\s\-]\d{2}\b|\bN[\s\-]\d{3}[\s\-]\d{3}[\s\-]\d{2}\b|\bPCG[\s\-]\d{3}[\s\-]\d{3}[\s\-]\d{2}\b")]
    private static partial Regex PartNumberRegex();
}
