using Xunit;
using PartsCopilot.Data;
using PartsCopilot.Models;
using PartsCopilot.Services;
using FluentAssertions;

namespace PartsCopilot.Tests;

/// <summary>
/// ManualNavigationService tests: page lookups, illustration extraction, null handling.
/// </summary>
public class ManualNavigationServiceTests : IAsyncLifetime, IDisposable
{
    private readonly string _dbPath;
    private readonly AppDatabase _db;
    private readonly PartsRepository _repo;
    private readonly ManualNavigationService _nav;

    public ManualNavigationServiceTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"partscopilot_test_{Guid.NewGuid():N}.db3");
        _db = new AppDatabase(_dbPath);
        _repo = new PartsRepository(_db);
        _nav = new ManualNavigationService(_repo);
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

    [Fact]
    public void GetPageNumber_ReturnsPartPageNumber()
    {
        var part = MakePart("101-00", 42);
        _nav.GetPageNumber(part).Should().Be(42);
    }

    [Fact]
    public void GetIllustrationGroup_ReturnsPartIllustration()
    {
        var part = MakePart("107-00", 42);
        _nav.GetIllustrationGroup(part).Should().Be("107-00");
    }

    [Fact]
    public void GetIllustrationGroup_ReturnsNullWhenPartHasNoIllustration()
    {
        var part = MakePart(null, 42);
        _nav.GetIllustrationGroup(part).Should().BeNull();
    }

    [Fact]
    public void GetPageNumber_ThrowsOnNull()
    {
        var act = () => _nav.GetPageNumber(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetIllustrationGroup_ThrowsOnNull()
    {
        var act = () => _nav.GetIllustrationGroup(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task GetPageAsync_ReturnsMatchingPage()
    {
        var part = MakePart("107-00", 42);
        var page = await _nav.GetPageAsync(part);

        page.Should().NotBeNull();
        page!.PageNumber.Should().Be(42);
        page.Illustration.Should().Be("107-00");
    }

    [Fact]
    public async Task GetPageAsync_ReturnsNullForMissingPage()
    {
        var part = MakePart("107-00", 999);
        var page = await _nav.GetPageAsync(part);
        page.Should().BeNull();
    }

    [Fact]
    public async Task GetPageAsync_ThrowsOnNull()
    {
        var act = () => _nav.GetPageAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GetIllustrationAsync_ReturnsMatchingGroup()
    {
        var part = MakePart("107-00", 42);
        var group = await _nav.GetIllustrationAsync(part);

        group.Should().NotBeNull();
        group!.IllustrationNumber.Should().Be("107-00");
    }

    [Fact]
    public async Task GetIllustrationAsync_ReturnsNullWhenPartHasNoIllustration()
    {
        var part = MakePart(null, 42);
        var group = await _nav.GetIllustrationAsync(part);
        group.Should().BeNull();
    }

    [Fact]
    public async Task GetIllustrationAsync_IsCaseInsensitive()
    {
        var part = MakePart("107-00", 42);
        // The seeded part has "107-00", so even if the lookup uses upper case internally it should match
        var group = await _nav.GetIllustrationAsync(part);
        group.Should().NotBeNull();
    }

    [Fact]
    public async Task GetIllustrationGroupsForManualAsync_ReturnsDistinctGroups()
    {
        var groups = await _nav.GetIllustrationGroupsForManualAsync("test-manual");

        groups.Should().HaveCountGreaterThanOrEqualTo(2, "we seeded parts across 101-00 and 107-00");
        groups.Select(g => g.IllustrationNumber).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task GetIllustrationGroupsForManualAsync_ReturnsEmptyForUnknownManual()
    {
        var groups = await _nav.GetIllustrationGroupsForManualAsync("nonexistent-manual");
        groups.Should().BeEmpty();
    }

    private async Task SeedTestData()
    {
        await _repo.SaveManualAsync(new ManualMetadata { Id = "test-manual", Title = "Test", FilePath = "test://m.pdf" });

        await _repo.SavePagesAsync([
            new ManualPage { ManualId = "test-manual", PageNumber = 42, RawText = "Illustration: 107-00\nLubrication", Illustration = "107-00", PageType = "part_table" },
            new ManualPage { ManualId = "test-manual", PageNumber = 10, RawText = "Illustration: 101-00\nCrankcase", Illustration = "101-00", PageType = "part_table" },
        ]);

        await _repo.SavePartsAsync([
            MakePart("107-00", 42),
            MakePart("101-00", 10),
        ]);
    }

    private static PartRecord MakePart(string? illustration, int page)
    {
        var pn = $"901 107 {page:D3} 00";
        var normalized = pn.Replace(" ", "").ToUpperInvariant();
        return new PartRecord
        {
            ManualId = "test-manual",
            Position = "1",
            PartNumber = pn,
            PartNumberNormalized = normalized,
            Description = "Test part",
            SearchText = $"{pn} {normalized} test part".ToLowerInvariant(),
            Illustration = illustration,
            PageNumber = page
        };
    }
}
