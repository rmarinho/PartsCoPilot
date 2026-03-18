using System.Diagnostics;
using PartsCopilot.Models;

namespace PartsCopilot.Services;

/// <summary>
/// Hybrid search: exact part number → description match → model filter → text overlap.
/// </summary>
public class HybridSearchService : ISearchService
{
    private readonly IPartsRepository _repo;

    public HybridSearchService(IPartsRepository repo) => _repo = repo;

    public async Task<SearchResult> SearchAsync(SearchQuery query, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var candidates = new List<SearchCandidate>();
        var normalized = query.UserText.Replace(" ", "").ToUpperInvariant();
        var lower = query.UserText.ToLowerInvariant();

        var parts = await _repo.SearchPartsAsync(query.UserText, query.ManualId, ct: ct);

        foreach (var part in parts)
        {
            var (score, reason) = ScorePart(part, normalized, lower, query);
            if (score > 0)
                candidates.Add(new SearchCandidate(part, score, reason));
        }

        // Apply vehicle context filters
        if (query.Context is { } ctx)
            candidates = ApplyContextFilter(candidates, ctx);

        candidates = candidates
            .OrderByDescending(c => c.Score)
            .Take(50)
            .ToList();

        sw.Stop();
        return new SearchResult(candidates, candidates.Count, sw.Elapsed);
    }

    private static (double Score, string Reason) ScorePart(PartRecord part, string normalizedQuery, string lowerQuery, SearchQuery query)
    {
        // Exact part number match — highest score
        if (part.PartNumberNormalized == normalizedQuery)
            return (1.0, "Exact part number match");

        // Partial part number match
        if (part.PartNumberNormalized.Contains(normalizedQuery) || normalizedQuery.Contains(part.PartNumberNormalized))
            return (0.9, "Partial part number match");

        // Exact description phrase match
        if (part.Description.Contains(query.UserText, StringComparison.OrdinalIgnoreCase))
            return (0.8, "Exact description match");

        // Word-level description match
        var queryWords = lowerQuery.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var matchedWords = queryWords.Count(w => part.SearchText.Contains(w));
        if (matchedWords > 0)
        {
            var wordScore = 0.3 + (0.4 * matchedWords / queryWords.Length);
            return (wordScore, $"Text match ({matchedWords}/{queryWords.Length} words)");
        }

        return (0, "No match");
    }

    private static List<SearchCandidate> ApplyContextFilter(List<SearchCandidate> candidates, VehicleContext ctx)
    {
        return candidates.Where(c =>
        {
            var part = c.Part;

            if (ctx.Model is not null && part.Model is not null &&
                !part.Model.Contains(ctx.Model, StringComparison.OrdinalIgnoreCase))
                return false;

            if (ctx.Year is not null && part.Remark is not null)
            {
                var yearStr = (ctx.Year.Value % 100).ToString();
                if (part.Remark.Contains($"-{yearStr}"))
                    return true; // "up to" this year
                if (part.Remark == yearStr)
                    return true; // "from" this year
            }

            return true;
        }).ToList();
    }
}
