using Xunit;
using FluentAssertions;
using NSubstitute;
using PartsCopilot.Models;
using PartsCopilot.Services;
using PartsCopilot.ViewModels;
namespace PartsCopilot.Tests;
public class FavoritesViewModelTests
{
    private readonly IUserDataRepository _userData = Substitute.For<IUserDataRepository>();
    private FavoritesViewModel CreateVm() => new(_userData);
    private static FavoriteEntry MakeFav(string id = "fav-1", string partId = "p1") => new() { Id = id, PartRecordId = partId, PartNumber = "901-107-751-00", Description = "Oil thermostat", PageNumber = 3 };
    private static SearchHistoryEntry MakeHist(string q = "oil", int c = 5) => new() { QueryText = q, ResultCount = c };
    [Fact] public async Task LoadData_PopulatesFavorites() { var vm = CreateVm(); _userData.GetFavoritesAsync(Arg.Any<CancellationToken>()).Returns(new[] { MakeFav("f1", "p1"), MakeFav("f2", "p2") }); _userData.GetRecentSearchesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(Array.Empty<SearchHistoryEntry>()); await vm.LoadDataCommand.ExecuteAsync(null); vm.Favorites.Should().HaveCount(2); vm.HasFavorites.Should().BeTrue(); }
    [Fact] public async Task LoadData_EmptyFavorites() { var vm = CreateVm(); _userData.GetFavoritesAsync(Arg.Any<CancellationToken>()).Returns(Array.Empty<FavoriteEntry>()); _userData.GetRecentSearchesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(Array.Empty<SearchHistoryEntry>()); await vm.LoadDataCommand.ExecuteAsync(null); vm.HasFavorites.Should().BeFalse(); }
    [Fact] public async Task LoadData_PopulatesHistory() { var vm = CreateVm(); _userData.GetFavoritesAsync(Arg.Any<CancellationToken>()).Returns(Array.Empty<FavoriteEntry>()); _userData.GetRecentSearchesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(new[] { MakeHist("oil", 10), MakeHist("brake", 3) }); await vm.LoadDataCommand.ExecuteAsync(null); vm.RecentSearches.Should().HaveCount(2); vm.HasHistory.Should().BeTrue(); }
    [Fact] public async Task RemoveFavorite_RemovesFromCollectionAndRepo() { var vm = CreateVm(); _userData.GetFavoritesAsync(Arg.Any<CancellationToken>()).Returns(new[] { MakeFav() }); _userData.GetRecentSearchesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(Array.Empty<SearchHistoryEntry>()); await vm.LoadDataCommand.ExecuteAsync(null); await vm.RemoveFavoriteCommand.ExecuteAsync(vm.Favorites[0]); vm.Favorites.Should().BeEmpty(); vm.HasFavorites.Should().BeFalse(); await _userData.Received(1).RemoveFavoriteAsync("p1"); }
    [Fact] public async Task RemoveFavorite_NullItem_DoesNothing() { var vm = CreateVm(); await vm.RemoveFavoriteCommand.ExecuteAsync(null); await _userData.DidNotReceive().RemoveFavoriteAsync(Arg.Any<string>()); }
    [Fact] public void SwitchToFavorites_SetsTabIndex0() { var vm = CreateVm(); vm.SwitchToHistoryCommand.Execute(null); vm.SwitchToFavoritesCommand.Execute(null); vm.SelectedTabIndex.Should().Be(0); vm.IsFavoritesTab.Should().BeTrue(); }
    [Fact] public void SwitchToHistory_SetsTabIndex1() { var vm = CreateVm(); vm.SwitchToHistoryCommand.Execute(null); vm.SelectedTabIndex.Should().Be(1); vm.IsHistoryTab.Should().BeTrue(); }
    [Fact] public async Task HasNoFavorites_TrueWhenOnFavoritesTabAndEmpty() { var vm = CreateVm(); _userData.GetFavoritesAsync(Arg.Any<CancellationToken>()).Returns(Array.Empty<FavoriteEntry>()); _userData.GetRecentSearchesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(Array.Empty<SearchHistoryEntry>()); await vm.LoadDataCommand.ExecuteAsync(null); vm.SwitchToFavoritesCommand.Execute(null); vm.HasNoFavorites.Should().BeTrue(); }
    [Fact] public async Task HasNoHistory_TrueWhenOnHistoryTabAndEmpty() { var vm = CreateVm(); _userData.GetFavoritesAsync(Arg.Any<CancellationToken>()).Returns(Array.Empty<FavoriteEntry>()); _userData.GetRecentSearchesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(Array.Empty<SearchHistoryEntry>()); await vm.LoadDataCommand.ExecuteAsync(null); vm.SwitchToHistoryCommand.Execute(null); vm.HasNoHistory.Should().BeTrue(); }
    [Fact] public void FavoriteItemViewModel_ExposesProperties() { var vm = new FavoriteItemViewModel(MakeFav() with { Model = "911 S", Illustration = "A1" }); vm.PartNumber.Should().Be("901-107-751-00"); vm.Model.Should().Be("911 S"); }
    [Fact] public void SearchHistoryItemViewModel_ExposesProperties() { var vm = new SearchHistoryItemViewModel(MakeHist("brake pad", 12)); vm.QueryText.Should().Be("brake pad"); vm.ResultCount.Should().Be(12); }
}
