using SQLite;

namespace PartsCopilot.Data;

public class AppDatabase
{
    private readonly SQLiteAsyncConnection _db;

    public AppDatabase(string dbPath)
    {
        _db = new SQLiteAsyncConnection(dbPath);
    }

    public SQLiteAsyncConnection Connection => _db;

    public async Task InitializeAsync()
    {
        await _db.CreateTableAsync<ManualEntity>();
        await _db.CreateTableAsync<PartEntity>();
        await _db.CreateTableAsync<PageEntity>();
        await _db.CreateTableAsync<IllustrationGroupEntity>();
        await _db.CreateTableAsync<LegendEntryEntity>();
        await _db.CreateTableAsync<VehicleTypeEntity>();
        await _db.CreateTableAsync<EngineTypeEntity>();
        await _db.CreateTableAsync<TransmissionTypeEntity>();
        await _db.CreateTableAsync<SearchHistoryEntity>();
        await _db.CreateTableAsync<FavoriteEntity>();

        // Composite index for search performance: manual-scoped text search
        await _db.ExecuteAsync(
            "CREATE INDEX IF NOT EXISTS IX_Parts_ManualId_SearchText ON Parts (ManualId, SearchText)");
    }
}
