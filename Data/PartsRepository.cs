using PartsCopilot.Models;
using PartsCopilot.Services;

namespace PartsCopilot.Data;

public class PartsRepository : IPartsRepository
{
    private readonly AppDatabase _db;

    public PartsRepository(AppDatabase db) => _db = db;

    public async Task<ManualMetadata?> GetManualAsync(string manualId, CancellationToken ct = default)
    {
        var entity = await _db.Connection.FindAsync<ManualEntity>(manualId);
        return entity?.ToDomain();
    }

    public async Task<IReadOnlyList<ManualMetadata>> GetAllManualsAsync(CancellationToken ct = default)
    {
        var entities = await _db.Connection.Table<ManualEntity>().ToListAsync();
        return entities.Select(e => e.ToDomain()).ToList();
    }

    public async Task SaveManualAsync(ManualMetadata manual, CancellationToken ct = default)
    {
        await _db.Connection.InsertOrReplaceAsync(ManualEntity.FromDomain(manual));
    }

    public async Task DeleteManualAsync(string manualId, CancellationToken ct = default)
    {
        await _db.Connection.Table<PartEntity>().DeleteAsync(p => p.ManualId == manualId);
        await _db.Connection.Table<PageEntity>().DeleteAsync(p => p.ManualId == manualId);
        await _db.Connection.Table<IllustrationGroupEntity>().DeleteAsync(g => g.ManualId == manualId);
        await _db.Connection.DeleteAsync<ManualEntity>(manualId);
    }

    public async Task SavePartsAsync(IReadOnlyList<PartRecord> parts, CancellationToken ct = default)
    {
        var entities = parts.Select(PartEntity.FromDomain).ToList();
        await _db.Connection.RunInTransactionAsync(conn =>
        {
            foreach (var e in entities)
                conn.InsertOrReplace(e);
        });
    }

    public async Task<IReadOnlyList<PartRecord>> GetPartsByManualAsync(string manualId, CancellationToken ct = default)
    {
        var entities = await _db.Connection.Table<PartEntity>()
            .Where(p => p.ManualId == manualId)
            .ToListAsync();
        return entities.Select(e => e.ToDomain()).ToList();
    }

    public async Task<PartRecord?> GetPartAsync(string partId, CancellationToken ct = default)
    {
        var entity = await _db.Connection.FindAsync<PartEntity>(partId);
        return entity?.ToDomain();
    }

    public async Task<IReadOnlyList<PartRecord>> SearchPartsAsync(string query, string? manualId = null, CancellationToken ct = default)
    {
        var normalized = query.Replace(" ", "").ToLowerInvariant();
        var lower = query.ToLowerInvariant();

        // Try exact part number match first
        var exactMatches = await _db.Connection.Table<PartEntity>()
            .Where(p => p.PartNumberNormalized == normalized)
            .ToListAsync();

        if (exactMatches.Count > 0)
        {
            if (manualId is not null)
                exactMatches = exactMatches.Where(p => p.ManualId == manualId).ToList();
            return exactMatches.Select(e => e.ToDomain()).ToList();
        }

        // Fall back to text search
        var allParts = manualId is not null
            ? await _db.Connection.Table<PartEntity>().Where(p => p.ManualId == manualId).ToListAsync()
            : await _db.Connection.Table<PartEntity>().ToListAsync();

        var results = allParts
            .Where(p => p.SearchText.Contains(lower) || p.Description.ToLowerInvariant().Contains(lower))
            .Take(100)
            .Select(e => e.ToDomain())
            .ToList();

        return results;
    }

    public async Task SavePagesAsync(IReadOnlyList<ManualPage> pages, CancellationToken ct = default)
    {
        var entities = pages.Select(PageEntity.FromDomain).ToList();
        await _db.Connection.RunInTransactionAsync(conn =>
        {
            foreach (var e in entities)
                conn.InsertOrReplace(e);
        });
    }

    public async Task<ManualPage?> GetPageAsync(string manualId, int pageNumber, CancellationToken ct = default)
    {
        var entity = await _db.Connection.Table<PageEntity>()
            .Where(p => p.ManualId == manualId && p.PageNumber == pageNumber)
            .FirstOrDefaultAsync();
        return entity?.ToDomain();
    }

    public async Task SaveIllustrationGroupsAsync(IReadOnlyList<IllustrationGroup> groups, CancellationToken ct = default)
    {
        var entities = groups.Select(IllustrationGroupEntity.FromDomain).ToList();
        await _db.Connection.RunInTransactionAsync(conn =>
        {
            foreach (var e in entities)
                conn.InsertOrReplace(e);
        });
    }
}
