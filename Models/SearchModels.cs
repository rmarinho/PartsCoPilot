namespace PartsCopilot.Models;

/// <summary>
/// A normalized part record extracted from a manual.
/// </summary>
public sealed record PartRecord
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public required string ManualId { get; init; }
    public string? Position { get; init; }
    public required string PartNumber { get; init; }
    public required string PartNumberNormalized { get; init; }
    public required string Description { get; init; }
    public required string SearchText { get; init; }
    public string? Remark { get; init; }
    public string? Quantity { get; init; }
    public string? Model { get; init; }
    public string? Section { get; init; }
    public string? Illustration { get; init; }
    public int PageNumber { get; init; }
}

/// <summary>
/// Vehicle context for filtering searches.
/// </summary>
public sealed record VehicleContext(
    string? Model = null,
    int? Year = null,
    string? Variant = null,
    string? Engine = null,
    string? Region = null);

/// <summary>
/// A search query combining user text and filters.
/// </summary>
public sealed record SearchQuery(
    string UserText,
    VehicleContext? Context = null,
    bool IsExactPartNumber = false,
    string? ManualId = null);

/// <summary>
/// A ranked search candidate.
/// </summary>
public sealed record SearchCandidate(
    PartRecord Part,
    double Score,
    string MatchReason);

/// <summary>
/// Complete search result returned to the UI.
/// </summary>
public sealed record SearchResult(
    IReadOnlyList<SearchCandidate> Candidates,
    int TotalMatches,
    TimeSpan SearchDuration);
