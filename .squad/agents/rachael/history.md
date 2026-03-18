# Rachael — History

## Core Context

- **Project:** A .NET MAUI AI-assisted parts catalog app for classic Porsche 911/912 manuals
- **Role:** AI Dev
- **Joined:** 2026-03-17T15:54:43.744Z

## Learnings

<!-- Append learnings below -->

### 2025-07-17 — Prompt Injection Fix (Issue #1)
- `PromptBuilder.BuildPrompt()` had raw user input interpolated at line 91 — classic injection vector.
- Fix: wrapped user input in `<user_query>` delimiter tags, added Rule 10 to system preamble marking tag contents as untrusted.
- Sanitization: trim → 500-char truncation → escape `<user_query>` / `</user_query>` tokens in user input to prevent tag breakout.
- Test project uses source-file linking — must add `<Compile Include>` in test csproj for any new source file tests need. Added `PromptBuilder.cs` link.
- 25 new tests cover: normal operation, 4 adversarial injection patterns, delimiter escape, length limits, null/empty/whitespace, special characters (including Unicode), and full context integration.
- The system preamble itself references `<user_query>` in Rule 10 — tests must account for this when counting tag occurrences (split gives N+1).
- PR #26 opened, 102/102 tests green, maccatalyst build clean.

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

### 2026-03-17 — Context Window Management (#9, PR #30)
- **TokenEstimator**: Character-based heuristic (~4 chars/token) for estimating prompt token counts. Conservative upper bound prevents context window overruns.
- **ContextBudget**: Configurable token limits (MaxContextTokens: 4000, MaxSnippetTokens: 1000, MaxDescriptionLength: 200). Allows model-specific tuning (GPT-4o: 128K, GPT-4o-mini: 16K).
- **ContextTrimmer**: Sorts candidates by relevance score descending, preserves high-relevance results, drops low-relevance when budget exceeded. Truncates long descriptions with '...' suffix. Logs warnings when content is dropped.
- **PromptBuilder integration**: Now uses ContextTrimmer (backward compatible, optional constructor). Existing code without trimmer continues to work.
- **Key files**: `Services/TokenEstimator.cs`, `Services/ContextTrimmer.cs`, `Services/PromptBuilder.cs`, `Models/AiModels.cs` (removed duplicate ContextBudget).
- **Testing**: 25 unit tests covering token estimation, budget enforcement, relevance ordering, truncation logic. All passing.
- **Consolidation**: Moved ContextBudget from Models to Services namespace to eliminate duplicate definitions.

### Cross-Team Updates (2026-03-17T18:30:00Z)

**Roy's Sprint #31 — Database Migration + Parser Fixes:**
- Versioned migration system with column-exists check for idempotency
- N+1 query fix in SearchPartsAsync (parameterized SQL, composite index)
- Parser improvements: hyphenated part numbers, fractional quantities, case-insensitive illustration matching
- All 86 tests passing (24 new migration + parser coverage)
- **Impact for Rachael:** PartsRepository now supports pagination, reduced memory footprint improves AI context efficiency

**Pris's UI Work (from previous session):**
- Manual Viewer Page: text-based rendering of ManualPage.RawText from SQLite
- Compare Parts flow: embedded search panel for Part B selection, 9-field side-by-side comparison
- Shell restructure: Tab-based navigation (Home, Search, Favorites)
- **Impact for Rachael:** ManualNavigationService integration enables page navigation in search results
