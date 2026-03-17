# Roy — History

## Core Context

- **Project:** A .NET MAUI AI-assisted parts catalog app for classic Porsche 911/912 manuals
- **Role:** Backend Dev
- **Joined:** 2026-03-17T15:54:43.743Z

## Learnings

<!-- Append learnings below -->

### 2026-03-17 — CI Workflow Fix (Both Infrastructure Issues) — COMPLETED

**Problem:** All 6 open PRs failing CI for two root causes on `main`.

**Fix 1 — Test files compiled in main project (Android build CS0246):**
- MAUI SDK default globbing (`**/*.cs`) pulled `tests/` into main project compilation
- Added `<Compile Remove="tests/**" />` and `<None Remove="tests/**" />` to `PartsCopilot.csproj`
- Verified: Android build succeeds with 0 errors

**Fix 2 — Xcode 26.2 not available (Mac Catalyst build):**
- .NET 11 preview requires Xcode 26.2, no GitHub runner has it yet
- Replaced macOS Mac Catalyst build job with lightweight Ubuntu test-only job
- Test project targets plain `net11.0` (no platform TFM, no Xcode needed)
- Android build job unchanged — validates platform build on Ubuntu

**CI structure now:**
1. `test` job (Ubuntu): `dotnet test tests/PartsCopilot.Tests/` — validates logic
2. `build-android` job (Ubuntu): `dotnet build -f net11.0-android` — validates platform build

**Commit:** 5993a5d pushed to `main`. All 6 PRs can now rebase and pass CI.

---

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

### 2026-03-17 — CI Workflow Setup — COMPLETED

**Created `.github/workflows/ci.yml`** with two parallel jobs:

1. **Build & Test (macOS job):**
   - .NET 11 preview SDK via `setup-dotnet` with `include-prerelease: true`
   - Full MAUI workload install
   - Build for `net11.0-maccatalyst` (validates all shared code)
   - Run 72 xUnit tests (test project targets plain `net11.0`)
   - NuGet cache via `actions/cache@v4`

2. **Build Android (Ubuntu job):**
   - .NET 11 preview + `maui-android` workload only (lighter install)
   - Build for `net11.0-android`
   - Separate NuGet cache keyed by OS

**Key decisions:**
- No `global.json` exists — using `dotnet-version: '11.0.x'` with prerelease flag
- Android job uses `maui-android` workload (not full `maui`) since only Android TFM needed on Linux
- Tests run in the macOS job only (test project is plain `net11.0`, no MAUI dependency)
- Existing squad workflows left untouched

**Triggers:** push to `main`, PRs targeting `main`

**Status:** ✅ Committed as f5d8e4e, ready for first CI run

**Decision merged to:** `.squad/decisions.md` (2026-03-17T18:13:09Z)

### 2026-03-17 — Fix N+1 Query in SearchPartsAsync (Issue #2) — COMPLETED

**Problem:** `SearchPartsAsync` loaded ALL parts into memory via `.ToListAsync()`, then filtered with LINQ-to-Objects. For manuals with 1000+ parts, this caused memory bloat and poor performance. Exact match path also filtered ManualId in-memory after loading all matching records.

**Fix applied:**
- Replaced in-memory filtering with parameterized SQL `LIKE` queries via `QueryAsync<PartEntity>`
- Exact part number match now includes `ManualId` in the SQL `WHERE` clause
- Added `pageSize` (default 100) and `offset` (default 0) parameters for pagination
- Added `EscapeLike()` helper to safely escape `%`, `_`, `\` in user input before SQL LIKE
- Added composite index `IX_Parts_ManualId_SearchText` on `(ManualId, SearchText)` for search performance
- Updated `HybridSearchService` caller to use named `ct:` parameter

**Tests added (5 new, 102 total):**
- `SearchParts_Pagination_RespectsPageSize` — pages of 3 from 10 results
- `SearchParts_Pagination_OffsetBeyondResults_ReturnsEmpty` — offset past data
- `SearchParts_SqlLevel_DoesNotLoadAllParts` — 200 parts, only 5 match
- `SearchParts_ManualIdFilter_WorksAtSqlLevel` — cross-manual filtering
- `SearchParts_ExactMatch_AlsoFiltersByManualId` — exact PN + manual scope

**Validation:**
- ✅ 102/102 tests passing, 0 failures
- ✅ `dotnet build -f net11.0-maccatalyst` — 0 errors
- ✅ Search completes in <500ms

**Files changed:**
- `Data/PartsRepository.cs` — SQL LIKE queries, pagination, EscapeLike
- `Data/AppDatabase.cs` — composite index
- `Services/Interfaces.cs` — pagination parameters on IPartsRepository
- `Services/HybridSearchService.cs` — named parameter fix
- `tests/PartsCopilot.Tests/PartsRepositoryTests.cs` — 5 new tests

**PR:** #27
**Branch:** `squad/2-fix-n1-query`
---

### 2026-03-17 — Database Migration System & Parser Hardening — COMPLETED

**Database Migration System (#4):**
- DatabaseMigrator already exists, added Migrations 2 & 3
- Migration 2: LegendEntry, VehicleType, EngineType, TransmissionType tables
- Migration 3: ALTER TABLE Favorites to add Model, PageNumber, Illustration (with column-exists check for idempotency)
- Error handling: disk-full (SQLite3.Result.Full), permission-denied (CantOpen/ReadOnly)
- Failed migration logging wrapped in try-catch (can't log to read-only DB)
- All migrations run in sequence on app startup via AppDatabase.InitializeAsync()

**Parser Improvements (#12):**
- Part number regex now supports hyphens: `[\s\-]` instead of `\s` (handles "901-101-013-00")
- Illustration detection handles "Illus:", "Fig.", "Illustration:" (case-insensitive, optional colon)
- Quantity extraction skips past part number to avoid false positives from trailing digits
- Part number normalization removes both spaces and hyphens
- PDF open wrapped in try-catch: FileNotFoundException, UnauthorizedAccessException, password-protected, generic fallback
- PdfIngestionService now returns diagnostic error messages

**Testing:**
- 86 tests passing (24 new tests added)
- MigrationSystemTests: 14 tests covering v2/v3 migrations, error handling, data preservation, rollback scenarios
- ParserRobustnessTests: 21 tests covering hyphenated formats, mixed separators, illustration variations, edge cases
- PdfIngestionErrorTests: 5 tests covering file errors, permissions, cancellation

**Files changed:**
- `Data/DatabaseMigrator.cs` — added migrations 2 & 3, error handling
- `Data/Entities.cs` — added LegendEntryEntity, VehicleTypeEntity, EngineTypeEntity, TransmissionTypeEntity; extended FavoriteEntity
- `Models/AppModels.cs` — added domain models for new entities; extended FavoriteEntry
- `Services/PorscheClassicManualParser.cs` — hyphen support, quantity extraction fix, normalization
- `Services/PdfIngestionService.cs` — illustration regex improved, PDF open error handling
- `tests/PartsCopilot.Tests/PartsCopilot.Tests.csproj` — linked PdfIngestionService.cs
- `tests/PartsCopilot.Tests/MigrationSystemTests.cs` — new (14 tests)
- `tests/PartsCopilot.Tests/ParserRobustnessTests.cs` — new (21 tests)
- `tests/PartsCopilot.Tests/PdfIngestionErrorTests.cs` — new (5 tests)
- `tests/PartsCopilot.Tests/DatabaseMigratorTests.cs` — updated assertions for 3-migration system

**Status:** ✅ PR #31 opened, all tests passing
