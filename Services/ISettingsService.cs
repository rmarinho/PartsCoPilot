namespace PartsCopilot.Services;

/// <summary>
/// User's preferred app theme.
/// </summary>
public enum ThemePreference
{
    System = 0,
    Light = 1,
    Dark = 2,
}

/// <summary>
/// Manages user settings: API key (secure), model preference, and theme.
/// </summary>
public interface ISettingsService
{
    /// <summary>Reads the OpenAI API key from platform-secure storage.</summary>
    Task<string?> GetApiKeyAsync();

    /// <summary>Writes the OpenAI API key to platform-secure storage.</summary>
    Task SetApiKeyAsync(string? apiKey);

    /// <summary>Returns the selected OpenAI model name.</summary>
    string GetModel();

    /// <summary>Persists the selected OpenAI model name.</summary>
    void SetModel(string model);

    /// <summary>Returns the user's theme preference.</summary>
    ThemePreference GetThemePreference();

    /// <summary>Persists the user's theme preference.</summary>
    void SetThemePreference(ThemePreference theme);

    /// <summary>Returns true if a non-placeholder API key is stored.</summary>
    Task<bool> HasApiKeyAsync();

    /// <summary>
    /// Makes a lightweight LLM call to validate the given key and model.
    /// Returns null on success, or an error message on failure.
    /// </summary>
    Task<string?> ValidateApiKeyAsync(string apiKey, string model, CancellationToken ct = default);

    /// <summary>Available model identifiers the user can choose from.</summary>
    IReadOnlyList<string> AvailableModels { get; }
}
