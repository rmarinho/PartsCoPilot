using Xunit;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using PartsCopilot.Models;
using PartsCopilot.Services;
using PartsCopilot.ViewModels;
namespace PartsCopilot.Tests;
public class PartDetailsViewModelTests
{
    private readonly IUserDataRepository _userData = Substitute.For<IUserDataRepository>();
    private PartDetailsViewModel CreateVm() => new(_userData);
    private static PartRecord MakePart(string id = "p1") => new() { Id = id, ManualId = "m1", PartNumber = "901-107-751-00", PartNumberNormalized = "90110775100", Description = "Oil thermostat", SearchText = "901-107-751-00 Oil thermostat", PageNumber = 3, Position = "1", Illustration = "A1", Quantity = "2", Model = "911 S", Remark = "Check fitment", Section = "Engine" };
    [Fact] public void ApplyQueryAttributes_NotifiesAllDerivedProperties() { var vm = CreateVm(); var changed = new List<string>(); vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName!); vm.ApplyQueryAttributes(new Dictionary<string, object> { ["Part"] = MakePart(), ["IsFavorite"] = true }); changed.Should().Contain("PartNumber"); changed.Should().Contain("Description"); changed.Should().Contain("PageDisplay"); changed.Should().Contain("HasRemark"); }
    [Fact] public void DerivedProperties_WithPart_ReturnCorrectValues() { var vm = CreateVm(); vm.ApplyQueryAttributes(new Dictionary<string, object> { ["Part"] = MakePart() }); vm.PartNumber.Should().Be("901-107-751-00"); vm.Description.Should().Be("Oil thermostat"); vm.PageDisplay.Should().Be("Page 3"); vm.HasRemark.Should().BeTrue(); vm.HasModel.Should().BeTrue(); }
    [Fact] public void DerivedProperties_NoPart_ReturnDefaults() { var vm = CreateVm(); vm.PartNumber.Should().Be(""); vm.Description.Should().Be(""); vm.PageNumber.Should().Be(0); vm.HasRemark.Should().BeFalse(); }
    [Fact] public void DerivedProperties_NullOptionals_HasFlagsFalse() { var vm = CreateVm(); var part = new PartRecord { ManualId = "m1", PartNumber = "X", PartNumberNormalized = "X", Description = "Y", SearchText = "X Y", PageNumber = 1 }; vm.ApplyQueryAttributes(new Dictionary<string, object> { ["Part"] = part }); vm.HasRemark.Should().BeFalse(); vm.HasPosition.Should().BeFalse(); vm.HasIllustration.Should().BeFalse(); vm.HasQuantity.Should().BeFalse(); vm.HasModel.Should().BeFalse(); vm.HasSection.Should().BeFalse(); }
    [Fact] public async Task ToggleFavorite_AddsWhenNotFavorite() { var vm = CreateVm(); vm.ApplyQueryAttributes(new Dictionary<string, object> { ["Part"] = MakePart(), ["IsFavorite"] = false }); await vm.ToggleFavoriteCommand.ExecuteAsync(null); vm.IsFavorite.Should().BeTrue(); await _userData.Received(1).SaveFavoriteAsync(Arg.Any<FavoriteEntry>()); }
    [Fact] public async Task ToggleFavorite_RemovesWhenFavorite() { var vm = CreateVm(); vm.ApplyQueryAttributes(new Dictionary<string, object> { ["Part"] = MakePart(), ["IsFavorite"] = true }); await vm.ToggleFavoriteCommand.ExecuteAsync(null); vm.IsFavorite.Should().BeFalse(); await _userData.Received(1).RemoveFavoriteAsync("p1"); }
    [Fact] public async Task ToggleFavorite_NoPart_DoesNothing() { var vm = CreateVm(); await vm.ToggleFavoriteCommand.ExecuteAsync(null); await _userData.DidNotReceive().SaveFavoriteAsync(Arg.Any<FavoriteEntry>()); }
    [Fact] public async Task ToggleFavorite_Error_SetsErrorMessage() { var vm = CreateVm(); vm.ApplyQueryAttributes(new Dictionary<string, object> { ["Part"] = MakePart(), ["IsFavorite"] = false }); _userData.SaveFavoriteAsync(Arg.Any<FavoriteEntry>()).Throws(new Exception("disk full")); await vm.ToggleFavoriteCommand.ExecuteAsync(null); vm.ErrorMessage.Should().Be("disk full"); }
    [Fact] public void ApplyQueryAttributes_WrongPartType_NoPart() { var vm = CreateVm(); vm.ApplyQueryAttributes(new Dictionary<string, object> { ["Part"] = "not a PartRecord" }); vm.Part.Should().BeNull(); }
}
