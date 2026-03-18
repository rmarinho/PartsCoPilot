using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using PartsCopilot.Models;

namespace PartsCopilot.Services;

/// <summary>
/// Calls the LLM via Semantic Kernel and maps the JSON response to <see cref="AiAnswer"/>.
/// Includes retry logic for transient failures and a per-call timeout.
/// </summary>
public sealed class PartsAiService : IPartsAiService
{
    private const int MaxRetries = 3;
    private static readonly TimeSpan CallTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan[] RetryDelays =
    [
        TimeSpan.FromSeconds(1),
        TimeSpan.FromSeconds(3),
        TimeSpan.FromSeconds(8),
    ];

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly Kernel _kernel;
    private readonly IPromptBuilder _promptBuilder;
    private readonly ILogger<PartsAiService> _logger;

    public PartsAiService(Kernel kernel, IPromptBuilder promptBuilder, ILogger<PartsAiService> logger)
    {
        _kernel = kernel;
        _promptBuilder = promptBuilder;
        _logger = logger;
    }

    public async Task<AiAnswer> AskAsync(PromptContext context, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var prompt = _promptBuilder.BuildPrompt(context);

        for (int attempt = 0; attempt <= MaxRetries; attempt++)
        {
            try
            {
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                timeoutCts.CancelAfter(CallTimeout);

                var json = await CallLlmAsync(prompt, timeoutCts.Token);

                if (string.IsNullOrWhiteSpace(json))
                {
                    _logger.LogWarning("AI returned an empty response (attempt {Attempt}).", attempt + 1);
                    if (attempt < MaxRetries) continue;
                    return FallbackAnswer("The model returned an empty response. Please try again.");
                }

                return ParseResponse(json);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                // Timeout — not caller cancellation
                _logger.LogWarning("AI call timed out after {Timeout}s (attempt {Attempt}/{Max}).",
                    CallTimeout.TotalSeconds, attempt + 1, MaxRetries + 1);
            }
            catch (OperationCanceledException)
            {
                throw; // Caller cancelled — propagate immediately
            }
            catch (HttpRequestException ex) when (IsTransient(ex))
            {
                _logger.LogWarning(ex, "Transient AI failure (attempt {Attempt}/{Max}): {Status}",
                    attempt + 1, MaxRetries + 1, ex.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI request failed (non-retryable).");
                return FallbackAnswer($"AI request failed: {ex.Message}");
            }

            if (attempt < MaxRetries)
            {
                var delay = RetryDelays[attempt];
                _logger.LogInformation("Retrying AI call in {Delay}s...", delay.TotalSeconds);
                await Task.Delay(delay, ct);
            }
        }

        return FallbackAnswer("AI service is temporarily unavailable. Please try again later.");
    }

    private async Task<string?> CallLlmAsync(string prompt, CancellationToken ct)
    {
        var chat = _kernel.GetRequiredService<IChatCompletionService>();

        var history = new ChatHistory();
        history.AddUserMessage(prompt);

        var settings = new PromptExecutionSettings
        {
            ExtensionData = new Dictionary<string, object>
            {
                ["response_format"] = new { type = "json_object" }
            }
        };

        var result = await chat.GetChatMessageContentAsync(history, settings, cancellationToken: ct);
        return result.Content;
    }

    private static bool IsTransient(HttpRequestException ex) =>
        ex.StatusCode is HttpStatusCode.TooManyRequests
            or HttpStatusCode.ServiceUnavailable
            or HttpStatusCode.GatewayTimeout
            or HttpStatusCode.BadGateway
            or HttpStatusCode.RequestTimeout;

    private AiAnswer ParseResponse(string json)
    {
        try
        {
            // Strip markdown fences if the model returns them despite instructions
            json = json.Trim();
            if (json.StartsWith("```"))
            {
                var firstNewline = json.IndexOf('\n');
                if (firstNewline > 0)
                    json = json[(firstNewline + 1)..];
                if (json.EndsWith("```"))
                    json = json[..^3];
                json = json.Trim();
            }

            var answer = JsonSerializer.Deserialize<AiAnswer>(json, JsonOptions);
            if (answer is null)
            {
                _logger.LogWarning("JSON deserialized to null.");
                return FallbackAnswer("Could not parse the AI response.");
            }
            return answer;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse AI JSON: {Json}", json[..Math.Min(json.Length, 200)]);
            return FallbackAnswer("The AI response was not valid JSON. Please try again.");
        }
    }

    private static AiAnswer FallbackAnswer(string message) =>
        new(message, [], NeedsClarification: false, ClarificationQuestion: null);
}
