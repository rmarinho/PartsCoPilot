using CommunityToolkit.Mvvm.Input;
using FluentAssertions;
using PartsCopilot.Services;
using PartsCopilot.ViewModels;
using Xunit;

namespace PartsCopilot.Tests;

/// <summary>
/// Tests for <see cref="SettingsViewModel"/> using a fake <see cref="ISettingsService"/>.
/// </summary>
public sealed class SettingsViewModelTests
{
    // ── Fakes ──────────────────────────────────────────────────────

    private sealed class FakeSettingsService : ISettingsService
    {
        public string? StoredApiKey { get; set; }
        public string StoredModel { get; set; } = "gpt-4o-mini";
        public ThemePreference StoredTheme { get; set; } = ThemePreference.System;
        public string? NextValidationError { get; set; }
        public int ValidateCallCount { get; private set; }

        public IReadOnlyList<string> AvailableModels { get; } =
            ["gpt-4o", "gpt-4o-mini", "gpt-4.1", "gpt-4.1-mini", "gpt-4.1-nano", "o4-mini"];

        public Task<string?> GetApiKeyAsync() => Task.FromResult(StoredApiKey);
        public Task SetApiKeyAsync(string? apiKey) { StoredApiKey = apiKey; return Task.CompletedTask; }
        public string GetModel() => StoredModel;
        public void SetModel(string model) => StoredModel = model;
        public ThemePreference GetThemePreference() => StoredTheme;
        public void SetThemePreference(ThemePreference theme) => StoredTheme = theme;
        public Task<bool> HasApiKeyAsync() => Task.FromResult(!string.IsNullOrWhiteSpace(StoredApiKey));

        public Task<string?> ValidateApiKeyAsync(string apiKey, string model, CancellationToken ct = default)
        {
            ValidateCallCount++;
            return Task.FromResult(NextValidationError);
        }
    }

    private static (SettingsViewModel vm, FakeSettingsService fake) Create(
        string? apiKey = null, string model = "gpt-4o-mini", ThemePreference theme = ThemePreference.System)
    {
        var fake = new FakeSettingsService
        {
            StoredApiKey = apiKey,
            StoredModel = model,
            StoredTheme = theme,
        };
        return (new SettingsViewModel(fake), fake);
    }

    // ── Constructor ────────────────────────────────────────────────

    [Fact]
    public void Constructor_SetsAvailableModels()
    {
        var (vm, _) = Create();
        vm.AvailableModels.Should().HaveCountGreaterThanOrEqualTo(2);
    }

    [Fact]
    public void Constructor_ThrowsOnNullService()
    {
        var act = () => new SettingsViewModel(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    // ── LoadSettings ───────────────────────────────────────────────

    [Fact]
    public async Task LoadSettings_PopulatesPropertiesFromService()
    {
        var (vm, _) = Create(apiKey: "sk-test-key", model: "gpt-4o", theme: ThemePreference.Dark);

        await vm.LoadSettingsCommand.ExecuteAsync(null);

        vm.ApiKey.Should().Be("sk-test-key");
        vm.SelectedModel.Should().Be("gpt-4o");
        vm.SelectedTheme.Should().Be(ThemePreference.Dark);
        vm.HasApiKey.Should().BeTrue();
        vm.IsLoaded.Should().BeTrue();
    }

    [Fact]
    public async Task LoadSettings_WhenNoApiKey_SetsEmptyAndHasApiKeyFalse()
    {
        var (vm, _) = Create(apiKey: null);

        await vm.LoadSettingsCommand.ExecuteAsync(null);

        vm.ApiKey.Should().BeEmpty();
        vm.HasApiKey.Should().BeFalse();
    }

    [Fact]
    public async Task LoadSettings_ClearsStatusMessage()
    {
        var (vm, _) = Create();

        await vm.LoadSettingsCommand.ExecuteAsync(null);

        vm.StatusMessage.Should().BeNull();
        vm.IsStatusError.Should().BeFalse();
    }

    // ── SaveSettings ───────────────────────────────────────────────

    [Fact]
    public async Task SaveSettings_PersistsAllValues()
    {
        var (vm, fake) = Create();
        vm.ApiKey = "sk-new-key";
        vm.SelectedModel = "gpt-4o";
        vm.SelectedTheme = ThemePreference.Light;

        await vm.SaveSettingsCommand.ExecuteAsync(null);

        fake.StoredApiKey.Should().Be("sk-new-key");
        fake.StoredModel.Should().Be("gpt-4o");
        fake.StoredTheme.Should().Be(ThemePreference.Light);
    }

    [Fact]
    public async Task SaveSettings_ShowsSuccessMessage()
    {
        var (vm, _) = Create();
        vm.ApiKey = "sk-key";

        await vm.SaveSettingsCommand.ExecuteAsync(null);

        vm.StatusMessage.Should().Contain("saved");
        vm.IsStatusError.Should().BeFalse();
    }

    [Fact]
    public async Task SaveSettings_EmptyApiKey_StoresNull()
    {
        var (vm, fake) = Create(apiKey: "old-key");
        vm.ApiKey = "";

        await vm.SaveSettingsCommand.ExecuteAsync(null);

        fake.StoredApiKey.Should().BeNull();
        vm.HasApiKey.Should().BeFalse();
    }

    [Fact]
    public async Task SaveSettings_WhitespaceApiKey_StoresNull()
    {
        var (vm, fake) = Create(apiKey: "old-key");
        vm.ApiKey = "   ";

        await vm.SaveSettingsCommand.ExecuteAsync(null);

        fake.StoredApiKey.Should().BeNull();
    }

    [Fact]
    public async Task SaveSettings_SetsIsSavingDuringExecution()
    {
        var (vm, _) = Create();
        bool wasSaving = false;

        vm.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(vm.IsSaving) && vm.IsSaving)
                wasSaving = true;
        };

        await vm.SaveSettingsCommand.ExecuteAsync(null);

        wasSaving.Should().BeTrue();
        vm.IsSaving.Should().BeFalse(); // Should be reset after
    }

    // ── TestConnection ─────────────────────────────────────────────

    [Fact]
    public async Task TestConnection_WhenEmpty_SetsError()
    {
        var (vm, fake) = Create();
        vm.ApiKey = "";

        await vm.TestConnectionCommand.ExecuteAsync(CancellationToken.None);

        vm.StatusMessage.Should().Contain("API key");
        vm.IsStatusError.Should().BeTrue();
        fake.ValidateCallCount.Should().Be(0);
    }

    [Fact]
    public async Task TestConnection_Success_ShowsSuccessMessage()
    {
        var (vm, fake) = Create();
        vm.ApiKey = "sk-valid-key";
        vm.SelectedModel = "gpt-4o-mini";
        fake.NextValidationError = null; // success

        await vm.TestConnectionCommand.ExecuteAsync(CancellationToken.None);

        vm.StatusMessage.Should().Contain("successful");
        vm.IsStatusError.Should().BeFalse();
        fake.ValidateCallCount.Should().Be(1);
    }

    [Fact]
    public async Task TestConnection_Failure_ShowsErrorMessage()
    {
        var (vm, fake) = Create();
        vm.ApiKey = "sk-bad-key";
        fake.NextValidationError = "Invalid API key.";

        await vm.TestConnectionCommand.ExecuteAsync(CancellationToken.None);

        vm.StatusMessage.Should().Be("Invalid API key.");
        vm.IsStatusError.Should().BeTrue();
    }

    [Fact]
    public async Task TestConnection_SetsIsTestingDuringExecution()
    {
        var (vm, _) = Create();
        vm.ApiKey = "sk-key";
        bool wasTesting = false;

        vm.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(vm.IsTestingConnection) && vm.IsTestingConnection)
                wasTesting = true;
        };

        await vm.TestConnectionCommand.ExecuteAsync(CancellationToken.None);

        wasTesting.Should().BeTrue();
        vm.IsTestingConnection.Should().BeFalse();
    }

    // ── ClearApiKey ────────────────────────────────────────────────

    [Fact]
    public void ClearApiKey_ResetsKeyAndFlag()
    {
        var (vm, _) = Create(apiKey: "sk-key");
        vm.ApiKey = "sk-key";
        vm.HasApiKey = true;

        vm.ClearApiKeyCommand.Execute(null);

        vm.ApiKey.Should().BeEmpty();
        vm.HasApiKey.Should().BeFalse();
        vm.StatusMessage.Should().BeNull();
    }

    // ── Theme Selection ────────────────────────────────────────────

    [Fact]
    public void SelectedThemeName_ReflectsThemePreference()
    {
        var (vm, _) = Create();
        vm.SelectedTheme = ThemePreference.Dark;

        vm.SelectedThemeName.Should().Be("Dark");
    }

    [Fact]
    public void SelectedThemeName_SetUpdatesThemePreference()
    {
        var (vm, _) = Create();
        vm.SelectedThemeName = "Light";

        vm.SelectedTheme.Should().Be(ThemePreference.Light);
    }

    [Fact]
    public void ThemeOptions_ContainsAllChoices()
    {
        var (vm, _) = Create();
        vm.ThemeOptions.Should().BeEquivalentTo(["System", "Light", "Dark"]);
    }

    // ── Model Selection ────────────────────────────────────────────

    [Fact]
    public async Task SaveSettings_ModelNotWhitespace_PersistsModel()
    {
        var (vm, fake) = Create();
        vm.SelectedModel = "gpt-4.1";

        await vm.SaveSettingsCommand.ExecuteAsync(null);

        fake.StoredModel.Should().Be("gpt-4.1");
    }

    [Fact]
    public async Task SaveSettings_EmptyModel_DoesNotOverwrite()
    {
        var (vm, fake) = Create(model: "gpt-4o");
        vm.SelectedModel = "";

        await vm.SaveSettingsCommand.ExecuteAsync(null);

        fake.StoredModel.Should().Be("gpt-4o"); // unchanged
    }
}
