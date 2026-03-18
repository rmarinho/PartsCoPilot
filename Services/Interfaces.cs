using PartsCopilot.Models;

namespace PartsCopilot.Services;

/// <summary>
/// Extracts raw pages from a PDF file.
/// </summary>
public interface IPdfIngestionService
{
    Task<IReadOnlyList<ManualPage>> ExtractPagesAsync(string filePath, string manualId, CancellationToken ct = default);
}

/// <summary>
/// Renders individual PDF pages to PNG images for display in the manual viewer.
/// Returns null on platforms where native rendering is unavailable.
/// </summary>
public interface IPdfPageRenderer
{
    Task<byte[]?> RenderPageToImageAsync(string filePath, int pageNumber, int dpi = 150, CancellationToken ct = default);
    bool IsSupported { get; }
}

/// <summary>
/// Parses raw page text into structured part records.
/// Implementations are manual-specific (e.g., Porsche classic format).
/// </summary>
public interface IManualParser
{
    string ManualType { get; }
    bool CanParse(IReadOnlyList<ManualPage> samplePages);
    Task<IReadOnlyList<PartRecord>> ParseAsync(IReadOnlyList<ManualPage> pages, string manualId, IProgress<int>? progress = null, CancellationToken ct = default);
}

/// <summary>
/// Stores and retrieves part records and manual metadata.
/// </summary>
public interface IPartsRepository
{
    Task<ManualMetadata?> GetManualAsync(string manualId, CancellationToken ct = default);
    Task<IReadOnlyList<ManualMetadata>> GetAllManualsAsync(CancellationToken ct = default);
    Task SaveManualAsync(ManualMetadata manual, CancellationToken ct = default);
    Task DeleteManualAsync(string manualId, CancellationToken ct = default);

    Task SavePartsAsync(IReadOnlyList<PartRecord> parts, CancellationToken ct = default);
    Task<IReadOnlyList<PartRecord>> GetPartsByManualAsync(string manualId, CancellationToken ct = default);
    Task<PartRecord?> GetPartAsync(string partId, CancellationToken ct = default);
    Task<IReadOnlyList<PartRecord>> SearchPartsAsync(string query, string? manualId = null, int pageSize = 100, int offset = 0, CancellationToken ct = default);

    Task SavePagesAsync(IReadOnlyList<ManualPage> pages, CancellationToken ct = default);
    Task<ManualPage?> GetPageAsync(string manualId, int pageNumber, CancellationToken ct = default);

    Task SaveIllustrationGroupsAsync(IReadOnlyList<IllustrationGroup> groups, CancellationToken ct = default);

    Task SaveLegendEntriesAsync(IReadOnlyList<LegendEntry> entries, CancellationToken ct = default);
    Task<IReadOnlyList<LegendEntry>> GetLegendEntriesAsync(string manualId, CancellationToken ct = default);

    Task SaveVehicleTypesAsync(IReadOnlyList<VehicleType> types, CancellationToken ct = default);
    Task<IReadOnlyList<VehicleType>> GetVehicleTypesAsync(string manualId, CancellationToken ct = default);

    Task SaveEngineTypesAsync(IReadOnlyList<EngineType> types, CancellationToken ct = default);
    Task<IReadOnlyList<EngineType>> GetEngineTypesAsync(string manualId, CancellationToken ct = default);

    Task SaveTransmissionTypesAsync(IReadOnlyList<TransmissionType> types, CancellationToken ct = default);
    Task<IReadOnlyList<TransmissionType>> GetTransmissionTypesAsync(string manualId, CancellationToken ct = default);
}

/// <summary>
/// Hybrid search combining exact, text, and ranked results.
/// </summary>
public interface ISearchService
{
    Task<SearchResult> SearchAsync(SearchQuery query, CancellationToken ct = default);
}

/// <summary>
/// Estimates token count for a given text.
/// </summary>
public interface ITokenEstimator
{
    int EstimateTokens(string? text);
}

/// <summary>
/// Trims context to fit within a token budget, preserving highest-relevance results.
/// </summary>
public interface IContextTrimmer
{
    TrimmedContext TrimToFit(
        IReadOnlyList<SearchCandidate> candidates,
        IReadOnlyList<string> snippets,
        ContextBudget budget);
}

/// <summary>
/// Builds retrieval-grounded prompts for the AI layer.
/// </summary>
public interface IPromptBuilder
{
    string BuildPrompt(PromptContext context);
}

/// <summary>
/// AI service that answers questions from retrieved context.
/// </summary>
public interface IPartsAiService
{
    Task<AiAnswer> AskAsync(PromptContext context, CancellationToken ct = default);
}

/// <summary>
/// Maps part records to their source PDF pages and illustration groups.
/// </summary>
public interface IManualNavigationService
{
    int GetPageNumber(PartRecord part);
    string? GetIllustrationGroup(PartRecord part);
    Task<ManualPage?> GetPageAsync(PartRecord part, CancellationToken ct = default);
    Task<IllustrationGroup?> GetIllustrationAsync(PartRecord part, CancellationToken ct = default);
}

/// <summary>
/// Manages search history and favorites.
/// </summary>
public interface IUserDataRepository
{
    Task SaveSearchAsync(SearchHistoryEntry entry, CancellationToken ct = default);
    Task<IReadOnlyList<SearchHistoryEntry>> GetRecentSearchesAsync(int limit = 20, CancellationToken ct = default);

    Task SaveFavoriteAsync(FavoriteEntry entry, CancellationToken ct = default);
    Task RemoveFavoriteAsync(string partRecordId, CancellationToken ct = default);
    Task<IReadOnlyList<FavoriteEntry>> GetFavoritesAsync(CancellationToken ct = default);
    Task<bool> IsFavoriteAsync(string partRecordId, CancellationToken ct = default);
}
