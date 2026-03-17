using Xunit;
using PartsCopilot.Data;
using PartsCopilot.Models;
using FluentAssertions;

namespace PartsCopilot.Tests;

/// <summary>
/// UserDataRepository CRUD tests: favorites + search history.
/// </summary>
public class UserDataRepositoryTests : IAsyncLifetime, IDisposable
{
    private readonly string _dbPath;
    private readonly AppDatabase _db;
    private readonly UserDataRepository _repo;

    public UserDataRepositoryTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"partscopilot_test_{Guid.NewGuid():N}.db3");
        _db = new AppDatabase(_dbPath);
        _repo = new UserDataRepository(_db);
    }

    public async Task InitializeAsync() => await _db.InitializeAsync();
    public Task DisposeAsync() => Task.CompletedTask;
    public void Dispose()
    {
        try { File.Delete(_dbPath); } catch { }
    }

    // --- Favorites ---

    [Fact]
    public async Task SaveFavorite_AndGetFavorites_RoundTrips()
    {
        var fav = new FavoriteEntry
        {
            PartRecordId = "part-1",
            PartNumber = "901 107 751 00",
            Description = "Oil thermostat",
            Model = "911",
            PageNumber = 42,
            Illustration = "107-00",
            ManualId = "m1"
        };

        await _repo.SaveFavoriteAsync(fav);
        var loaded = await _repo.GetFavoritesAsync();

        loaded.Should().HaveCount(1);
        loaded[0].PartNumber.Should().Be("901 107 751 00");
        loaded[0].Description.Should().Be("Oil thermostat");
        loaded[0].Model.Should().Be("911");
    }

    [Fact]
    public async Task IsFavorite_ReturnsTrueWhenSaved()
    {
        var fav = new FavoriteEntry
        {
            PartRecordId = "part-1",
            PartNumber = "901 107 751 00",
            Description = "Oil thermostat"
        };

        await _repo.SaveFavoriteAsync(fav);

        var result = await _repo.IsFavoriteAsync("part-1");
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsFavorite_ReturnsFalseWhenNotSaved()
    {
        var result = await _repo.IsFavoriteAsync("nonexistent");
        result.Should().BeFalse();
    }

    [Fact]
    public async Task RemoveFavorite_DeletesEntry()
    {
        var fav = new FavoriteEntry
        {
            PartRecordId = "part-1",
            PartNumber = "901 107 751 00",
            Description = "Oil thermostat"
        };

        await _repo.SaveFavoriteAsync(fav);
        await _repo.RemoveFavoriteAsync("part-1");

        var isFav = await _repo.IsFavoriteAsync("part-1");
        isFav.Should().BeFalse();

        var all = await _repo.GetFavoritesAsync();
        all.Should().BeEmpty();
    }

    [Fact]
    public async Task RemoveFavorite_DoesNotAffectOtherFavorites()
    {
        await _repo.SaveFavoriteAsync(new FavoriteEntry { PartRecordId = "part-1", PartNumber = "PN-1", Description = "Part 1" });
        await _repo.SaveFavoriteAsync(new FavoriteEntry { PartRecordId = "part-2", PartNumber = "PN-2", Description = "Part 2" });

        await _repo.RemoveFavoriteAsync("part-1");

        var remaining = await _repo.GetFavoritesAsync();
        remaining.Should().HaveCount(1);
        remaining[0].PartRecordId.Should().Be("part-2");
    }

    [Fact]
    public async Task GetFavorites_ReturnsNewestFirst()
    {
        var older = new FavoriteEntry
        {
            PartRecordId = "part-old",
            PartNumber = "PN-1",
            Description = "Older",
            SavedAt = DateTime.UtcNow.AddDays(-1)
        };
        var newer = new FavoriteEntry
        {
            PartRecordId = "part-new",
            PartNumber = "PN-2",
            Description = "Newer",
            SavedAt = DateTime.UtcNow
        };

        await _repo.SaveFavoriteAsync(older);
        await _repo.SaveFavoriteAsync(newer);

        var loaded = await _repo.GetFavoritesAsync();
        loaded[0].PartRecordId.Should().Be("part-new");
        loaded[1].PartRecordId.Should().Be("part-old");
    }

    [Fact]
    public async Task SaveFavorite_Upserts_WhenSameIdSavedTwice()
    {
        var id = Guid.NewGuid().ToString();
        var fav1 = new FavoriteEntry { Id = id, PartRecordId = "part-1", PartNumber = "PN-1", Description = "V1" };
        var fav2 = new FavoriteEntry { Id = id, PartRecordId = "part-1", PartNumber = "PN-1", Description = "V2" };

        await _repo.SaveFavoriteAsync(fav1);
        await _repo.SaveFavoriteAsync(fav2);

        var all = await _repo.GetFavoritesAsync();
        all.Should().HaveCount(1, "InsertOrReplace should upsert on same PK");
        all[0].Description.Should().Be("V2");
    }

    // --- Search History ---

    [Fact]
    public async Task SaveSearch_AndGetRecentSearches_RoundTrips()
    {
        var entry = new SearchHistoryEntry
        {
            QueryText = "oil pump",
            ManualId = "m1",
            ResultCount = 5
        };

        await _repo.SaveSearchAsync(entry);
        var loaded = await _repo.GetRecentSearchesAsync();

        loaded.Should().HaveCount(1);
        loaded[0].QueryText.Should().Be("oil pump");
        loaded[0].ResultCount.Should().Be(5);
    }

    [Fact]
    public async Task GetRecentSearches_ReturnsNewestFirst()
    {
        var older = new SearchHistoryEntry { QueryText = "old query", SearchedAt = DateTime.UtcNow.AddHours(-2) };
        var newer = new SearchHistoryEntry { QueryText = "new query", SearchedAt = DateTime.UtcNow };

        await _repo.SaveSearchAsync(older);
        await _repo.SaveSearchAsync(newer);

        var loaded = await _repo.GetRecentSearchesAsync();
        loaded[0].QueryText.Should().Be("new query");
    }

    [Fact]
    public async Task GetRecentSearches_RespectsLimit()
    {
        for (int i = 0; i < 10; i++)
            await _repo.SaveSearchAsync(new SearchHistoryEntry { QueryText = $"query-{i}" });

        var loaded = await _repo.GetRecentSearchesAsync(limit: 3);
        loaded.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetRecentSearches_ReturnsEmptyWhenNoHistory()
    {
        var loaded = await _repo.GetRecentSearchesAsync();
        loaded.Should().BeEmpty();
    }
}
