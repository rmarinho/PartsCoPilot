using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace PartsCopilot.Services;

/// <summary>
/// Settings backed by SecureStorage (API key) and Preferences (model, theme).
/// On first launch, seeds from environment variables for backward compatibility.
/// </summary>
public sealed class SettingsService : ISettingsService
{
    private const string ApiKeyStorageKey = "openai_api_key";
    private const string ModelPrefsKey = "openai_model";
    private const string ThemePrefsKey = "app_theme";
    private const string SeededFlagKey = "settings_seeded";
    private const string DefaultModel = "gpt-4o-mini";
    private const string PlaceholderKey = "sk-placeholder";

    private static readonly string[] Models =
    [
        "gpt-4o",
        "gpt-4o-mini",
        "gpt-4.1",
        "gpt-4.1-mini",
        "gpt-4.1-nano",
        "o4-mini",
    ];

    public IReadOnlyList<string> AvailableModels => Models;

    public SettingsService()
    {
        // Seed from env vars on very first launch
        SeedFromEnvironmentIfNeeded();
    }

    public async Task<string?> GetApiKeyAsync()
    {
        try
        {
            var key = await SecureStorage.Default.GetAsync(ApiKeyStorageKey);
            return string.IsNullOrWhiteSpace(key) || key == PlaceholderKey ? null : key;
        }
        catch
        {
            return null;
        }
    }

    public async Task SetApiKeyAsync(string? apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            SecureStorage.Default.Remove(ApiKeyStorageKey);
        }
        else
        {
            await SecureStorage.Default.SetAsync(ApiKeyStorageKey, apiKey.Trim());
        }
    }

    public string GetModel()
    {
        var model = Preferences.Default.Get(ModelPrefsKey, DefaultModel);
        return string.IsNullOrWhiteSpace(model) ? DefaultModel : model;
    }

    public void SetModel(string model)
    {
        Preferences.Default.Set(ModelPrefsKey, model);
    }

    public ThemePreference GetThemePreference()
    {
        var value = Preferences.Default.Get(ThemePrefsKey, (int)ThemePreference.System);
        return Enum.IsDefined(typeof(ThemePreference), value)
            ? (ThemePreference)value
            : ThemePreference.System;
    }

    public void SetThemePreference(ThemePreference theme)
    {
        Preferences.Default.Set(ThemePrefsKey, (int)theme);
    }

    public async Task<bool> HasApiKeyAsync()
    {
        var key = await GetApiKeyAsync();
        return !string.IsNullOrWhiteSpace(key);
    }

    public async Task<string?> ValidateApiKeyAsync(string apiKey, string model, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            return "API key is required.";

        try
        {
            var kb = Kernel.CreateBuilder();
            kb.AddOpenAIChatCompletion(model, apiKey);
            var kernel = kb.Build();
            var chat = kernel.GetRequiredService<IChatCompletionService>();

            var history = new ChatHistory();
            history.AddUserMessage("Say 'ok'.");

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(15));

            var result = await chat.GetChatMessageContentAsync(history, cancellationToken: cts.Token);
            return result.Content is null ? "Received empty response from API." : null;
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return "Connection timed out. Check your network and try again.";
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            return "Invalid API key. Please check and try again.";
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        {
            return "Rate limit exceeded. Wait a moment and try again.";
        }
        catch (HttpRequestException ex)
        {
            return $"API error: {ex.StatusCode} — {ex.Message}";
        }
        catch (Exception ex)
        {
            return $"Connection failed: {ex.Message}";
        }
    }

    private void SeedFromEnvironmentIfNeeded()
    {
        if (Preferences.Default.Get(SeededFlagKey, false))
            return;

        var envKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (!string.IsNullOrWhiteSpace(envKey) && envKey != PlaceholderKey)
        {
            // Fire-and-forget; next GetApiKeyAsync will pick it up
            SecureStorage.Default.SetAsync(ApiKeyStorageKey, envKey).ConfigureAwait(false);
        }

        var envModel = Environment.GetEnvironmentVariable("OPENAI_MODEL");
        if (!string.IsNullOrWhiteSpace(envModel))
        {
            Preferences.Default.Set(ModelPrefsKey, envModel);
        }

        Preferences.Default.Set(SeededFlagKey, true);
    }
}
