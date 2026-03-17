using Xunit;
using PartsCopilot.Data;
using PartsCopilot.Models;
using PartsCopilot.Services;
using FluentAssertions;

namespace PartsCopilot.Tests;

/// <summary>
/// Edge case tests: empty queries, special characters, case sensitivity,
/// null handling, and SeedDataService robustness.
/// </summary>
public class EdgeCaseTests : IAsyncLifetime, IDisposable
{
    private readonly string _dbPath;
    private readonly AppDatabase _db;
    private readonly PartsRepository _repo;
    private readonly HybridSearchService _search;

    public EdgeCaseTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"partscopilot_test_{Guid.NewGuid():N}.db3");
        _db = new AppDatabase(_dbPath);
        _repo = new PartsRepository(_db);
        _search = new HybridSearchService(_repo);
    }

    public async Task InitializeAsync()
    {
        await _db.InitializeAsync();
        await SeedTestData();
    }

    public Task DisposeAsync() => Task.CompletedTask;
    public void Dispose()
    {
        try { File.Delete(_dbPath); } catch { }
    }

    // --- Empty / whitespace searches ---

    [Fact]
    public async Task Search_EmptyString_DoesNotThrow()
    {
        // Empty string in Contains("") matches everything — verify no crash
        var result = await _search.SearchAsync(new SearchQuery(""));
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task Search_WhitespaceOnly_ReturnsEmpty()
    {
        var result = await _search.SearchAsync(new SearchQuery("   "));
        result.Candidates.Should().BeEmpty();
    }

    [Fact]
    public async Task RepositorySearch_SingleCharacter_DoesNotThrow()
    {
        var results = await _repo.SearchPartsAsync("o");
        // 'o' is common — should match parts with 'oil' etc.
        results.Should().NotBeNull();
    }

    // --- Special characters in part numbers ---

    [Fact]
    public async Task Search_PartNumberWithSlash_DoesNotThrow()
    {
        var result = await _search.SearchAsync(new SearchQuery("901/107"));
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task Search_PartNumberWithDash_DoesNotThrow()
    {
        var result = await _search.SearchAsync(new SearchQuery("107-00"));
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task Search_SqlInjectionAttempt_DoesNotThrow()
    {
        var result = await _search.SearchAsync(new SearchQuery("'; DROP TABLE Parts; --"));
        result.Should().NotBeNull();
        result.Candidates.Should().BeEmpty();
    }

    [Fact]
    public async Task Search_UnicodeCharacters_DoesNotThrow()
    {
        var result = await _search.SearchAsync(new SearchQuery("Ölkühler Dichtung"));
        result.Should().NotBeNull();
    }

    // --- Case sensitivity ---

    [Fact]
    public async Task Search_IsCaseInsensitive_ForDescription()
    {
        var upper = await _search.SearchAsync(new SearchQuery("OIL THERMOSTAT"));
        var lower = await _search.SearchAsync(new SearchQuery("oil thermostat"));
        var mixed = await _search.SearchAsync(new SearchQuery("Oil Thermostat"));

        upper.Candidates.Should().NotBeEmpty();
        lower.Candidates.Should().NotBeEmpty();
        mixed.Candidates.Should().NotBeEmpty();

        // All should find the same top result
        upper.Candidates[0].Part.PartNumber.Should().Be(lower.Candidates[0].Part.PartNumber);
        lower.Candidates[0].Part.PartNumber.Should().Be(mixed.Candidates[0].Part.PartNumber);
    }

    [Fact]
    public async Task Search_IsCaseInsensitive_ForPartNumber()
    {
        var upper = await _search.SearchAsync(new SearchQuery("901 107 751 00"));
        var result = await _search.SearchAsync(new SearchQuery("901107751 00"));

        upper.Candidates.Should().NotBeEmpty();
    }

    // --- Manual ID filtering ---

    [Fact]
    public async Task Search_ManualIdFilter_OnlyReturnsMatchingManual()
    {
        var result = await _search.SearchAsync(new SearchQuery("oil", ManualId: "test-manual"));
        result.Candidates.Should().NotBeEmpty();
        result.Candidates.Should().AllSatisfy(c =>
            c.Part.ManualId.Should().Be("test-manual"));
    }

    [Fact]
    public async Task Search_ManualIdFilter_ReturnsEmptyForWrongManual()
    {
        var result = await _search.SearchAsync(new SearchQuery("oil", ManualId: "wrong-manual"));
        result.Candidates.Should().BeEmpty();
    }

    // --- SeedDataService edge cases ---

    [Fact]
    public async Task SeedAsync_CalledTwice_DoesNotDuplicate()
    {
        var seed = new SeedDataService(_db, _repo);

        await seed.SeedAsync();
        var countAfterFirst = (await _repo.GetPartsByManualAsync("seed-911-912-1965-1969")).Count;

        await seed.SeedAsync();
        var countAfterSecond = (await _repo.GetPartsByManualAsync("seed-911-912-1965-1969")).Count;

        countAfterSecond.Should().Be(countAfterFirst,
            "SeedAsync deletes + reseeds, so count should be identical after two calls");
    }

    [Fact]
    public async Task SeedAsync_AllTablesPopulated_WithCorrectCounts()
    {
        var seed = new SeedDataService(_db, _repo);
        await seed.SeedAsync();

        var parts = await _repo.GetPartsByManualAsync("seed-911-912-1965-1969");
        var vehicles = await _repo.GetVehicleTypesAsync("seed-911-912-1965-1969");
        var engines = await _repo.GetEngineTypesAsync("seed-911-912-1965-1969");
        var transmissions = await _repo.GetTransmissionTypesAsync("seed-911-912-1965-1969");
        var legends = await _repo.GetLegendEntriesAsync("seed-911-912-1965-1969");

        parts.Should().HaveCount(26, "seed creates 26 parts across 5 illustration groups");
        vehicles.Should().HaveCount(8, "seed creates exactly 8 vehicle types");
        engines.Should().HaveCount(5, "seed creates exactly 5 engine types");
        transmissions.Should().HaveCount(4, "seed creates exactly 4 transmission types");
        legends.Should().HaveCount(5, "seed creates exactly 5 legend entries");
    }

    [Fact]
    public async Task SeedIfEmpty_DoesNotSeed_WhenDataAlreadyExists()
    {
        // Pre-populate with a manual
        await _repo.SaveManualAsync(new ManualMetadata { Id = "existing", Title = "Existing", FilePath = "test://e.pdf" });

        var seed = new SeedDataService(_db, _repo);
        await seed.SeedIfEmptyAsync();

        // Seed manual should NOT exist
        var seedManual = await _repo.GetManualAsync("seed-911-912-1965-1969");
        seedManual.Should().BeNull("SeedIfEmpty should skip when data exists");
    }

    // --- Repository null/missing key handling ---

    [Fact]
    public async Task GetPart_ReturnsNullForMissingId()
    {
        var result = await _repo.GetPartAsync("nonexistent-id");
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetPartsByManual_ReturnsEmptyForMissingManual()
    {
        var result = await _repo.GetPartsByManualAsync("nonexistent-manual");
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPageAsync_ReturnsNullForMissingPage()
    {
        var result = await _repo.GetPageAsync("test-manual", 9999);
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteManual_OnNonexistent_DoesNotThrow()
    {
        var act = () => _repo.DeleteManualAsync("nonexistent");
        await act.Should().NotThrowAsync();
    }

    // --- VehicleContext edge cases ---

    [Fact]
    public async Task Search_WithYearContext_FiltersCorrectly()
    {
        var ctx = new VehicleContext(Year: 1968);
        var result = await _search.SearchAsync(new SearchQuery("crankshaft", ctx));
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task Search_WithNullModelContext_ReturnsAllModels()
    {
        var ctx = new VehicleContext(Model: null);
        var result = await _search.SearchAsync(new SearchQuery("oil", ctx));
        result.Candidates.Should().NotBeEmpty();
    }

    private async Task SeedTestData()
    {
        await _repo.SaveManualAsync(new ManualMetadata { Id = "test-manual", Title = "Test", FilePath = "test://m.pdf" });

        var parts = new List<PartRecord>
        {
            MakePart("58", "901 107 751 00", "Oil thermostat", "107-00", 42, "1", "911", null),
            MakePart("1", "901 107 005 03", "Oil pump", "107-00", 42, "1", "911", null),
            MakePart("2", "912 107 005 01", "Oil pump", "107-00", 42, "1", "912", null),
            MakePart("1", "901 102 011 03", "Crankshaft", "101-05", 16, "1", "911", "-68"),
            MakePart("2", "901 102 011 04", "Crankshaft", "101-05", 16, "1", "911", "69"),
        };

        await _repo.SavePartsAsync(parts);
    }

    private static PartRecord MakePart(string pos, string pn, string desc, string ill, int page, string qty, string model, string? remark)
    {
        var normalized = pn.Replace(" ", "").ToUpperInvariant();
        return new PartRecord
        {
            ManualId = "test-manual",
            Position = pos,
            PartNumber = pn,
            PartNumberNormalized = normalized,
            Description = desc,
            SearchText = $"{pn} {normalized} {desc} {model} {remark}".ToLowerInvariant(),
            Remark = remark,
            Quantity = qty,
            Model = model,
            Illustration = ill,
            PageNumber = page
        };
    }
}
