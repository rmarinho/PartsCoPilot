using SQLite;

namespace PartsCopilot.Data;

/// <summary>
/// Tracks schema version and applies ordered migrations.
/// Migrations are idempotent — safe to re-run on an already-migrated database.
/// </summary>
public class DatabaseMigrator
{
    private readonly SQLiteAsyncConnection _db;
    private readonly List<Migration> _migrations = [];

    public DatabaseMigrator(SQLiteAsyncConnection db)
    {
        _db = db;
        RegisterMigrations();
    }

    public int CurrentVersion { get; private set; }
    public int TargetVersion => _migrations.Count;

    /// <summary>
    /// Ensures the schema_version and migration_log tables exist,
    /// reads the current version, then applies all pending migrations in order.
    /// </summary>
    public async Task MigrateAsync()
    {
        try
        {
            await EnsureVersionTablesAsync();
        }
        catch (SQLiteException ex) when (ex.Result == SQLite3.Result.Full)
        {
            throw new InvalidOperationException("Database migration failed: disk full", ex);
        }
        catch (SQLiteException ex) when (ex.Result == SQLite3.Result.CannotOpen || ex.Result == SQLite3.Result.ReadOnly)
        {
            throw new InvalidOperationException("Database migration failed: permission denied or read-only database", ex);
        }

        CurrentVersion = await GetSchemaVersionAsync();

        while (CurrentVersion < TargetVersion)
        {
            var next = _migrations[CurrentVersion]; // 0-indexed: version 0 → migration[0] brings to v1
            var startedAt = DateTime.UtcNow;

            try
            {
                await next.Apply(_db);
                CurrentVersion = next.Version;
                await SetSchemaVersionAsync(CurrentVersion);
                await LogMigrationAsync(next.Version, next.Name, startedAt, success: true);
            }
            catch (SQLiteException ex) when (ex.Result == SQLite3.Result.Full)
            {
                await LogMigrationAsync(next.Version, next.Name, startedAt, success: false, $"Disk full: {ex.Message}");
                throw new InvalidOperationException(
                    $"Migration v{next.Version} '{next.Name}' failed: disk full", ex);
            }
            catch (SQLiteException ex) when (ex.Result == SQLite3.Result.CannotOpen || ex.Result == SQLite3.Result.ReadOnly)
            {
                await LogMigrationAsync(next.Version, next.Name, startedAt, success: false, $"Permission denied: {ex.Message}");
                throw new InvalidOperationException(
                    $"Migration v{next.Version} '{next.Name}' failed: permission denied or read-only database", ex);
            }
            catch (Exception ex)
            {
                await LogMigrationAsync(next.Version, next.Name, startedAt, success: false, ex.Message);
                throw new InvalidOperationException(
                    $"Migration v{next.Version} '{next.Name}' failed: {ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// Returns the full migration history from the log table.
    /// </summary>
    public async Task<IReadOnlyList<MigrationLogEntry>> GetMigrationHistoryAsync()
    {
        await EnsureVersionTablesAsync();
        return await _db.Table<MigrationLogEntry>().OrderBy(e => e.Version).ToListAsync();
    }

    private void RegisterMigrations()
    {
        // Migration 1: Baseline — creates all current tables
        _migrations.Add(new Migration(1, "Baseline schema", async db =>
        {
            await db.CreateTableAsync<ManualEntity>();
            await db.CreateTableAsync<PartEntity>();
            await db.CreateTableAsync<PageEntity>();
            await db.CreateTableAsync<IllustrationGroupEntity>();
            await db.CreateTableAsync<SearchHistoryEntity>();
            await db.CreateTableAsync<FavoriteEntity>();
        }));

        // Migration 2: Add supporting tables (LegendEntry, VehicleType, EngineType, TransmissionType)
        _migrations.Add(new Migration(2, "Add supporting tables", async db =>
        {
            await db.CreateTableAsync<LegendEntryEntity>();
            await db.CreateTableAsync<VehicleTypeEntity>();
            await db.CreateTableAsync<EngineTypeEntity>();
            await db.CreateTableAsync<TransmissionTypeEntity>();
        }));

        // Migration 3: Add Model, PageNumber, Illustration fields to Favorites
        _migrations.Add(new Migration(3, "Extend FavoriteEntry schema", async db =>
        {
            // SQLite ALTER TABLE only supports ADD COLUMN (not DROP or RENAME)
            await db.ExecuteAsync("ALTER TABLE Favorites ADD COLUMN Model TEXT");
            await db.ExecuteAsync("ALTER TABLE Favorites ADD COLUMN PageNumber INTEGER");
            await db.ExecuteAsync("ALTER TABLE Favorites ADD COLUMN Illustration TEXT");
        }));
    }

    private async Task EnsureVersionTablesAsync()
    {
        await _db.CreateTableAsync<SchemaVersionEntry>();
        await _db.CreateTableAsync<MigrationLogEntry>();
    }

    private async Task<int> GetSchemaVersionAsync()
    {
        var entry = await _db.Table<SchemaVersionEntry>().FirstOrDefaultAsync();
        return entry?.Version ?? 0;
    }

    private async Task SetSchemaVersionAsync(int version)
    {
        var entry = new SchemaVersionEntry { Id = 1, Version = version, AppliedAt = DateTime.UtcNow };
        await _db.InsertOrReplaceAsync(entry);
    }

    private async Task LogMigrationAsync(int version, string name, DateTime startedAt, bool success, string? error = null)
    {
        await _db.InsertAsync(new MigrationLogEntry
        {
            Version = version,
            Name = name,
            AppliedAt = startedAt,
            DurationMs = (int)(DateTime.UtcNow - startedAt).TotalMilliseconds,
            Success = success,
            ErrorMessage = error
        });
    }
}

/// <summary>
/// A single schema migration step.
/// </summary>
public class Migration
{
    public int Version { get; }
    public string Name { get; }
    public Func<SQLiteAsyncConnection, Task> Apply { get; }

    public Migration(int version, string name, Func<SQLiteAsyncConnection, Task> apply)
    {
        Version = version;
        Name = name;
        Apply = apply;
    }
}

/// <summary>
/// Single-row table tracking the current schema version.
/// </summary>
[Table("schema_version")]
public class SchemaVersionEntry
{
    [PrimaryKey] public int Id { get; set; } = 1;
    public int Version { get; set; }
    public DateTime AppliedAt { get; set; }
}

/// <summary>
/// Log of all migration runs (successes and failures).
/// </summary>
[Table("migration_log")]
public class MigrationLogEntry
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    public int Version { get; set; }
    public string Name { get; set; } = "";
    public DateTime AppliedAt { get; set; }
    public int DurationMs { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}
