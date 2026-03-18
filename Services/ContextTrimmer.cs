using Microsoft.Extensions.Logging;
using PartsCopilot.Models;

namespace PartsCopilot.Services;

/// <summary>
/// Configuration for context window budget management.
/// </summary>
public sealed class ContextBudget
{
    /// <summary>Maximum tokens allocated for candidate context. Default: 4000.</summary>
    public int MaxContextTokens { get; init; } = 4000;

    /// <summary>
    /// Maximum characters for an individual candidate description before truncation.
    /// Default: 200.
    /// </summary>
    public int MaxDescriptionLength { get; init; } = 200;

    /// <summary>Maximum tokens allocated for page snippets. Default: 1000.</summary>
    public int MaxSnippetTokens { get; init; } = 1000;
}

/// <summary>
/// Trims search candidates and page snippets to fit within a token budget,
/// prioritizing high-relevance results.
/// </summary>
public sealed class ContextTrimmer : IContextTrimmer
{
    private readonly ITokenEstimator _estimator;
    private readonly ILogger<ContextTrimmer>? _logger;

    public ContextTrimmer(ITokenEstimator estimator, ILogger<ContextTrimmer>? logger = null)
    {
        _estimator = estimator ?? throw new ArgumentNullException(nameof(estimator));
        _logger = logger;
    }

    /// <summary>
    /// Trims candidates and snippets to fit within the configured budget.
    /// Candidates are sorted by score descending; highest-relevance results are preserved.
    /// </summary>
    public TrimmedContext TrimToFit(
        IReadOnlyList<SearchCandidate> candidates,
        IReadOnlyList<string> snippets,
        ContextBudget budget)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(snippets);
        ArgumentNullException.ThrowIfNull(budget);

        var trimmedCandidates = TrimCandidates(candidates, budget);
        var trimmedSnippets = TrimSnippets(snippets, budget);

        return new TrimmedContext(trimmedCandidates, trimmedSnippets);
    }

    private List<SearchCandidate> TrimCandidates(
        IReadOnlyList<SearchCandidate> candidates, ContextBudget budget)
    {
        // Sort by score descending — highest relevance first
        var sorted = candidates.OrderByDescending(c => c.Score).ToList();
        var result = new List<SearchCandidate>();
        int usedTokens = 0;

        for (int i = 0; i < sorted.Count; i++)
        {
            var candidate = sorted[i];
            var line = FormatCandidateLine(candidate, i + 1);
            var lineTokens = _estimator.EstimateTokens(line);

            if (usedTokens + lineTokens > budget.MaxContextTokens)
            {
                // Try truncating the description to fit
                var truncated = TruncateCandidate(candidate, budget.MaxDescriptionLength);
                var truncatedLine = FormatCandidateLine(truncated, i + 1);
                var truncatedTokens = _estimator.EstimateTokens(truncatedLine);

                if (usedTokens + truncatedTokens > budget.MaxContextTokens)
                {
                    // Can't fit even truncated — stop adding candidates
                    int dropped = sorted.Count - i;
                    _logger?.LogInformation(
                        "Context trimming: dropped {Dropped} low-relevance candidates (budget {Budget} tokens, used {Used}).",
                        dropped, budget.MaxContextTokens, usedTokens);
                    break;
                }

                result.Add(truncated);
                usedTokens += truncatedTokens;
            }
            else
            {
                // Check if description needs truncation even within budget
                if (candidate.Part.Description.Length > budget.MaxDescriptionLength)
                {
                    candidate = TruncateCandidate(candidate, budget.MaxDescriptionLength);
                    line = FormatCandidateLine(candidate, i + 1);
                    lineTokens = _estimator.EstimateTokens(line);
                }

                result.Add(candidate);
                usedTokens += lineTokens;
            }
        }

        if (result.Count < candidates.Count)
        {
            _logger?.LogInformation(
                "Context trimming: included {Included}/{Total} candidates within {Budget} token budget.",
                result.Count, candidates.Count, budget.MaxContextTokens);
        }

        return result;
    }

    private List<string> TrimSnippets(IReadOnlyList<string> snippets, ContextBudget budget)
    {
        var result = new List<string>();
        int usedTokens = 0;

        foreach (var snippet in snippets)
        {
            var tokens = _estimator.EstimateTokens(snippet);
            if (usedTokens + tokens > budget.MaxSnippetTokens)
            {
                int dropped = snippets.Count - result.Count;
                _logger?.LogInformation(
                    "Context trimming: dropped {Dropped} page snippets (budget {Budget} tokens, used {Used}).",
                    dropped, budget.MaxSnippetTokens, usedTokens);
                break;
            }
            result.Add(snippet);
            usedTokens += tokens;
        }

        return result;
    }

    /// <summary>
    /// Formats a candidate the same way PromptBuilder does, for token counting.
    /// </summary>
    internal static string FormatCandidateLine(SearchCandidate c, int index)
    {
        var p = c.Part;
        return $"  [{index}] PartNumber={p.PartNumber} | Desc={p.Description} | Pos={p.Position} " +
               $"| Illus={p.Illustration} | Page={p.PageNumber} | Qty={p.Quantity} " +
               $"| Model={p.Model} | Remark={p.Remark} | Score={c.Score:F2} | Reason={c.MatchReason}";
    }

    private static SearchCandidate TruncateCandidate(SearchCandidate candidate, int maxDescLength)
    {
        var desc = candidate.Part.Description;
        if (desc.Length <= maxDescLength)
            return candidate;

        var truncatedDesc = desc[..(maxDescLength - 3)] + "...";
        var truncatedPart = candidate.Part with { Description = truncatedDesc };
        return candidate with { Part = truncatedPart };
    }
}

/// <summary>
/// Result of context trimming: the candidates and snippets that fit within budget.
/// </summary>
public sealed record TrimmedContext(
    IReadOnlyList<SearchCandidate> Candidates,
    IReadOnlyList<string> Snippets);
