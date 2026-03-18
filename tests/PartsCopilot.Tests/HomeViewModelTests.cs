using Xunit;
using FluentAssertions;
using NSubstitute;
using PartsCopilot.Models;
using PartsCopilot.Services;
using PartsCopilot.ViewModels;
namespace PartsCopilot.Tests;
public class HomeViewModelTests
{
    private readonly IPartsRepository _repo = Substitute.For<IPartsRepository>();
    private readonly IPdfIngestionService _ingestion = Substitute.For<IPdfIngestionService>();
    private readonly IManualParser _parser = Substitute.For<IManualParser>();
    private readonly IUserDataRepository _userData = Substitute.For<IUserDataRepository>();
    private HomeViewModel CreateVm() => new(_repo, _ingestion, _parser, _userData);
    [Fact] public async Task LoadState_WithManual_SetsProperties() { var vm = CreateVm(); _repo.GetAllManualsAsync(Arg.Any<CancellationToken>()).Returns(new[] { new ManualMetadata { Id = "m1", Title = "911 Parts", FilePath = "/p", PageCount = 10, PartCount = 25 } }); await vm.LoadStateCommand.ExecuteAsync(null); vm.ActiveManualTitle.Should().Be("911 Parts"); vm.TotalParts.Should().Be(25); vm.HasManual.Should().BeTrue(); }
    [Fact] public async Task LoadState_NoManual() { var vm = CreateVm(); _repo.GetAllManualsAsync(Arg.Any<CancellationToken>()).Returns(Array.Empty<ManualMetadata>()); await vm.LoadStateCommand.ExecuteAsync(null); vm.ActiveManualTitle.Should().BeNull(); vm.HasManual.Should().BeFalse(); }
    [Fact] public async Task ImportManual_UserCancels() { var vm = CreateVm(); await vm.ImportManualCommand.ExecuteAsync(null); vm.IsImporting.Should().BeFalse(); await _ingestion.DidNotReceive().ExtractPagesAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()); }
    [Fact] public void InitialState_AllFlagsAreFalse() { var vm = CreateVm(); vm.IsImporting.Should().BeFalse(); vm.HasManual.Should().BeFalse(); vm.ImportStatus.Should().BeNull(); vm.ImportProgress.Should().Be(0); }
}
