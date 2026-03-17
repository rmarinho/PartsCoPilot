# Roy — History

## Core Context

- **Project:** A .NET MAUI AI-assisted parts catalog app for classic Porsche 911/912 manuals
- **Role:** Backend Dev
- **Joined:** 2026-03-17T15:54:43.743Z

## Learnings

<!-- Append learnings below -->

### 2026-03-17 — Week 1 Delivery: SemanticKernel Upgrade + AI Resilience — COMPLETED

**Upgrade: Microsoft.SemanticKernel 1.54.0 → 1.73.0**
- Resolved critical vulnerability NU1904 (GHSA-2ww3-72rp-wpp4)
- Minor version bump, backward compatible (SK follows semver)
- Rachael's AI layer implementation unaffected

**Resilience hardening added to PartsAiService:**
- 30-second timeout per LLM call via `CancellationTokenSource.CreateLinkedTokenSource` + `CancelAfter`
- Up to 3 retries with exponential backoff: 1s, 3s, 8s
- Retryable conditions: 429 (rate limit), 502, 503, 504, 408
- Caller cancellation always propagates immediately (only timeout triggers retry)
- Empty responses also retry before falling back
- BCL-only implementation (no Polly dependency)

**Validation:**
- ✅ `dotnet build -f net11.0-maccatalyst` — 0 errors, NU1904 gone
- ✅ `dotnet test` — 72/72 tests passing
- ✅ No breaking API changes

**Cross-team impact:**
- Rachael: SK kernel builder API unchanged
- Pris: No UI impact
- All: Vulnerability resolved, AI calls resilient to transient failures

**Files changed:**
- `PartsCopilot.csproj` — version bump
- `Services/PartsAiService.cs` — retry + timeout logic

---

### 2026-03-17 — Test Suite Expansion (28 → 72 tests) — COMPLETED

**New test files added:**
- `UserDataRepositoryTests.cs` — 12 tests: favorites CRUD (save, get, remove, upsert, ordering, isolation), search history (save, get, ordering, limit, empty state)
- `ManualNavigationServiceTests.cs` — 14 tests: GetPageNumber, GetIllustrationGroup, GetPageAsync, GetIllustrationAsync, GetIllustrationGroupsForManualAsync, null argument handling (ArgumentNullException), missing data returns null/empty, case-insensitive illustration lookup
- `EdgeCaseTests.cs` — 18 tests: empty/whitespace search, special characters (slash, dash, unicode, SQL injection), case-insensitive search, ManualId filtering, SeedDataService idempotency (SeedAsync called twice = no duplicates), exact seed table counts, SeedIfEmpty skip when data exists, repository null/missing key handling, VehicleContext edge cases

**Issues found & addressed:**
- Seed data actually creates 26 parts (not 25 as documented in ManualMetadata.PartCount) — 5+6+5+5+5 across illustration groups
- Empty string search (`""`) matches all parts via `Contains("")` — documented as expected behavior, not a bug (caller should validate)
- Source file linking added for `UserDataRepository.cs` and `ManualNavigationService.cs` in test csproj

**Final count: 72 passing tests, 0 failures.**

**Cross-team coverage:**
- ManualNavigationServiceTests now validates page/illustration navigation used by Pris's ManualViewerViewModel
- UserDataRepositoryTests covers favorites and search history that underpin Pris's FavoritesPage and SearchPage
- EdgeCaseTests ensures robustness of seed data that populates all Pris UI screens

### 2026-03-17 — Backend Hardening Sprint

**Models added:**
- `LegendEntry` — legend/applicability entries per illustration (code, description, applicable models, year range, notes)
- `VehicleType` — vehicle type entries from summary pages (code, model, variant, year range, chassis range)
- `EngineType` — engine type entries (code, name, displacement, power, applicable models)
- `TransmissionType` — transmission type entries (code, name, type, applicable models)

**Entities & tables:** All four new models have corresponding SQLite entity classes in `Data/Entities.cs` with `ToDomain()`/`FromDomain()` mappers. Tables created in `AppDatabase.InitializeAsync()`.

**Repository:** `IPartsRepository` and `PartsRepository` extended with Save/Get methods for all new types.

**Seed data:** `Data/SeedDataService.cs` creates 25 realistic PartRecords across 5 illustration groups, 8 ManualPages, 8 VehicleTypes, 5 EngineTypes, 4 TransmissionTypes, and 5 LegendEntries. Registered as singleton in DI. Call `SeedIfEmptyAsync()` for idempotent seeding.

**Test project:** `tests/PartsCopilot.Tests/` — xUnit + FluentAssertions v8, 28 tests covering:
- HybridSearchService ranking (exact PN > description > word overlap, context filtering)
- PorscheClassicManualParser (CanParse detection, row extraction, normalization, illustration assignment)
- PartsRepository CRUD (all entity types, cascade delete, search, seed service idempotency)

**Gotchas:**
- FluentAssertions v8 renamed `HaveCountGreaterOrEqualTo` → `HaveCountGreaterThanOrEqualTo`
- SQLite `:memory:` shares state across connections in same process; use temp files for isolated tests
- Main csproj needed `DefaultItemExcludes` for `tests\**` to prevent SDK glob from compiling test files
- Test project uses `<Compile Include="../../...">` links (not project reference) since MAUI projects can't be referenced by plain net11.0

### 2026-03-17 — Integration Points with Rachael & Pris
- **Rachael's AI layer:** Seed data (25 parts) is fed to PromptBuilder as AI context. ManualNavigationService queries repository for page lookups.
- **Pris's UI:** Seed data populates search results, favorites list, and part details. Pris integrated IPartsRepository for UI data binding.

### 2026-03-17 — SemanticKernel Upgrade & AI Resilience

**SemanticKernel upgrade:** `1.54.0` → `1.73.0` in PartsCopilot.csproj. This resolves critical vulnerability NU1904 (GHSA-2ww3-72rp-wpp4). Build confirmed zero NU1904 warnings.

**Retry/timeout policy added to PartsAiService:**
- 30-second timeout per LLM call via `CancellationTokenSource.CreateLinkedTokenSource` + `CancelAfter`
- Up to 3 retries with exponential backoff (1s, 3s, 8s) for transient HTTP failures
- Retryable conditions: 429 (rate limit), 502, 503, 504, 408 — detected via `HttpRequestException.StatusCode`
- Caller cancellation (`ct`) always propagates immediately — only timeout triggers retry
- Empty responses also retry before falling back
- No new dependencies added — pure BCL implementation, no Polly needed
- All 72 existing tests still pass after changes

**Files changed:**
- `PartsCopilot.csproj` — SK version bump
- `Services/PartsAiService.cs` — retry loop, timeout, transient detection
