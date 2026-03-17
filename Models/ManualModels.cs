namespace PartsCopilot.Models;

/// <summary>
/// Metadata about an imported parts manual. Supports multiple manuals.
/// </summary>
public sealed record ManualMetadata
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public required string Title { get; init; }
    public required string FilePath { get; init; }
    public string? VehicleMake { get; init; }
    public string? VehicleModel { get; init; }
    public string? YearRange { get; init; }
    public string? ManualType { get; init; }
    public int PageCount { get; init; }
    public int PartCount { get; init; }
    public DateTime ImportedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// A single page extracted from a PDF manual.
/// </summary>
public sealed record ManualPage
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public required string ManualId { get; init; }
    public required int PageNumber { get; init; }
    public required string RawText { get; init; }
    public string? Illustration { get; init; }
    public string? Section { get; init; }
    public required string PageType { get; init; }
}

/// <summary>
/// An illustration group within a manual.
/// </summary>
public sealed record IllustrationGroup
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public required string ManualId { get; init; }
    public required string IllustrationNumber { get; init; }
    public string? Title { get; init; }
    public required int StartPage { get; init; }
    public int? EndPage { get; init; }
}
