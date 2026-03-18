using FluentAssertions;
using PartsCopilot.Models;
using PartsCopilot.Services;
using Xunit;

namespace PartsCopilot.Tests;

/// <summary>
/// Tests for TokenEstimator: chars/4 heuristic accuracy.
/// </summary>
public class TokenEstimatorTests
{
    private readonly TokenEstimator _estimator = new();

    [Fact]
    public void EstimateTokens_NullInput_ReturnsZero()
    {
        _estimator.EstimateTokens(null).Should().Be(0);
    }

    [Fact]
    public void EstimateTokens_EmptyInput_ReturnsZero()
    {
        _estimator.EstimateTokens("").Should().Be(0);
    }

    [Fact]
    public void EstimateTokens_FourChars_ReturnsOne()
    {
        _estimator.EstimateTokens("test").Should().Be(1);
    }

    [Fact]
    public void EstimateTokens_EightChars_ReturnsTwo()
    {
        _estimator.EstimateTokens("12345678").Should().Be(2);
    }

    [Fact]
    public void EstimateTokens_FiveChars_RoundsUp()
    {
        // 5 / 4 = 1.25 → ceil = 2
        _estimator.EstimateTokens("hello").Should().Be(2);
    }

    [Fact]
    public void EstimateTokens_OneChar_ReturnsOne()
    {
        _estimator.EstimateTokens("x").Should().Be(1);
    }

    [Fact]
    public void EstimateTokens_LargeText_ScalesLinearly()
    {
        var text = new string('a', 4000);
        _estimator.EstimateTokens(text).Should().Be(1000);
    }

    [Fact]
    public void EstimateTokens_RealisticSentence_ReasonableRange()
    {
        // "The quick brown fox" = 19 chars → 19/4 = 4.75 → ceil = 5
        // Real tokenizers would give ~4-5 tokens, so this is close
        var tokens = _estimator.EstimateTokens("The quick brown fox");
        tokens.Should().BeInRange(4, 6);
    }
}

/// <summary>
/// Tests for ContextTrimmer: budget enforcement, relevance ordering, truncation.
/// </summary>
public class ContextTrimmerTests
{
    private readonly TokenEstimator _estimator = new();
    private readonly ContextTrimmer _trimmer;

    public ContextTrimmerTests()
    {
        _trimmer = new ContextTrimmer(_estimator);
    }

    private static SearchCandidate MakeCandidate(double score, string partNumber = "P-001",
        string description = "Test part description", string matchReason = "text")
    {
        var part = new PartRecord
        {
            ManualId = "test-manual",
            PartNumber = partNumber,
            PartNumberNormalized = partNumber.Replace("-", ""),
            Description = description,
            SearchText = $"{partNumber} {description}",
            PageNumber = 1
        };
        return new SearchCandidate(part, score, matchReason);
    }

    [Fact]
    public void TrimToFit_EmptyResults_ReturnsEmpty()
    {
        var budget = new ContextBudget { MaxContextTokens = 4000 };
        var result = _trimmer.TrimToFit([], [], budget);

        result.Candidates.Should().BeEmpty();
        result.Snippets.Should().BeEmpty();
    }

    [Fact]
    public void TrimToFit_AllResultsFit_PreservesAll()
    {
        var candidates = new[]
        {
            MakeCandidate(0.95, "P-001"),
            MakeCandidate(0.80, "P-002"),
            MakeCandidate(0.65, "P-003"),
        };
        var budget = new ContextBudget { MaxContextTokens = 4000 };

        var result = _trimmer.TrimToFit(candidates, [], budget);

        result.Candidates.Should().HaveCount(3);
    }

    [Fact]
    public void TrimToFit_SortsbyRelevanceDescending()
    {
        var candidates = new[]
        {
            MakeCandidate(0.30, "P-LOW"),
            MakeCandidate(0.95, "P-HIGH"),
            MakeCandidate(0.60, "P-MED"),
        };
        var budget = new ContextBudget { MaxContextTokens = 4000 };

        var result = _trimmer.TrimToFit(candidates, [], budget);

        result.Candidates[0].Part.PartNumber.Should().Be("P-HIGH");
        result.Candidates[1].Part.PartNumber.Should().Be("P-MED");
        result.Candidates[2].Part.PartNumber.Should().Be("P-LOW");
    }

    [Fact]
    public void TrimToFit_ExceedsBudget_DropsLowRelevance()
    {
        // Each candidate line is roughly 150-200 chars → ~40-50 tokens
        // With a budget of 100 tokens, only ~2 candidates should fit
        var candidates = new[]
        {
            MakeCandidate(0.95, "P-HIGH", "High relevance part"),
            MakeCandidate(0.80, "P-MED",  "Medium relevance part"),
            MakeCandidate(0.10, "P-LOW1", "Low relevance part one"),
            MakeCandidate(0.05, "P-LOW2", "Low relevance part two"),
            MakeCandidate(0.01, "P-LOW3", "Low relevance part three"),
        };
        var budget = new ContextBudget { MaxContextTokens = 100 };

        var result = _trimmer.TrimToFit(candidates, [], budget);

        // Should have fewer than all candidates
        result.Candidates.Count.Should().BeLessThan(5);
        // High-relevance should be preserved
        result.Candidates[0].Part.PartNumber.Should().Be("P-HIGH");
        // Low-relevance should be dropped
        result.Candidates.Should().NotContain(c => c.Part.PartNumber == "P-LOW3");
    }

    [Fact]
    public void TrimToFit_SingleHugeResult_TruncatesDescription()
    {
        var hugeDesc = new string('X', 1000);
        var candidates = new[] { MakeCandidate(0.95, "P-001", hugeDesc) };
        var budget = new ContextBudget
        {
            MaxContextTokens = 4000,
            MaxDescriptionLength = 100
        };

        var result = _trimmer.TrimToFit(candidates, [], budget);

        result.Candidates.Should().HaveCount(1);
        result.Candidates[0].Part.Description.Length.Should().BeLessThanOrEqualTo(100);
        result.Candidates[0].Part.Description.Should().EndWith("...");
    }

    [Fact]
    public void TrimToFit_SnippetsExceedBudget_TrimsSnippets()
    {
        var snippets = Enumerable.Range(1, 20)
            .Select(i => new string('S', 200) + $" snippet {i}")
            .ToList();
        var budget = new ContextBudget { MaxSnippetTokens = 200 };

        var result = _trimmer.TrimToFit([], snippets, budget);

        result.Snippets.Count.Should().BeLessThan(20);
        result.Snippets.Count.Should().BeGreaterThan(0);
    }

    [Fact]
    public void TrimToFit_HighRelevancePreserved_LowRelevanceTrimmed()
    {
        // Create 50 candidates with decreasing scores
        var candidates = Enumerable.Range(0, 50)
            .Select(i => MakeCandidate(1.0 - (i * 0.02), $"P-{i:D3}"))
            .ToList();
        // Tight budget — can only fit a few
        var budget = new ContextBudget { MaxContextTokens = 200 };

        var result = _trimmer.TrimToFit(candidates, [], budget);

        // The included ones should all be high-relevance
        foreach (var c in result.Candidates)
            c.Score.Should().BeGreaterThanOrEqualTo(0.80);
    }

    [Fact]
    public void TrimToFit_NullCandidates_Throws()
    {
        var act = () => _trimmer.TrimToFit(null!, [], new ContextBudget());
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void TrimToFit_NullSnippets_Throws()
    {
        var act = () => _trimmer.TrimToFit([], null!, new ContextBudget());
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void TrimToFit_NullBudget_Throws()
    {
        var act = () => _trimmer.TrimToFit([], [], null!);
        act.Should().Throw<ArgumentNullException>();
    }
}

/// <summary>
/// Tests for PromptBuilder integration with context trimming.
/// </summary>
public class PromptBuilderContextWindowTests
{
    private readonly TokenEstimator _estimator = new();

    private static SearchCandidate MakeCandidate(double score, string partNumber = "P-001",
        string description = "Test part")
    {
        var part = new PartRecord
        {
            ManualId = "test-manual",
            PartNumber = partNumber,
            PartNumberNormalized = partNumber.Replace("-", ""),
            Description = description,
            SearchText = $"{partNumber} {description}",
            PageNumber = 1
        };
        return new SearchCandidate(part, score, "text");
    }

    [Fact]
    public void BuildPrompt_WithTrimmer_TrimsExcessCandidates()
    {
        var trimmer = new ContextTrimmer(new TokenEstimator());
        var budget = new ContextBudget { MaxContextTokens = 100 };
        var builder = new PromptBuilder(trimmer, budget);

        var candidates = Enumerable.Range(0, 20)
            .Select(i => MakeCandidate(1.0 - (i * 0.05), $"P-{i:D3}"))
            .ToList();

        var context = new PromptContext("Find brake pads", null, candidates, []);
        var prompt = builder.BuildPrompt(context);

        // Should contain fewer than 20 candidate rows
        var candidateLines = prompt.Split('\n')
            .Count(l => l.TrimStart().StartsWith("["));
        candidateLines.Should().BeLessThan(20);
        candidateLines.Should().BeGreaterThan(0);
    }

    [Fact]
    public void BuildPrompt_WithoutTrimmer_IncludesAllCandidates()
    {
        var builder = new PromptBuilder(); // no trimmer

        var candidates = Enumerable.Range(0, 5)
            .Select(i => MakeCandidate(0.90 - (i * 0.1), $"P-{i:D3}"))
            .ToList();

        var context = new PromptContext("Find brake pads", null, candidates, []);
        var prompt = builder.BuildPrompt(context);

        var candidateLines = prompt.Split('\n')
            .Count(l => l.TrimStart().StartsWith("["));
        candidateLines.Should().Be(5);
    }

    [Fact]
    public void BuildPrompt_BackwardCompatible_NoTrimmer_Works()
    {
        var builder = new PromptBuilder();
        var context = new PromptContext("test question", null, [], []);
        var prompt = builder.BuildPrompt(context);

        prompt.Should().Contain("PartsCopilot");
        prompt.Should().Contain("<user_query>test question</user_query>");
    }

    [Fact]
    public void BuildPrompt_HighRelevanceFirst_InOutput()
    {
        var trimmer = new ContextTrimmer(new TokenEstimator());
        var builder = new PromptBuilder(trimmer);

        var candidates = new[]
        {
            MakeCandidate(0.30, "P-LOW"),
            MakeCandidate(0.95, "P-HIGH"),
            MakeCandidate(0.60, "P-MED"),
        };

        var context = new PromptContext("test", null, candidates, []);
        var prompt = builder.BuildPrompt(context);

        var highIdx = prompt.IndexOf("P-HIGH", StringComparison.Ordinal);
        var medIdx = prompt.IndexOf("P-MED", StringComparison.Ordinal);
        var lowIdx = prompt.IndexOf("P-LOW", StringComparison.Ordinal);

        highIdx.Should().BeLessThan(medIdx, "highest score should appear first");
        medIdx.Should().BeLessThan(lowIdx, "medium score should appear before low");
    }

    [Fact]
    public void BuildPrompt_EmptyCandidates_NoCandidateSection()
    {
        var trimmer = new ContextTrimmer(new TokenEstimator());
        var builder = new PromptBuilder(trimmer);
        var context = new PromptContext("test", null, [], []);
        var prompt = builder.BuildPrompt(context);

        prompt.Should().NotContain("RETRIEVED CANDIDATES");
    }

    [Fact]
    public void ContextBudget_Defaults_AreReasonable()
    {
        var budget = new ContextBudget();
        budget.MaxContextTokens.Should().Be(4000);
        budget.MaxDescriptionLength.Should().Be(200);
        budget.MaxSnippetTokens.Should().Be(1000);
    }

    [Fact]
    public void ContextBudget_IsConfigurable()
    {
        var budget = new ContextBudget
        {
            MaxContextTokens = 8000,
            MaxDescriptionLength = 500,
            MaxSnippetTokens = 2000,
        };
        budget.MaxContextTokens.Should().Be(8000);
        budget.MaxDescriptionLength.Should().Be(500);
        budget.MaxSnippetTokens.Should().Be(2000);
    }
}
