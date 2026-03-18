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

    public async Task<IReadOnlyList<PartRecord>> SearchPartsAsync(string query, string? manualId = null, int pageSize = 100, int offset = 0, CancellationToken ct = default)
    {
        var normalized = query.Replace(" ", "").ToLowerInvariant();
        var lower = query.ToLowerInvariant();

        // Try exact part number match first — fully at SQL level
        List<PartEntity> exactMatches;
        if (manualId is not null)
        {
            exactMatches = await _db.Connection.QueryAsync<PartEntity>(
                "SELECT * FROM Parts WHERE PartNumberNormalized = ? AND ManualId = ? LIMIT ? OFFSET ?",
                normalized, manualId, pageSize, offset);
        }
        else
        {
            exactMatches = await _db.Connection.QueryAsync<PartEntity>(
                "SELECT * FROM Parts WHERE PartNumberNormalized = ? LIMIT ? OFFSET ?",
                normalized, pageSize, offset);
        }

        if (exactMatches.Count > 0)
            return exactMatches.Select(e => e.ToDomain()).ToList();

        // Fall back to text search — SQL LIKE, no in-memory filtering
        var likePattern = $"%{EscapeLike(lower)}%";
        List<PartEntity> results;
        if (manualId is not null)
        {
            results = await _db.Connection.QueryAsync<PartEntity>(
                "SELECT * FROM Parts WHERE ManualId = ? AND (SearchText LIKE ? ESCAPE '\\' OR LOWER(Description) LIKE ? ESCAPE '\\') LIMIT ? OFFSET ?",
                manualId, likePattern, likePattern, pageSize, offset);
        }
        else
        {
            results = await _db.Connection.QueryAsync<PartEntity>(
                "SELECT * FROM Parts WHERE SearchText LIKE ? ESCAPE '\\' OR LOWER(Description) LIKE ? ESCAPE '\\' LIMIT ? OFFSET ?",
                likePattern, likePattern, pageSize, offset);
        }

        return results.Select(e => e.ToDomain()).ToList();
    }

    private static string EscapeLike(string value)
        => value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");

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

    public async Task SaveLegendEntriesAsync(IReadOnlyList<LegendEntry> entries, CancellationToken ct = default)
    {
        var entities = entries.Select(LegendEntryEntity.FromDomain).ToList();
        await _db.Connection.RunInTransactionAsync(conn =>
        {
            foreach (var e in entities)
                conn.InsertOrReplace(e);
        });
    }

    public async Task<IReadOnlyList<LegendEntry>> GetLegendEntriesAsync(string manualId, CancellationToken ct = default)
    {
        var entities = await _db.Connection.Table<LegendEntryEntity>()
            .Where(e => e.ManualId == manualId)
            .ToListAsync();
        return entities.Select(e => e.ToDomain()).ToList();
    }

    public async Task SaveVehicleTypesAsync(IReadOnlyList<VehicleType> types, CancellationToken ct = default)
    {
        var entities = types.Select(VehicleTypeEntity.FromDomain).ToList();
        await _db.Connection.RunInTransactionAsync(conn =>
        {
            foreach (var e in entities)
                conn.InsertOrReplace(e);
        });
    }

    public async Task<IReadOnlyList<VehicleType>> GetVehicleTypesAsync(string manualId, CancellationToken ct = default)
    {
        var entities = await _db.Connection.Table<VehicleTypeEntity>()
            .Where(e => e.ManualId == manualId)
            .ToListAsync();
        return entities.Select(e => e.ToDomain()).ToList();
    }

    public async Task SaveEngineTypesAsync(IReadOnlyList<EngineType> types, CancellationToken ct = default)
    {
        var entities = types.Select(EngineTypeEntity.FromDomain).ToList();
        await _db.Connection.RunInTransactionAsync(conn =>
        {
            foreach (var e in entities)
                conn.InsertOrReplace(e);
        });
    }

    public async Task<IReadOnlyList<EngineType>> GetEngineTypesAsync(string manualId, CancellationToken ct = default)
    {
        var entities = await _db.Connection.Table<EngineTypeEntity>()
            .Where(e => e.ManualId == manualId)
            .ToListAsync();
        return entities.Select(e => e.ToDomain()).ToList();
    }

    public async Task SaveTransmissionTypesAsync(IReadOnlyList<TransmissionType> types, CancellationToken ct = default)
    {
        var entities = types.Select(TransmissionTypeEntity.FromDomain).ToList();
        await _db.Connection.RunInTransactionAsync(conn =>
        {
            foreach (var e in entities)
                conn.InsertOrReplace(e);
        });
    }

    public async Task<IReadOnlyList<TransmissionType>> GetTransmissionTypesAsync(string manualId, CancellationToken ct = default)
    {
        var entities = await _db.Connection.Table<TransmissionTypeEntity>()
            .Where(e => e.ManualId == manualId)
            .ToListAsync();
        return entities.Select(e => e.ToDomain()).ToList();
    }
}
