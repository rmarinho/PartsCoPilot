# Roy — History

## Core Context

- **Project:** A .NET MAUI AI-assisted parts catalog app for classic Porsche 911/912 manuals
- **Role:** Backend Dev
- **Joined:** 2026-03-17T15:54:43.743Z

## Learnings

<!-- Append learnings below -->

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
