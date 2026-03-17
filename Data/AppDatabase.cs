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
        await _db.CreateTableAsync<SearchHistoryEntity>();
        await _db.CreateTableAsync<FavoriteEntity>();
    }
}
