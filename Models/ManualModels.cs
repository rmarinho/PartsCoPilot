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
    /// <summary>
    /// Pre-rendered PNG image of the full PDF page (illustrations, diagrams, text layout).
    /// Null when rendering was unavailable or failed — viewer falls back to text mode.
    /// </summary>
    public byte[]? ImageData { get; init; }
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

/// <summary>
/// A legend/applicability entry from the manual — captures model, engine,
/// or transmission applicability notes tied to a specific illustration.
/// </summary>
public sealed record LegendEntry
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public required string ManualId { get; init; }
    public required string Code { get; init; }
    public required string Description { get; init; }
    public string? Illustration { get; init; }
    public string? ApplicableModels { get; init; }
    public string? YearRange { get; init; }
    public string? Notes { get; init; }
}

/// <summary>
/// A vehicle type entry from the manual summary pages.
/// </summary>
public sealed record VehicleType
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public required string ManualId { get; init; }
    public required string Code { get; init; }
    public required string ModelName { get; init; }
    public string? Variant { get; init; }
    public int? YearFrom { get; init; }
    public int? YearTo { get; init; }
    public string? ChassisRange { get; init; }
}

/// <summary>
/// An engine type entry from the manual summary pages.
/// </summary>
public sealed record EngineType
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public required string ManualId { get; init; }
    public required string Code { get; init; }
    public required string EngineName { get; init; }
    public string? Displacement { get; init; }
    public string? Power { get; init; }
    public string? ApplicableModels { get; init; }
    public int? YearFrom { get; init; }
    public int? YearTo { get; init; }
}

/// <summary>
/// A transmission type entry from the manual summary pages.
/// </summary>
public sealed record TransmissionType
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public required string ManualId { get; init; }
    public required string Code { get; init; }
    public required string TransmissionName { get; init; }
    public string? Type { get; init; }
    public string? ApplicableModels { get; init; }
    public int? YearFrom { get; init; }
    public int? YearTo { get; init; }
}
