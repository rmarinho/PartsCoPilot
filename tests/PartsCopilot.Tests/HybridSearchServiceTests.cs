using Xunit;
using PartsCopilot.Data;
using PartsCopilot.Models;
using PartsCopilot.Services;
using FluentAssertions;

namespace PartsCopilot.Tests;

/// <summary>
/// Verifies the HybridSearchService ranking:
/// exact part number > description match > word overlap.
/// </summary>
public class HybridSearchServiceTests : IAsyncLifetime, IDisposable
{
    private readonly string _dbPath;
    private readonly AppDatabase _db;
    private readonly PartsRepository _repo;
    private readonly HybridSearchService _search;

    public HybridSearchServiceTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"partscopilot_test_{Guid.NewGuid():N}.db3");
        _db = new AppDatabase(_dbPath);
        _repo = new PartsRepository(_db);
        _search = new HybridSearchService(_repo);
    }

    public async Task InitializeAsync()
    {
        await _db.InitializeAsync();
        await SeedTestParts();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    public void Dispose()
    {
        try { File.Delete(_dbPath); } catch { }
    }

    [Fact]
    public async Task ExactPartNumber_RanksHigherThan_DescriptionMatch()
    {
        var result = await _search.SearchAsync(new SearchQuery("901 107 751 00"));

        result.Candidates.Should().NotBeEmpty();
        result.Candidates[0].Part.PartNumber.Should().Be("901 107 751 00");
        result.Candidates[0].MatchReason.Should().Contain("part number");
        result.Candidates[0].Score.Should().BeGreaterThanOrEqualTo(0.9);
    }

    [Fact]
    public async Task DescriptionMatch_RanksHigherThan_WordOverlap()
    {
        var result = await _search.SearchAsync(new SearchQuery("Oil thermostat"));

        result.Candidates.Should().NotBeEmpty();

        var oilThermostat = result.Candidates.First(c => c.Part.Description == "Oil thermostat");
        var oilPump = result.Candidates.FirstOrDefault(c => c.Part.Description == "Oil pump");

        oilThermostat.Score.Should().BeGreaterThan(oilPump?.Score ?? 0,
            "exact description match should score higher than partial word overlap");
    }

    [Fact]
    public async Task WordOverlap_ReturnsResults_WithPartialMatch()
    {
        var result = await _search.SearchAsync(new SearchQuery("oil"));

        result.Candidates.Should().HaveCountGreaterThanOrEqualTo(2,
            "both 'Oil thermostat' and 'Oil pump' contain the word 'oil'");
    }

    [Fact]
    public async Task NoMatch_ReturnsEmpty()
    {
        var result = await _search.SearchAsync(new SearchQuery("xyznonexistent"));

        result.Candidates.Should().BeEmpty();
    }

    [Fact]
    public async Task NormalizedPartNumber_MatchesWithoutSpaces()
    {
        var result = await _search.SearchAsync(new SearchQuery("90110775100"));

        result.Candidates.Should().NotBeEmpty();
        result.Candidates[0].Part.PartNumber.Should().Be("901 107 751 00");
    }

    [Fact]
    public async Task VehicleContextFilter_NarrowsByModel()
    {
        var ctx = new VehicleContext(Model: "912");
        var result = await _search.SearchAsync(new SearchQuery("oil pump", ctx));

        result.Candidates.Should().NotBeEmpty();
        result.Candidates.Should().AllSatisfy(c =>
            c.Part.Model.Should().Contain("912"));
    }

    private async Task SeedTestParts()
    {
        var manualId = "test-manual";

        await _repo.SaveManualAsync(new ManualMetadata
        {
            Id = manualId,
            Title = "Test Manual",
            FilePath = "test://manual.pdf",
        });

        var parts = new List<PartRecord>
        {
            MakePart("58", "901 107 751 00", "Oil thermostat", manualId, "107-00", 42, "1", "911", null),
            MakePart("1", "901 107 005 03", "Oil pump", manualId, "107-00", 42, "1", "911", null),
            MakePart("2", "912 107 005 01", "Oil pump", manualId, "107-00", 42, "1", "912", null),
            MakePart("60", "901 107 764 00", "Oil cooler", manualId, "107-00", 42, "1", "911", null),
            MakePart("1", "901 101 013 00", "Crankcase", manualId, "101-00", 10, "1", "911", null),
            MakePart("4", "901 108 901 00", "Fuel pump", manualId, "202-00", 60, "1", "911", null),
        };

        await _repo.SavePartsAsync(parts);
    }

    private static PartRecord MakePart(string pos, string pn, string desc, string manualId, string ill, int page, string qty, string model, string? remark)
    {
        var normalized = pn.Replace(" ", "").ToUpperInvariant();
        return new PartRecord
        {
            ManualId = manualId, Position = pos, PartNumber = pn,
            PartNumberNormalized = normalized, Description = desc,
            SearchText = $"{pn} {normalized} {desc} {model} {remark}".ToLowerInvariant(),
            Remark = remark, Quantity = qty, Model = model,
            Illustration = ill, PageNumber = page
        };
    }
}
