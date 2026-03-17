# Suggested Contracts

## AI response JSON

```json
{
  "answer": "The best match is the oil thermostat entry for the selected 911 context.",
  "matches": [
    {
      "partNumber": "901 107 751 00",
      "description": "Oil thermostat",
      "position": "58",
      "illustration": "101-05",
      "page": 42,
      "qty": "1",
      "model": "911",
      "remark": "",
      "fitment": "Use only for the selected vehicle context when applicable.",
      "confidence": 0.96
    }
  ],
  "needsClarification": false,
  "clarificationQuestion": ""
}
```

## Suggested C# models

```csharp
public sealed record VehicleContext(
    string? Model,
    int? Year,
    string? Variant,
    string? Engine,
    string? Region);

public sealed record SearchQuery(
    string UserText,
    VehicleContext Context,
    bool IsExactPartNumber);

public sealed record PartRecord(
    string Id,
    string Position,
    string PartNumber,
    string PartNumberNormalized,
    string Description,
    string SearchText,
    string? Remark,
    string? Quantity,
    string? Model,
    string? Section,
    string? Illustration,
    int PageNumber);

public sealed record SearchCandidate(
    PartRecord Part,
    double Score,
    string MatchReason);

public sealed record AiMatch(
    string PartNumber,
    string Description,
    string Position,
    string Illustration,
    int Page,
    string Qty,
    string Model,
    string Remark,
    string Fitment,
    double Confidence);

public sealed record AiAnswer(
    string Answer,
    IReadOnlyList<AiMatch> Matches,
    bool NeedsClarification,
    string? ClarificationQuestion);
```

## Prompt payload object

```csharp
public sealed record PromptContext(
    string UserQuestion,
    VehicleContext VehicleContext,
    IReadOnlyList<SearchCandidate> Candidates,
    IReadOnlyList<string> PageSnippets);
```
