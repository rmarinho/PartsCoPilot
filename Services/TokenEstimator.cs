using Microsoft.Extensions.Logging;

namespace PartsCopilot.Services;

/// <summary>
/// Estimates token counts for text using a chars/4 heuristic.
/// Provides a conservative upper-bound suitable for budget enforcement.
/// </summary>
public sealed class TokenEstimator : ITokenEstimator
{
    /// <summary>Average characters per token for English text (GPT-family models).</summary>
    internal const double CharsPerToken = 4.0;

    /// <summary>
    /// Estimates the number of tokens in the given text.
    /// Returns 0 for null or empty input.
    /// </summary>
    public int EstimateTokens(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return 0;

        return (int)Math.Ceiling(text.Length / CharsPerToken);
    }
}
