namespace PartsCopilot.Models;

/// <summary>
/// Structured AI response with matched parts.
/// </summary>
public sealed record AiAnswer(
    string Answer,
    IReadOnlyList<AiMatch> Matches,
    bool NeedsClarification,
    string? ClarificationQuestion);

/// <summary>
/// A single AI-identified match.
/// </summary>
public sealed record AiMatch(
    string PartNumber,
    string Description,
    string? Position,
    string? Illustration,
    int Page,
    string? Qty,
    string? Model,
    string? Remark,
    string? Fitment,
    double Confidence);

/// <summary>
/// Context passed to the prompt builder.
/// </summary>
public sealed record PromptContext(
    string UserQuestion,
    VehicleContext? VehicleContext,
    IReadOnlyList<SearchCandidate> Candidates,
    IReadOnlyList<string> PageSnippets);


