# Rachael — History

## Core Context

- **Project:** A .NET MAUI AI-assisted parts catalog app for classic Porsche 911/912 manuals
- **Role:** AI Dev
- **Joined:** 2026-03-17T15:54:43.744Z

## Learnings

<!-- Append learnings below -->

### 2025-07-15 — AI Layer Bootstrap
- Added `Microsoft.SemanticKernel` 1.54.0 to the project. NU1904 warning on SK.Core — known upstream vuln, not actionable on our side yet.
- `IPromptBuilder` / `PromptBuilder`: System prompt enforces grounded-only answers, no invented part numbers, structured JSON output matching `AiAnswer`. Accepts `PromptContext` with candidates + snippets.
- `IPartsAiService` / `PartsAiService`: Uses SK `IChatCompletionService` with `json_object` response format. Strips markdown fences defensively. Falls back to a safe `AiAnswer` on empty/invalid responses.
- `IManualNavigationService` / `ManualNavigationService`: Maps `PartRecord` → page number, illustration group, and full `ManualPage`/`IllustrationGroup` via `IPartsRepository`.
- DI: All three registered as singletons. Kernel built from env vars `OPENAI_API_KEY` and `OPENAI_MODEL` (defaults to `gpt-4o-mini`).
- Fixed pre-existing build break: `Page` ambiguity in `PdfIngestionService.cs` (PdfPig vs MAUI Controls) by fully qualifying the type.
- Project targets `net11.0` — XAML binding warnings are pre-existing and unrelated to AI layer.

### 2026-03-17 — Integration Points with Roy & Pris
- **Roy's seed data (25 parts):** Ready to feed AI context. Seed data in `SeedDataService` provides candidates for PromptBuilder.
- **Pris's FavoritesPage & PartDetailsPage:** Both can now leverage ManualNavigationService for page navigation. Commands (`OpenPageCommand`) are wired to both SearchViewModel and PartDetailsViewModel.
- **Pris's result card actions:** AI service ready to power result ranking once integrated with SearchViewModel.
