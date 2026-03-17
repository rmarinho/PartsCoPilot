using Xunit;
using PartsCopilot.Data;
using PartsCopilot.Models;
using FluentAssertions;

namespace PartsCopilot.Tests;

/// <summary>
/// PartsRepository CRUD tests using in-memory SQLite.
/// </summary>
public class PartsRepositoryTests : IAsyncLifetime, IDisposable
{
    private readonly string _dbPath;
    private readonly AppDatabase _db;
    private readonly PartsRepository _repo;

    public PartsRepositoryTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"partscopilot_test_{Guid.NewGuid():N}.db3");
        _db = new AppDatabase(_dbPath);
        _repo = new PartsRepository(_db);
    }

    public async Task InitializeAsync() => await _db.InitializeAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    public void Dispose()
    {
        try { File.Delete(_dbPath); } catch { }
    }

    [Fact]
    public async Task SaveAndGetManual_RoundTrips()
    {
        var manual = new ManualMetadata
        {
            Id = "m1",
            Title = "Test Manual",
            FilePath = "/path/to/test.pdf",
            VehicleMake = "Porsche",
            VehicleModel = "911",
        };

        await _repo.SaveManualAsync(manual);
        var loaded = await _repo.GetManualAsync("m1");

        loaded.Should().NotBeNull();
        loaded!.Title.Should().Be("Test Manual");
        loaded.VehicleMake.Should().Be("Porsche");
    }

    [Fact]
    public async Task GetManual_ReturnsNullForMissing()
    {
        var loaded = await _repo.GetManualAsync("nonexistent");
        loaded.Should().BeNull();
    }

    [Fact]
    public async Task SaveAndGetParts_RoundTrips()
    {
        await SeedManual("m1");

        var parts = new List<PartRecord>
        {
            MakePart("1", "901 107 751 00", "Oil thermostat", "m1"),
            MakePart("2", "901 107 005 03", "Oil pump", "m1"),
        };

        await _repo.SavePartsAsync(parts);
        var loaded = await _repo.GetPartsByManualAsync("m1");

        loaded.Should().HaveCount(2);
        loaded.Should().Contain(p => p.PartNumber == "901 107 751 00");
    }

    [Fact]
    public async Task GetPart_ReturnsSpecificPart()
    {
        await SeedManual("m1");
        var part = MakePart("1", "901 107 751 00", "Oil thermostat", "m1");
        await _repo.SavePartsAsync([part]);

        var loaded = await _repo.GetPartAsync(part.Id);

        loaded.Should().NotBeNull();
        loaded!.Description.Should().Be("Oil thermostat");
    }

    [Fact]
    public async Task SearchParts_FindsByExactPartNumber()
    {
        await SeedManual("m1");
        await _repo.SavePartsAsync([MakePart("58", "901 107 751 00", "Oil thermostat", "m1")]);

        var results = await _repo.SearchPartsAsync("901 107 751 00");

        results.Should().HaveCount(1);
        results[0].Description.Should().Be("Oil thermostat");
    }

    [Fact]
    public async Task SearchParts_FindsByDescription()
    {
        await SeedManual("m1");
        await _repo.SavePartsAsync([
            MakePart("58", "901 107 751 00", "Oil thermostat", "m1"),
            MakePart("1", "901 107 005 03", "Oil pump", "m1"),
        ]);

        var results = await _repo.SearchPartsAsync("oil");

        results.Should().HaveCountGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task SearchParts_Pagination_RespectsPageSize()
    {
        await SeedManual("m1");
        var parts = Enumerable.Range(1, 10)
            .Select(i => MakePart(i.ToString(), $"900 100 00{i:D2} 00", $"Widget {i}", "m1"))
            .ToList();
        await _repo.SavePartsAsync(parts);

        var page1 = await _repo.SearchPartsAsync("widget", "m1", pageSize: 3, offset: 0);
        var page2 = await _repo.SearchPartsAsync("widget", "m1", pageSize: 3, offset: 3);

        page1.Should().HaveCount(3);
        page2.Should().HaveCount(3);
        page1.Select(p => p.Id).Should().NotIntersectWith(page2.Select(p => p.Id));
    }

    [Fact]
    public async Task SearchParts_Pagination_OffsetBeyondResults_ReturnsEmpty()
    {
        await SeedManual("m1");
        await _repo.SavePartsAsync([MakePart("1", "901 107 751 00", "Oil thermostat", "m1")]);

        var results = await _repo.SearchPartsAsync("oil", "m1", pageSize: 10, offset: 100);

        results.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchParts_SqlLevel_DoesNotLoadAllParts()
    {
        await SeedManual("m1");
        // Seed 200 parts but only 5 match "brake"
        var brakeParts = Enumerable.Range(1, 5)
            .Select(i => MakePart($"b{i}", $"901 200 00{i:D2} 00", $"Brake pad {i}", "m1"))
            .ToList();
        var otherParts = Enumerable.Range(1, 195)
            .Select(i => MakePart($"o{i}", $"901 300 00{i:D3} 0", $"Gasket {i}", "m1"))
            .ToList();
        await _repo.SavePartsAsync(brakeParts.Concat(otherParts).ToList());

        var results = await _repo.SearchPartsAsync("brake", "m1");

        results.Should().HaveCount(5, "only brake parts should match, not all 200");
    }

    [Fact]
    public async Task SearchParts_ManualIdFilter_WorksAtSqlLevel()
    {
        await SeedManual("m1");
        await SeedManual("m2");
        await _repo.SavePartsAsync([
            MakePart("1", "901 107 751 00", "Oil thermostat", "m1"),
            MakePart("2", "901 107 751 01", "Oil thermostat", "m2"),
        ]);

        var results = await _repo.SearchPartsAsync("oil", "m1");

        results.Should().HaveCount(1);
        results[0].ManualId.Should().Be("m1");
    }

    [Fact]
    public async Task SearchParts_ExactMatch_AlsoFiltersByManualId()
    {
        await SeedManual("m1");
        await SeedManual("m2");
        await _repo.SavePartsAsync([
            MakePart("1", "901 107 751 00", "Oil thermostat", "m1"),
            MakePart("2", "901 107 751 00", "Oil thermostat variant", "m2"),
        ]);

        var results = await _repo.SearchPartsAsync("901 107 751 00", "m1");

        results.Should().HaveCount(1);
        results[0].ManualId.Should().Be("m1");
    }

    [Fact]
    public async Task DeleteManual_CascadesToRelatedData()
    {
        await SeedManual("m1");
        await _repo.SavePartsAsync([MakePart("1", "901 107 751 00", "Oil thermostat", "m1")]);
        await _repo.SavePagesAsync([new ManualPage { ManualId = "m1", PageNumber = 42, RawText = "test", PageType = "part_table" }]);

        await _repo.DeleteManualAsync("m1");

        var manual = await _repo.GetManualAsync("m1");
        var parts = await _repo.GetPartsByManualAsync("m1");
        manual.Should().BeNull();
        parts.Should().BeEmpty();
    }

    [Fact]
    public async Task SaveAndGetPages_RoundTrips()
    {
        await SeedManual("m1");

        var pages = new List<ManualPage>
        {
            new() { ManualId = "m1", PageNumber = 10, RawText = "Illustration: 101-00\nCrankcase", Illustration = "101-00", PageType = "part_table" },
            new() { ManualId = "m1", PageNumber = 42, RawText = "Illustration: 107-00\nLubrication", Illustration = "107-00", PageType = "part_table" },
        };

        await _repo.SavePagesAsync(pages);
        var loaded = await _repo.GetPageAsync("m1", 42);

        loaded.Should().NotBeNull();
        loaded!.Illustration.Should().Be("107-00");
    }

    [Fact]
    public async Task SaveAndGetLegendEntries_RoundTrips()
    {
        await SeedManual("m1");

        var entries = new List<LegendEntry>
        {
            new() { ManualId = "m1", Code = "A", Description = "All 911 models", ApplicableModels = "911" },
        };

        await _repo.SaveLegendEntriesAsync(entries);
        var loaded = await _repo.GetLegendEntriesAsync("m1");

        loaded.Should().HaveCount(1);
        loaded[0].Code.Should().Be("A");
    }

    [Fact]
    public async Task SaveAndGetVehicleTypes_RoundTrips()
    {
        await SeedManual("m1");

        var types = new List<VehicleType>
        {
            new() { ManualId = "m1", Code = "901", ModelName = "911", YearFrom = 1965, YearTo = 1969 },
        };

        await _repo.SaveVehicleTypesAsync(types);
        var loaded = await _repo.GetVehicleTypesAsync("m1");

        loaded.Should().HaveCount(1);
        loaded[0].ModelName.Should().Be("911");
    }

    [Fact]
    public async Task SaveAndGetEngineTypes_RoundTrips()
    {
        await SeedManual("m1");

        var types = new List<EngineType>
        {
            new() { ManualId = "m1", Code = "901/01", EngineName = "Flat-6 2.0L" },
        };

        await _repo.SaveEngineTypesAsync(types);
        var loaded = await _repo.GetEngineTypesAsync("m1");

        loaded.Should().HaveCount(1);
        loaded[0].EngineName.Should().Be("Flat-6 2.0L");
    }

    [Fact]
    public async Task SaveAndGetTransmissionTypes_RoundTrips()
    {
        await SeedManual("m1");

        var types = new List<TransmissionType>
        {
            new() { ManualId = "m1", Code = "901/0", TransmissionName = "4-speed manual" },
        };

        await _repo.SaveTransmissionTypesAsync(types);
        var loaded = await _repo.GetTransmissionTypesAsync("m1");

        loaded.Should().HaveCount(1);
        loaded[0].TransmissionName.Should().Be("4-speed manual");
    }

    [Fact]
    public async Task SeedDataService_SeedsAllTables()
    {
        var seed = new SeedDataService(_db, _repo);
        await seed.SeedAsync();

        var manuals = await _repo.GetAllManualsAsync();
        var parts = await _repo.GetPartsByManualAsync("seed-911-912-1965-1969");
        var vehicles = await _repo.GetVehicleTypesAsync("seed-911-912-1965-1969");
        var engines = await _repo.GetEngineTypesAsync("seed-911-912-1965-1969");
        var transmissions = await _repo.GetTransmissionTypesAsync("seed-911-912-1965-1969");
        var legends = await _repo.GetLegendEntriesAsync("seed-911-912-1965-1969");

        manuals.Should().HaveCount(1);
        parts.Should().HaveCountGreaterThanOrEqualTo(20);
        vehicles.Should().HaveCountGreaterThanOrEqualTo(5);
        engines.Should().HaveCountGreaterThanOrEqualTo(3);
        transmissions.Should().HaveCountGreaterThanOrEqualTo(3);
        legends.Should().HaveCountGreaterThanOrEqualTo(3);
    }

    [Fact]
    public async Task SeedDataService_IsIdempotent()
    {
        var seed = new SeedDataService(_db, _repo);

        await seed.SeedIfEmptyAsync();
        await seed.SeedIfEmptyAsync();

        var manuals = await _repo.GetAllManualsAsync();
        manuals.Should().HaveCount(1, "second seed should be skipped");
    }

    private async Task SeedManual(string id)
    {
        await _repo.SaveManualAsync(new ManualMetadata { Id = id, Title = "Test", FilePath = "test://m.pdf" });
    }

    private static PartRecord MakePart(string pos, string pn, string desc, string manualId)
    {
        var normalized = pn.Replace(" ", "").ToUpperInvariant();
        return new PartRecord
        {
            ManualId = manualId, Position = pos, PartNumber = pn,
            PartNumberNormalized = normalized, Description = desc,
            SearchText = $"{pn} {normalized} {desc}".ToLowerInvariant(),
            Illustration = "107-00", PageNumber = 42
        };
    }
}
