using FluentAssertions;
using PartsCopilot.Data;
using SQLite;
using Xunit;

namespace PartsCopilot.Tests;

public class DatabaseMigratorTests : IDisposable
{
    private readonly string _dbPath;
    private readonly SQLiteAsyncConnection _db;

    public DatabaseMigratorTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"migration_test_{Guid.NewGuid()}.db3");
        _db = new SQLiteAsyncConnection(_dbPath);
    }

    public void Dispose()
    {
        _db.CloseAsync().GetAwaiter().GetResult();
        if (File.Exists(_dbPath)) File.Delete(_dbPath);
    }

    [Fact]
    public async Task MigrateAsync_FreshDatabase_CreatesAllTables()
    {
        var migrator = new DatabaseMigrator(_db);

        await migrator.MigrateAsync();

        migrator.CurrentVersion.Should().Be(migrator.TargetVersion, "should apply all migrations");

        // Verify tables exist by querying them
        var manuals = await _db.Table<ManualEntity>().CountAsync();
        var parts = await _db.Table<PartEntity>().CountAsync();
        var pages = await _db.Table<PageEntity>().CountAsync();
        var groups = await _db.Table<IllustrationGroupEntity>().CountAsync();
        var history = await _db.Table<SearchHistoryEntity>().CountAsync();
        var favs = await _db.Table<FavoriteEntity>().CountAsync();

        manuals.Should().Be(0);
        parts.Should().Be(0);
        pages.Should().Be(0);
        groups.Should().Be(0);
        history.Should().Be(0);
        favs.Should().Be(0);
    }

    [Fact]
    public async Task MigrateAsync_SetsSchemaVersion()
    {
        var migrator = new DatabaseMigrator(_db);

        await migrator.MigrateAsync();

        var versionEntry = await _db.Table<SchemaVersionEntry>().FirstOrDefaultAsync();
        versionEntry.Should().NotBeNull();
        versionEntry!.Version.Should().Be(migrator.TargetVersion, "should apply all migrations");
    }

    [Fact]
    public async Task MigrateAsync_LogsMigrationHistory()
    {
        var migrator = new DatabaseMigrator(_db);

        await migrator.MigrateAsync();

        var logs = await migrator.GetMigrationHistoryAsync();
        logs.Should().HaveCount(migrator.TargetVersion, "one log entry per migration");
        logs[0].Version.Should().Be(1);
        logs[0].Name.Should().Be("Baseline schema");
        logs[0].Success.Should().BeTrue();
        logs[0].ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task MigrateAsync_Idempotent_DoesNotReapply()
    {
        var migrator = new DatabaseMigrator(_db);

        await migrator.MigrateAsync();
        await migrator.MigrateAsync(); // second run

        var logs = await migrator.GetMigrationHistoryAsync();
        logs.Should().HaveCount(migrator.TargetVersion, "migrations should only run once each");
    }

    [Fact]
    public async Task MigrateAsync_PreservesExistingData()
    {
        var migrator = new DatabaseMigrator(_db);
        await migrator.MigrateAsync();

        // Insert some data
        await _db.InsertAsync(new ManualEntity
        {
            Id = "test-manual",
            Title = "Test Manual",
            FilePath = "test.pdf",
            PageCount = 10,
            PartCount = 5,
            ImportedAt = DateTime.UtcNow
        });

        // Run migrations again — data should survive
        var migrator2 = new DatabaseMigrator(_db);
        await migrator2.MigrateAsync();

        var manual = await _db.FindAsync<ManualEntity>("test-manual");
        manual.Should().NotBeNull();
        manual!.Title.Should().Be("Test Manual");
    }

    [Fact]
    public async Task MigrateAsync_TracksVersionCorrectly()
    {
        var migrator = new DatabaseMigrator(_db);

        migrator.CurrentVersion.Should().Be(0, "no migrations applied yet");
        migrator.TargetVersion.Should().BeGreaterThanOrEqualTo(1);

        await migrator.MigrateAsync();

        migrator.CurrentVersion.Should().Be(migrator.TargetVersion);
    }

    [Fact]
    public async Task AppDatabase_InitializeAsync_RunsMigrations()
    {
        var appDb = new AppDatabase(_dbPath);
        await appDb.InitializeAsync();

        appDb.Migrator.CurrentVersion.Should().Be(appDb.Migrator.TargetVersion, "should apply all migrations");

        // Tables should be usable
        var count = await appDb.Connection.Table<PartEntity>().CountAsync();
        count.Should().Be(0);
    }

    [Fact]
    public async Task GetMigrationHistoryAsync_ReturnsOrderedEntries()
    {
        var migrator = new DatabaseMigrator(_db);
        await migrator.MigrateAsync();

        var history = await migrator.GetMigrationHistoryAsync();

        history.Should().BeInAscendingOrder(e => e.Version);
        history.All(e => e.DurationMs >= 0).Should().BeTrue();
    }
}
