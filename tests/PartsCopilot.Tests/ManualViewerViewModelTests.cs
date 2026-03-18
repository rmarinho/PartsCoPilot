using Xunit;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using PartsCopilot.Models;
using PartsCopilot.Services;
using PartsCopilot.ViewModels;
namespace PartsCopilot.Tests;
public class ManualViewerViewModelTests
{
    private readonly IManualNavigationService _nav = Substitute.For<IManualNavigationService>();
    private readonly IPartsRepository _repo = Substitute.For<IPartsRepository>();
    private ManualViewerViewModel CreateVm() => new(_nav, _repo);
    private static ManualMetadata MakeManual(int pc = 10) => new() { Id = "m1", Title = "911 Parts Manual", FilePath = "/p", PageCount = pc, PartCount = 25 };
    private static ManualPage MakePage(int n = 3, string? ill = "A1", string? sec = "Engine") => new() { ManualId = "m1", PageNumber = n, RawText = $"Page {n} content", PageType = "parts", Illustration = ill, Section = sec };
    [Fact] public async Task LoadPage_SetsContent() { var vm = CreateVm(); vm.ManualId = "m1"; vm.PageNumber = 3; _repo.GetManualAsync("m1", Arg.Any<CancellationToken>()).Returns(MakeManual()); _repo.GetPageAsync("m1", 3, Arg.Any<CancellationToken>()).Returns(MakePage()); await vm.LoadPageCommand.ExecuteAsync(null); vm.HasContent.Should().BeTrue(); vm.PageContent.Should().Be("Page 3 content"); vm.ManualTitle.Should().Be("911 Parts Manual"); vm.TotalPages.Should().Be(10); vm.IsLoading.Should().BeFalse(); }
    [Fact] public async Task LoadPage_EmptyManualId_ShowsError() { var vm = CreateVm(); vm.ManualId = ""; vm.PageNumber = 3; await vm.LoadPageCommand.ExecuteAsync(null); vm.HasError.Should().BeTrue(); vm.ErrorMessage.Should().Contain("No page information"); }
    [Fact] public async Task LoadPage_ZeroPageNumber_ShowsError() { var vm = CreateVm(); vm.ManualId = "m1"; vm.PageNumber = 0; await vm.LoadPageCommand.ExecuteAsync(null); vm.HasError.Should().BeTrue(); }
    [Fact] public async Task LoadPage_PageNotFound_ShowsError() { var vm = CreateVm(); vm.ManualId = "m1"; vm.PageNumber = 999; _repo.GetManualAsync("m1", Arg.Any<CancellationToken>()).Returns(MakeManual()); _repo.GetPageAsync("m1", 999, Arg.Any<CancellationToken>()).Returns((ManualPage?)null); await vm.LoadPageCommand.ExecuteAsync(null); vm.HasError.Should().BeTrue(); vm.ErrorMessage.Should().Contain("Page 999 not found"); }
    [Fact] public async Task LoadPage_Exception_ShowsError() { var vm = CreateVm(); vm.ManualId = "m1"; vm.PageNumber = 1; _repo.GetManualAsync("m1", Arg.Any<CancellationToken>()).Throws(new Exception("db corrupt")); await vm.LoadPageCommand.ExecuteAsync(null); vm.HasError.Should().BeTrue(); vm.ErrorMessage.Should().Contain("db corrupt"); vm.IsLoading.Should().BeFalse(); }
    [Fact] public async Task GoToPreviousPage_Decrements() { var vm = CreateVm(); vm.ManualId = "m1"; vm.PageNumber = 5; _repo.GetManualAsync("m1", Arg.Any<CancellationToken>()).Returns(MakeManual()); _repo.GetPageAsync("m1", 4, Arg.Any<CancellationToken>()).Returns(MakePage(4)); await vm.GoToPreviousPageCommand.ExecuteAsync(null); vm.PageNumber.Should().Be(4); }
    [Fact] public async Task GoToPreviousPage_AtPage1_DoesNothing() { var vm = CreateVm(); vm.ManualId = "m1"; vm.PageNumber = 1; await vm.GoToPreviousPageCommand.ExecuteAsync(null); vm.PageNumber.Should().Be(1); }
    [Fact] public async Task GoToNextPage_Increments() { var vm = CreateVm(); vm.ManualId = "m1"; vm.PageNumber = 3; vm.TotalPages = 10; _repo.GetManualAsync("m1", Arg.Any<CancellationToken>()).Returns(MakeManual()); _repo.GetPageAsync("m1", 4, Arg.Any<CancellationToken>()).Returns(MakePage(4)); await vm.GoToNextPageCommand.ExecuteAsync(null); vm.PageNumber.Should().Be(4); }
    [Fact] public async Task GoToNextPage_AtLastPage_DoesNothing() { var vm = CreateVm(); vm.ManualId = "m1"; vm.PageNumber = 10; vm.TotalPages = 10; await vm.GoToNextPageCommand.ExecuteAsync(null); vm.PageNumber.Should().Be(10); }
    [Fact] public async Task LoadPage_UpdatesNavigationState() { var vm = CreateVm(); vm.ManualId = "m1"; vm.PageNumber = 1; _repo.GetManualAsync("m1", Arg.Any<CancellationToken>()).Returns(MakeManual(10)); _repo.GetPageAsync("m1", 1, Arg.Any<CancellationToken>()).Returns(MakePage(1)); await vm.LoadPageCommand.ExecuteAsync(null); vm.CanGoBack.Should().BeFalse(); vm.CanGoForward.Should().BeTrue(); vm.PageIndicator.Should().Be("Page 1 of 10"); }
    [Fact] public async Task LoadPage_LastPage_CanGoForwardFalse() { var vm = CreateVm(); vm.ManualId = "m1"; vm.PageNumber = 10; _repo.GetManualAsync("m1", Arg.Any<CancellationToken>()).Returns(MakeManual(10)); _repo.GetPageAsync("m1", 10, Arg.Any<CancellationToken>()).Returns(MakePage(10)); await vm.LoadPageCommand.ExecuteAsync(null); vm.CanGoBack.Should().BeTrue(); vm.CanGoForward.Should().BeFalse(); }
    [Fact] public async Task LoadPage_BuildsPageTitle() { var vm = CreateVm(); vm.ManualId = "m1"; vm.PageNumber = 3; _repo.GetManualAsync("m1", Arg.Any<CancellationToken>()).Returns(MakeManual()); _repo.GetPageAsync("m1", 3, Arg.Any<CancellationToken>()).Returns(MakePage(3, "A1", "Engine")); await vm.LoadPageCommand.ExecuteAsync(null); vm.PageTitle.Should().Contain("Page 3").And.Contain("A1"); }
    [Fact] public async Task LoadPage_NoIllustration_SimpleTitle() { var vm = CreateVm(); vm.ManualId = "m1"; vm.PageNumber = 3; _repo.GetManualAsync("m1", Arg.Any<CancellationToken>()).Returns(MakeManual()); _repo.GetPageAsync("m1", 3, Arg.Any<CancellationToken>()).Returns(MakePage(3, null, null)); await vm.LoadPageCommand.ExecuteAsync(null); vm.PageTitle.Should().Be("Page 3"); }
}
