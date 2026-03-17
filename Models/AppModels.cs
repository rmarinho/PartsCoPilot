namespace PartsCopilot.Models;

/// <summary>
/// A saved search history entry.
/// </summary>
public sealed record SearchHistoryEntry
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public required string QueryText { get; init; }
    public string? ManualId { get; init; }
    public int ResultCount { get; init; }
    public DateTime SearchedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// A user's favorite part.
/// </summary>
public sealed record FavoriteEntry
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public required string PartRecordId { get; init; }
    public required string PartNumber { get; init; }
    public required string Description { get; init; }
    public string? ManualId { get; init; }
    public DateTime SavedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Diagnostics from a PDF ingestion run.
/// </summary>
public sealed record IngestionDiagnostics
{
    public int PagesProcessed { get; init; }
    public int PagesWithIllustrations { get; init; }
    public int PagesClassifiedAsPartTables { get; init; }
    public int RowsParsed { get; init; }
    public int RowsMissingPartNumber { get; init; }
    public int RowsMissingDescription { get; init; }
    public TimeSpan Duration { get; init; }
    public IReadOnlyList<string> Warnings { get; init; } = [];
}
