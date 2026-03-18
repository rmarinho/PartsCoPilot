using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PartsCopilot.Services;

namespace PartsCopilot.ViewModels;

/// <summary>
/// ViewModel for the Settings page — API key, model, theme, and connection testing.
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settings;

    public SettingsViewModel(ISettingsService settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _availableModels = new ObservableCollection<string>(_settings.AvailableModels);
    }

    // ── Observable properties ──────────────────────────────────────

    [ObservableProperty]
    private string _apiKey = string.Empty;

    [ObservableProperty]
    private string _selectedModel = string.Empty;

    [ObservableProperty]
    private ObservableCollection<string> _availableModels;

    [ObservableProperty]
    private ThemePreference _selectedTheme;

    [ObservableProperty]
    private ObservableCollection<string> _themeOptions = new(["System", "Light", "Dark"]);

    /// <summary>String-mapped theme name for Picker binding.</summary>
    public string SelectedThemeName
    {
        get => SelectedTheme.ToString();
        set
        {
            if (Enum.TryParse<ThemePreference>(value, out var parsed) && parsed != SelectedTheme)
            {
                SelectedTheme = parsed;
                OnPropertyChanged();
            }
        }
    }

    partial void OnSelectedThemeChanged(ThemePreference value) =>
        OnPropertyChanged(nameof(SelectedThemeName));

    [ObservableProperty]
    private bool _isTestingConnection;

    [ObservableProperty]
    private bool _isSaving;

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private bool _isStatusError;

    [ObservableProperty]
    private bool _hasApiKey;

    [ObservableProperty]
    private bool _isLoaded;

    // ── Lifecycle ──────────────────────────────────────────────────

    [RelayCommand]
    private async Task LoadSettingsAsync()
    {
        var key = await _settings.GetApiKeyAsync();
        ApiKey = key ?? string.Empty;
        SelectedModel = _settings.GetModel();
        SelectedTheme = _settings.GetThemePreference();
        HasApiKey = !string.IsNullOrWhiteSpace(key);
        IsLoaded = true;
        ClearStatus();
    }

    // ── Commands ───────────────────────────────────────────────────

    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        IsSaving = true;
        ClearStatus();

        try
        {
            await _settings.SetApiKeyAsync(
                string.IsNullOrWhiteSpace(ApiKey) ? null : ApiKey);

            if (!string.IsNullOrWhiteSpace(SelectedModel))
                _settings.SetModel(SelectedModel);

            _settings.SetThemePreference(SelectedTheme);
            ApplyTheme(SelectedTheme);

            HasApiKey = !string.IsNullOrWhiteSpace(ApiKey);
            SetSuccess("Settings saved.");
        }
        catch (Exception ex)
        {
            SetError($"Failed to save: {ex.Message}");
        }
        finally
        {
            IsSaving = false;
        }
    }

    [RelayCommand]
    private async Task TestConnectionAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            SetError("Enter an API key first.");
            return;
        }

        IsTestingConnection = true;
        ClearStatus();

        try
        {
            var model = string.IsNullOrWhiteSpace(SelectedModel)
                ? "gpt-4o-mini"
                : SelectedModel;

            var error = await _settings.ValidateApiKeyAsync(ApiKey, model, ct);

            if (error is null)
                SetSuccess("Connection successful! ✓");
            else
                SetError(error);
        }
        catch (OperationCanceledException)
        {
            ClearStatus();
        }
        catch (Exception ex)
        {
            SetError($"Test failed: {ex.Message}");
        }
        finally
        {
            IsTestingConnection = false;
        }
    }

    [RelayCommand]
    private void ClearApiKey()
    {
        ApiKey = string.Empty;
        HasApiKey = false;
        ClearStatus();
    }

    // ── Helpers ────────────────────────────────────────────────────

    private void SetSuccess(string message)
    {
        StatusMessage = message;
        IsStatusError = false;
    }

    private void SetError(string message)
    {
        StatusMessage = message;
        IsStatusError = true;
    }

    private void ClearStatus()
    {
        StatusMessage = null;
        IsStatusError = false;
    }

    private static void ApplyTheme(ThemePreference preference)
    {
        // Delegate to the MAUI host if available (not available in test context)
        OnThemeChanged?.Invoke(preference);
    }

    /// <summary>
    /// Set by the MAUI application host to wire up theme changes.
    /// </summary>
    internal static Action<ThemePreference>? OnThemeChanged { get; set; }
}
