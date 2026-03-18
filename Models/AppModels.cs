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
    public string? Model { get; init; }
    public int PageNumber { get; init; }
    public string? Illustration { get; init; }
    public string? ManualId { get; init; }
    public DateTime SavedAt { get; init; } = DateTime.UtcNow;
    public string? Model { get; init; }
    public int? PageNumber { get; init; }
    public string? Illustration { get; init; }
}

/// <summary>
/// A legend/applicability entry from a parts manual.
/// </summary>
public sealed record LegendEntry
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public required string ManualId { get; init; }
    public string? Illustration { get; init; }
    public required string Code { get; init; }
    public required string Description { get; init; }
    public string? ApplicableModels { get; init; }
    public string? YearRange { get; init; }
    public string? Notes { get; init; }
}

/// <summary>
/// A vehicle type entry from summary pages.
/// </summary>
public sealed record VehicleType
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public required string ManualId { get; init; }
    public required string Code { get; init; }
    public required string Model { get; init; }
    public string? Variant { get; init; }
    public string? YearRange { get; init; }
    public string? ChassisRange { get; init; }
}

/// <summary>
/// An engine type entry from summary pages.
/// </summary>
public sealed record EngineType
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public required string ManualId { get; init; }
    public required string Code { get; init; }
    public required string Name { get; init; }
    public string? Displacement { get; init; }
    public string? Power { get; init; }
    public string? ApplicableModels { get; init; }
}

/// <summary>
/// A transmission type entry from summary pages.
/// </summary>
public sealed record TransmissionType
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public required string ManualId { get; init; }
    public required string Code { get; init; }
    public required string Name { get; init; }
    public string? Type { get; init; }
    public string? ApplicableModels { get; init; }
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
