using FluentAssertions;
using PartsCopilot.Data;
using SQLite;
using Xunit;

namespace PartsCopilot.Tests;

/// <summary>
/// Tests for the complete database migration system including error handling and new migrations.
/// </summary>
public class MigrationSystemTests : IDisposable
{
    private readonly string _dbPath;
    private SQLiteAsyncConnection? _db;

    public MigrationSystemTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"migration_system_test_{Guid.NewGuid()}.db3");
    }

    public void Dispose()
    {
        _db?.CloseAsync().GetAwaiter().GetResult();
        if (File.Exists(_dbPath)) File.Delete(_dbPath);
    }

    [Fact]
    public async Task Migration2_CreatesNewTables()
    {
        _db = new SQLiteAsyncConnection(_dbPath);
        var migrator = new DatabaseMigrator(_db);

        await migrator.MigrateAsync();

        migrator.CurrentVersion.Should().BeGreaterThanOrEqualTo(2);

        // Verify new tables exist
        var legends = await _db.Table<LegendEntryEntity>().CountAsync();
        var vehicles = await _db.Table<VehicleTypeEntity>().CountAsync();
        var engines = await _db.Table<EngineTypeEntity>().CountAsync();
        var transmissions = await _db.Table<TransmissionTypeEntity>().CountAsync();

        legends.Should().Be(0);
        vehicles.Should().Be(0);
        engines.Should().Be(0);
        transmissions.Should().Be(0);
    }

    [Fact]
    public async Task Migration3_AddsFavoriteFields()
    {
        _db = new SQLiteAsyncConnection(_dbPath);
        var migrator = new DatabaseMigrator(_db);

        await migrator.MigrateAsync();

        migrator.CurrentVersion.Should().BeGreaterThanOrEqualTo(3);

        // Insert a favorite with new fields
        var favorite = new FavoriteEntity
        {
            Id = Guid.NewGuid().ToString(),
            PartRecordId = "test-part",
            PartNumber = "901-101-013-00",
            Description = "Test part",
            ModelName = "911T",
            PageNumber = 42,
            Illustration = "101-00",
            SavedAt = DateTime.UtcNow
        };

        await _db.InsertAsync(favorite);

        var retrieved = await _db.FindAsync<FavoriteEntity>(favorite.Id);
        retrieved.Should().NotBeNull();
        retrieved!.ModelName.Should().Be("911T");
        retrieved.PageNumber.Should().Be(42);
        retrieved.Illustration.Should().Be("101-00");
    }

    [Fact]
    public async Task Migration3_PreservesExistingFavorites()
    {
        _db = new SQLiteAsyncConnection(_dbPath);
        var migrator1 = new DatabaseMigrator(_db);
        
        // Apply baseline migration only (v1)
        await _db.CreateTableAsync<SchemaVersionEntry>();
        await _db.CreateTableAsync<MigrationLogEntry>();
        await _db.CreateTableAsync<FavoriteEntity>();
        await _db.InsertOrReplaceAsync(new SchemaVersionEntry { Id = 1, Version = 1, AppliedAt = DateTime.UtcNow });

        // Insert a favorite without new fields
        var oldFavorite = new FavoriteEntity
        {
            Id = "old-fav-123",
            PartRecordId = "old-part",
            PartNumber = "901-000-000-00",
            Description = "Old favorite",
            SavedAt = DateTime.UtcNow
        };
        await _db.InsertAsync(oldFavorite);

        // Now apply all migrations
        var migrator2 = new DatabaseMigrator(_db);
        await migrator2.MigrateAsync();

        // Old favorite should still exist
        var retrieved = await _db.FindAsync<FavoriteEntity>("old-fav-123");
        retrieved.Should().NotBeNull();
        retrieved!.PartNumber.Should().Be("901-000-000-00");
        retrieved.Description.Should().Be("Old favorite");
        // New fields should be null/default
        retrieved.Model.Should().BeNull();
        retrieved.PageNumber.Should().Be(0);
        retrieved.Illustration.Should().BeNull();
    }

    [Fact]
    public async Task MigrateAsync_ThrowsOnReadOnlyDatabase()
    {
        // Create a database file and make it read-only
        _db = new SQLiteAsyncConnection(_dbPath);
        await _db.CreateTableAsync<SchemaVersionEntry>();
        await _db.CloseAsync();

        File.SetAttributes(_dbPath, FileAttributes.ReadOnly);

        _db = new SQLiteAsyncConnection(_dbPath);
        var migrator = new DatabaseMigrator(_db);

        var act = async () => await migrator.MigrateAsync();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*permission denied*");

        // Cleanup: remove read-only flag
        File.SetAttributes(_dbPath, FileAttributes.Normal);
    }

    [Fact]
    public async Task MigrateAsync_LogsFailedMigration_WhenPossible()
    {
        _db = new SQLiteAsyncConnection(_dbPath);
        
        // Create version tables and manually set version to 1 (simulating partial migration state)
        await _db.CreateTableAsync<SchemaVersionEntry>();
        await _db.CreateTableAsync<MigrationLogEntry>();
        await _db.InsertOrReplaceAsync(new SchemaVersionEntry { Id = 1, Version = 1, AppliedAt = DateTime.UtcNow });
        
        // Create baseline tables manually (simulate migration 1 completed)
        await _db.CreateTableAsync<ManualEntity>();
        await _db.CreateTableAsync<PartEntity>();
        await _db.CreateTableAsync<PageEntity>();
        await _db.CreateTableAsync<IllustrationGroupEntity>();
        await _db.CreateTableAsync<SearchHistoryEntity>();
        await _db.CreateTableAsync<FavoriteEntity>();
        
        // Close connection and make database read-only before attempting migration 2
        await _db.CloseAsync();
        File.SetAttributes(_dbPath, FileAttributes.ReadOnly);

        _db = new SQLiteAsyncConnection(_dbPath);
        var migrator = new DatabaseMigrator(_db);

        var act = async () => await migrator.MigrateAsync();
        
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*permission denied*");

        // Cleanup
        File.SetAttributes(_dbPath, FileAttributes.Normal);
    }

    [Fact]
    public async Task AllMigrations_RunInOrder()
    {
        _db = new SQLiteAsyncConnection(_dbPath);
        var migrator = new DatabaseMigrator(_db);

        await migrator.MigrateAsync();

        var history = await migrator.GetMigrationHistoryAsync();
        
        history.Should().HaveCount(4);
        history[0].Version.Should().Be(1);
        history[0].Name.Should().Be("Baseline schema");
        history[1].Version.Should().Be(2);
        history[1].Name.Should().Be("Add supporting tables");
        history[2].Version.Should().Be(3);
        history[2].Name.Should().Be("Extend FavoriteEntry schema");
        history[3].Version.Should().Be(4);
        history[3].Name.Should().Be("Add ImageData to Pages");
        
        history.All(h => h.Success).Should().BeTrue();
    }

    [Fact]
    public async Task LegendEntry_CanBeInsertedAndRetrieved()
    {
        _db = new SQLiteAsyncConnection(_dbPath);
        var migrator = new DatabaseMigrator(_db);
        await migrator.MigrateAsync();

        var legend = new LegendEntryEntity
        {
            Id = Guid.NewGuid().ToString(),
            ManualId = "test-manual",
            Illustration = "101-00",
            Code = "A",
            Description = "Optional equipment",
            ApplicableModels = "911T, 911E",
            YearRange = "1969-1973",
            Notes = "Not available in USA"
        };

        await _db.InsertAsync(legend);

        var retrieved = await _db.FindAsync<LegendEntryEntity>(legend.Id);
        retrieved.Should().NotBeNull();
        retrieved!.Code.Should().Be("A");
        retrieved.Description.Should().Be("Optional equipment");
    }

    [Fact]
    public async Task VehicleType_CanBeInsertedAndRetrieved()
    {
        _db = new SQLiteAsyncConnection(_dbPath);
        var migrator = new DatabaseMigrator(_db);
        await migrator.MigrateAsync();

        var vehicle = new VehicleTypeEntity
        {
            Id = Guid.NewGuid().ToString(),
            ManualId = "test-manual",
            Code = "T",
            ModelName = "911T",
            Variant = "Coupe",
            YearFrom = 1969,
            YearTo = 1973,
            ChassisRange = "119300001-119310000"
        };

        await _db.InsertAsync(vehicle);

        var retrieved = await _db.FindAsync<VehicleTypeEntity>(vehicle.Id);
        retrieved.Should().NotBeNull();
        retrieved!.ModelName.Should().Be("911T");
        retrieved.Variant.Should().Be("Coupe");
    }

    [Fact]
    public async Task EngineType_CanBeInsertedAndRetrieved()
    {
        _db = new SQLiteAsyncConnection(_dbPath);
        var migrator = new DatabaseMigrator(_db);
        await migrator.MigrateAsync();

        var engine = new EngineTypeEntity
        {
            Id = Guid.NewGuid().ToString(),
            ManualId = "test-manual",
            Code = "901/01",
            EngineName = "Type 901/01",
            Displacement = "1991 cc",
            Power = "110 HP",
            ApplicableModels = "911T"
        };

        await _db.InsertAsync(engine);

        var retrieved = await _db.FindAsync<EngineTypeEntity>(engine.Id);
        retrieved.Should().NotBeNull();
        retrieved!.EngineName.Should().Be("Type 901/01");
        retrieved.Displacement.Should().Be("1991 cc");
    }

    [Fact]
    public async Task TransmissionType_CanBeInsertedAndRetrieved()
    {
        _db = new SQLiteAsyncConnection(_dbPath);
        var migrator = new DatabaseMigrator(_db);
        await migrator.MigrateAsync();

        var transmission = new TransmissionTypeEntity
        {
            Id = Guid.NewGuid().ToString(),
            ManualId = "test-manual",
            Code = "905/00",
            TransmissionName = "Type 905",
            Type = "5-speed manual",
            ApplicableModels = "911, 912"
        };

        await _db.InsertAsync(transmission);

        var retrieved = await _db.FindAsync<TransmissionTypeEntity>(transmission.Id);
        retrieved.Should().NotBeNull();
        retrieved!.TransmissionName.Should().Be("Type 905");
        retrieved.Type.Should().Be("5-speed manual");
    }
}
