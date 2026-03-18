using SQLite;

namespace PartsCopilot.Data;

public class AppDatabase
{
    private readonly SQLiteAsyncConnection _db;
    private readonly DatabaseMigrator _migrator;

    public AppDatabase(string dbPath)
    {
        _db = new SQLiteAsyncConnection(dbPath);
        _migrator = new DatabaseMigrator(_db);
    }

    public SQLiteAsyncConnection Connection => _db;
    public DatabaseMigrator Migrator => _migrator;

    /// <summary>
    /// Runs all pending migrations, then returns.
    /// Tables are created by the migration system — no direct CreateTable calls here.
    /// </summary>
    public async Task InitializeAsync()
    {
        await _migrator.MigrateAsync();
    }
}
