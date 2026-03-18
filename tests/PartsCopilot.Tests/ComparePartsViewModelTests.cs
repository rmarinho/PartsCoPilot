using Xunit;
using FluentAssertions;
using NSubstitute;
using PartsCopilot.Models;
using PartsCopilot.Services;
using PartsCopilot.ViewModels;
namespace PartsCopilot.Tests;
public class ComparePartsViewModelTests
{
    private readonly IPartsRepository _repo = Substitute.For<IPartsRepository>();
    private ComparePartsViewModel CreateVm() => new(_repo);
    private static PartRecord MakePart(string id = "p1", string pn = "901-107-751-00", string desc = "Oil thermostat", string? model = "911", string? illustration = "A1", string? section = "Engine", int page = 3, string? qty = "1", string? remark = null, string? position = "1") =>
        new() { Id = id, ManualId = "m1", PartNumber = pn, PartNumberNormalized = pn.Replace("-", ""), Description = desc, SearchText = $"{pn} {desc}", PageNumber = page, Model = model, Illustration = illustration, Section = section, Quantity = qty, Remark = remark, Position = position };
    [Fact] public void BothPartsNull_HasBothPartsFalse() { CreateVm().HasBothParts.Should().BeFalse(); }
    [Fact] public void OnlyLeftPart_HasBothPartsFalse() { var vm = CreateVm(); vm.ApplyQueryAttributes(new Dictionary<string, object> { ["Part"] = MakePart() }); vm.HasBothParts.Should().BeFalse(); }
    [Fact] public void BothPartsSet_HasBothPartsTrue() { var vm = CreateVm(); vm.ApplyQueryAttributes(new Dictionary<string, object> { ["Part"] = MakePart() }); vm.SelectPartCommand.Execute(MakePart("p2")); vm.HasBothParts.Should().BeTrue(); }
    [Fact] public void MatchingFields_ReturnTrue() { var vm = CreateVm(); vm.ApplyQueryAttributes(new Dictionary<string, object> { ["Part"] = MakePart("p1") }); vm.SelectPartCommand.Execute(MakePart("p2")); vm.PartNumbersMatch.Should().BeTrue(); vm.DescriptionsMatch.Should().BeTrue(); vm.ModelsMatch.Should().BeTrue(); vm.IllustrationsMatch.Should().BeTrue(); vm.SectionsMatch.Should().BeTrue(); vm.PagesMatch.Should().BeTrue(); vm.QuantitiesMatch.Should().BeTrue(); vm.PositionsMatch.Should().BeTrue(); }
    [Fact] public void DifferentFields_ReturnFalse() { var vm = CreateVm(); vm.ApplyQueryAttributes(new Dictionary<string, object> { ["Part"] = MakePart("p1") }); vm.SelectPartCommand.Execute(MakePart("p2", "912-200-100-00", "Brake disc", "912", "B2", "Brakes", 7, "2", "Note", "5")); vm.PartNumbersMatch.Should().BeFalse(); vm.DescriptionsMatch.Should().BeFalse(); vm.ModelsMatch.Should().BeFalse(); vm.RemarksMatch.Should().BeFalse(); }
    [Fact] public void NormalizedEqual_BothNull_Match() { var vm = CreateVm(); vm.ApplyQueryAttributes(new Dictionary<string, object> { ["Part"] = MakePart("p1", remark: null) }); vm.SelectPartCommand.Execute(MakePart("p2", remark: null)); vm.RemarksMatch.Should().BeTrue(); }
    [Fact] public void NormalizedEqual_BothWhitespace_Match() { var vm = CreateVm(); vm.ApplyQueryAttributes(new Dictionary<string, object> { ["Part"] = MakePart("p1", remark: "  ") }); vm.SelectPartCommand.Execute(MakePart("p2", remark: "")); vm.RemarksMatch.Should().BeTrue(); }
    [Fact] public void NormalizedEqual_CaseInsensitive_Match() { var vm = CreateVm(); vm.ApplyQueryAttributes(new Dictionary<string, object> { ["Part"] = MakePart("p1", model: "911 S") }); vm.SelectPartCommand.Execute(MakePart("p2", model: "911 s")); vm.ModelsMatch.Should().BeTrue(); }
    [Fact] public void NormalizedEqual_WhitespaceTrimmed_Match() { var vm = CreateVm(); vm.ApplyQueryAttributes(new Dictionary<string, object> { ["Part"] = MakePart("p1", section: "  Engine  ") }); vm.SelectPartCommand.Execute(MakePart("p2", section: "Engine")); vm.SectionsMatch.Should().BeTrue(); }
    [Fact] public void NormalizedEqual_OneNull_NoMatch() { var vm = CreateVm(); vm.ApplyQueryAttributes(new Dictionary<string, object> { ["Part"] = MakePart("p1", remark: "Note") }); vm.SelectPartCommand.Execute(MakePart("p2", remark: null)); vm.RemarksMatch.Should().BeFalse(); }
    [Fact] public void SwapParts_ReversesLeftAndRight() { var vm = CreateVm(); vm.ApplyQueryAttributes(new Dictionary<string, object> { ["Part"] = MakePart("p1", "AAA") }); vm.SelectPartCommand.Execute(MakePart("p2", "BBB")); vm.SwapPartsCommand.Execute(null); vm.LeftPart!.Id.Should().Be("p2"); vm.RightPart!.Id.Should().Be("p1"); }
    [Fact] public void SwapParts_OnlyOnePart_DoesNothing() { var vm = CreateVm(); vm.ApplyQueryAttributes(new Dictionary<string, object> { ["Part"] = MakePart() }); vm.SwapPartsCommand.Execute(null); vm.LeftPart!.Id.Should().Be("p1"); vm.RightPart.Should().BeNull(); }
    [Fact] public void ClearRightPart_ResetsAndShowsSearch() { var vm = CreateVm(); vm.ApplyQueryAttributes(new Dictionary<string, object> { ["Part"] = MakePart() }); vm.SelectPartCommand.Execute(MakePart("p2")); vm.ClearRightPartCommand.Execute(null); vm.RightPart.Should().BeNull(); vm.ShowSearchPanel.Should().BeTrue(); }
    [Fact] public void SelectPart_HidesSearchPanel() { var vm = CreateVm(); vm.SelectPartCommand.Execute(MakePart()); vm.ShowSearchPanel.Should().BeFalse(); }
    [Fact] public async Task Search_ExcludesLeftPartFromResults() { var vm = CreateVm(); vm.ApplyQueryAttributes(new Dictionary<string, object> { ["Part"] = MakePart("p1") }); _repo.SearchPartsAsync("oil", "m1", Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(new[] { MakePart("p1"), MakePart("p2", "902-000-000-00", "Other") }); vm.SearchQuery = "oil"; await vm.SearchCommand.ExecuteAsync(null); vm.SearchResults.Should().HaveCount(1); vm.SearchResults[0].Id.Should().Be("p2"); }
    [Fact] public async Task Search_EmptyQuery_DoesNotSearch() { var vm = CreateVm(); vm.SearchQuery = "  "; await vm.SearchCommand.ExecuteAsync(null); await _repo.DidNotReceive().SearchPartsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>()); }
    [Fact] public void ApplyQueryAttributes_SetsLeftPart() { var vm = CreateVm(); var part = MakePart(); vm.ApplyQueryAttributes(new Dictionary<string, object> { ["Part"] = part }); vm.LeftPart.Should().BeSameAs(part); }
}
