using PartsCopilot.Models;
using PartsCopilot.Services;

namespace PartsCopilot.Data;

public class UserDataRepository : IUserDataRepository
{
    private readonly AppDatabase _db;

    public UserDataRepository(AppDatabase db) => _db = db;

    public async Task SaveSearchAsync(SearchHistoryEntry entry, CancellationToken ct = default)
    {
        await _db.Connection.InsertOrReplaceAsync(SearchHistoryEntity.FromDomain(entry));
    }

    public async Task<IReadOnlyList<SearchHistoryEntry>> GetRecentSearchesAsync(int limit = 20, CancellationToken ct = default)
    {
        var entities = await _db.Connection.Table<SearchHistoryEntity>()
            .OrderByDescending(e => e.SearchedAt)
            .Take(limit)
            .ToListAsync();
        return entities.Select(e => e.ToDomain()).ToList();
    }

    public async Task SaveFavoriteAsync(FavoriteEntry entry, CancellationToken ct = default)
    {
        await _db.Connection.InsertOrReplaceAsync(FavoriteEntity.FromDomain(entry));
    }

    public async Task RemoveFavoriteAsync(string partRecordId, CancellationToken ct = default)
    {
        await _db.Connection.Table<FavoriteEntity>()
            .DeleteAsync(f => f.PartRecordId == partRecordId);
    }

    public async Task<IReadOnlyList<FavoriteEntry>> GetFavoritesAsync(CancellationToken ct = default)
    {
        var entities = await _db.Connection.Table<FavoriteEntity>()
            .OrderByDescending(e => e.SavedAt)
            .ToListAsync();
        return entities.Select(e => e.ToDomain()).ToList();
    }

    public async Task<bool> IsFavoriteAsync(string partRecordId, CancellationToken ct = default)
    {
        var count = await _db.Connection.Table<FavoriteEntity>()
            .Where(f => f.PartRecordId == partRecordId)
            .CountAsync();
        return count > 0;
    }
}
