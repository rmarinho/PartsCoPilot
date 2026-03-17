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
    Task<IReadOnlyList<PartRecord>> SearchPartsAsync(string query, string? manualId = null, CancellationToken ct = default);

    Task SavePagesAsync(IReadOnlyList<ManualPage> pages, CancellationToken ct = default);
    Task<ManualPage?> GetPageAsync(string manualId, int pageNumber, CancellationToken ct = default);

    Task SaveIllustrationGroupsAsync(IReadOnlyList<IllustrationGroup> groups, CancellationToken ct = default);
}

/// <summary>
/// Hybrid search combining exact, text, and ranked results.
/// </summary>
public interface ISearchService
{
    Task<SearchResult> SearchAsync(SearchQuery query, CancellationToken ct = default);
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
