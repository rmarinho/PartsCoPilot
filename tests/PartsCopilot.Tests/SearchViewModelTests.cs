using Xunit;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using PartsCopilot.Models;
using PartsCopilot.Services;
using PartsCopilot.ViewModels;
namespace PartsCopilot.Tests;
public class SearchViewModelTests
{
    private readonly ISearchService _search = Substitute.For<ISearchService>();
    private readonly IPartsRepository _repo = Substitute.For<IPartsRepository>();
    private readonly IUserDataRepository _userData = Substitute.For<IUserDataRepository>();
    private SearchViewModel CreateVm() => new(_search, _repo, _userData);
    private static PartRecord MakePart(string id = "p1", string pn = "901-107-751-00", string desc = "Oil thermostat") =>
        new() { Id = id, ManualId = "m1", PartNumber = pn, PartNumberNormalized = pn.Replace("-", ""), Description = desc, SearchText = $"{pn} {desc}", PageNumber = 3 };
    private static SearchResult MakeResult(params PartRecord[] parts)
    {
        var candidates = parts.Select(p => new SearchCandidate(p, 0.9, "exact")).ToList();
        return new SearchResult(candidates, candidates.Count, TimeSpan.FromMilliseconds(42));
    }
    [Fact] public async Task Search_PopulatesResultsAndFlags()
    { var vm = CreateVm(); _search.SearchAsync(Arg.Any<SearchQuery>(), Arg.Any<CancellationToken>()).Returns(MakeResult(MakePart())); _userData.IsFavoriteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false); vm.QueryText = "oil"; await vm.SearchCommand.ExecuteAsync(null); vm.Results.Should().HaveCount(1); vm.HasResults.Should().BeTrue(); vm.HasNoResults.Should().BeFalse(); vm.TotalMatches.Should().Be(1); vm.SearchDuration.Should().Be("42ms"); vm.IsSearching.Should().BeFalse(); }
    [Fact] public async Task Search_EmptyQuery_DoesNothing()
    { var vm = CreateVm(); vm.QueryText = "  "; await vm.SearchCommand.ExecuteAsync(null); await _search.DidNotReceive().SearchAsync(Arg.Any<SearchQuery>(), Arg.Any<CancellationToken>()); }
    [Fact] public async Task Search_NoResults_SetsHasNoResults()
    { var vm = CreateVm(); _search.SearchAsync(Arg.Any<SearchQuery>(), Arg.Any<CancellationToken>()).Returns(new SearchResult(Array.Empty<SearchCandidate>(), 0, TimeSpan.Zero)); vm.QueryText = "nonexistent"; await vm.SearchCommand.ExecuteAsync(null); vm.HasResults.Should().BeFalse(); vm.HasNoResults.Should().BeTrue(); }
    [Fact] public async Task Search_Exception_SetsErrorMessage()
    { var vm = CreateVm(); _search.SearchAsync(Arg.Any<SearchQuery>(), Arg.Any<CancellationToken>()).Throws(new InvalidOperationException("DB error")); vm.QueryText = "test"; await vm.SearchCommand.ExecuteAsync(null); vm.ErrorMessage.Should().Be("DB error"); }
    [Fact] public async Task Search_SavesSearchHistory()
    { var vm = CreateVm(); _search.SearchAsync(Arg.Any<SearchQuery>(), Arg.Any<CancellationToken>()).Returns(MakeResult(MakePart())); _userData.IsFavoriteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false); vm.QueryText = "oil filter"; await vm.SearchCommand.ExecuteAsync(null); await _userData.Received(1).SaveSearchAsync(Arg.Is<SearchHistoryEntry>(e => e.QueryText == "oil filter"), Arg.Any<CancellationToken>()); }
    [Fact] public async Task Search_PassesFilters()
    { var vm = CreateVm(); _search.SearchAsync(Arg.Any<SearchQuery>(), Arg.Any<CancellationToken>()).Returns(new SearchResult(Array.Empty<SearchCandidate>(), 0, TimeSpan.Zero)); vm.QueryText = "brake"; vm.SelectedModel = "911 S"; vm.SelectedYear = 1967; await vm.SearchCommand.ExecuteAsync(null); await _search.Received(1).SearchAsync(Arg.Is<SearchQuery>(q => q.Context != null && q.Context.Model == "911 S" && q.Context.Year == 1967), Arg.Any<CancellationToken>()); }
    [Fact] public void ClearFilters_ResetsModelAndYear()
    { var vm = CreateVm(); vm.SelectedModel = "912"; vm.SelectedYear = 1968; vm.ClearFiltersCommand.Execute(null); vm.SelectedModel.Should().BeNull(); vm.SelectedYear.Should().BeNull(); }
    [Fact] public async Task ToggleFavorite_AddsWhenNotFavorite()
    { var vm = CreateVm(); var c = new SearchCandidateViewModel(new SearchCandidate(MakePart(), 0.9, "exact")) { IsFavorite = false }; await vm.ToggleFavoriteCommand.ExecuteAsync(c); c.IsFavorite.Should().BeTrue(); await _userData.Received(1).SaveFavoriteAsync(Arg.Any<FavoriteEntry>()); }
    [Fact] public async Task ToggleFavorite_RemovesWhenFavorite()
    { var vm = CreateVm(); var c = new SearchCandidateViewModel(new SearchCandidate(MakePart(), 0.9, "exact")) { IsFavorite = true }; await vm.ToggleFavoriteCommand.ExecuteAsync(c); c.IsFavorite.Should().BeFalse(); await _userData.Received(1).RemoveFavoriteAsync("p1"); }
    [Fact] public async Task ToggleFavorite_NullItem_DoesNothing()
    { var vm = CreateVm(); await vm.ToggleFavoriteCommand.ExecuteAsync(null); await _userData.DidNotReceive().SaveFavoriteAsync(Arg.Any<FavoriteEntry>()); }
    [Fact] public async Task ToggleFavorite_Error_SetsErrorMessage()
    { var vm = CreateVm(); _userData.SaveFavoriteAsync(Arg.Any<FavoriteEntry>()).Throws(new Exception("save error")); var c = new SearchCandidateViewModel(new SearchCandidate(MakePart(), 0.9, "exact")) { IsFavorite = false }; await vm.ToggleFavoriteCommand.ExecuteAsync(c); vm.ErrorMessage.Should().Be("save error"); }
    [Fact] public async Task LoadManualInfo_SetsTitle()
    { var vm = CreateVm(); _repo.GetAllManualsAsync(Arg.Any<CancellationToken>()).Returns(new[] { new ManualMetadata { Id = "m1", Title = "911 Parts", FilePath = "/p", PageCount = 10, PartCount = 5 } }); await vm.LoadManualInfoCommand.ExecuteAsync(null); vm.ActiveManualTitle.Should().Be("911 Parts"); }
    [Fact] public async Task LoadManualInfo_NoManual_ShowsPlaceholder()
    { var vm = CreateVm(); _repo.GetAllManualsAsync(Arg.Any<CancellationToken>()).Returns(Array.Empty<ManualMetadata>()); await vm.LoadManualInfoCommand.ExecuteAsync(null); vm.ActiveManualTitle.Should().Be("No manual imported"); }
    [Fact] public async Task Search_ChecksFavoriteStatusPerResult()
    { var vm = CreateVm(); var p1 = MakePart("p1"); var p2 = MakePart("p2", "902-000-000-00", "Another"); _search.SearchAsync(Arg.Any<SearchQuery>(), Arg.Any<CancellationToken>()).Returns(MakeResult(p1, p2)); _userData.IsFavoriteAsync("p1", Arg.Any<CancellationToken>()).Returns(true); _userData.IsFavoriteAsync("p2", Arg.Any<CancellationToken>()).Returns(false); vm.QueryText = "part"; await vm.SearchCommand.ExecuteAsync(null); vm.Results[0].IsFavorite.Should().BeTrue(); vm.Results[1].IsFavorite.Should().BeFalse(); }
    [Fact] public void SearchCandidateViewModel_ExposesProperties()
    { var part = MakePart("x", "911-101-001-00", "Cyl") with { Quantity = "2", Model = "911 S", Illustration = "A1", Remark = "Note" }; var vm = new SearchCandidateViewModel(new SearchCandidate(part, 0.85, "desc match")); vm.PartNumber.Should().Be("911-101-001-00"); vm.Description.Should().Be("Cyl"); vm.Score.Should().Be("85%"); vm.MatchReason.Should().Be("desc match"); }
}
