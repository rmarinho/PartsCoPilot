# Squad Decisions

## Active Decisions

### 2026-03-17T16:22:00Z: User directive — Premium model for all agent spawns
**By:** Rui Marinho (via Copilot)  
**What:** Use claude-opus-4.6 for all agent spawns — no cost optimization, premium model everywhere.  
**Why:** User request — captured for team memory.

### 2026-03-17T16:21:00Z: Architecture decision — Keep single-project structure
**By:** Deckard (Lead)  
**What:** Keep the current flat single-project layout. Do NOT restructure to multi-project.  
**Why:**
1. ~25 source files would all need to move and have namespaces rewritten
2. Current folder structure (`Models/`, `Services/`, `Data/`, `ViewModels/`, `Views/`) already provides clean separation of concerns
3. MVVM pattern, DI, repository pattern, and interface-based services are all properly implemented
4. Restructuring risk: introduces bugs in a working pipeline for zero user-facing benefit
5. This app is a single-target product, not a shared library ecosystem — multi-project adds overhead without payoff at this scale

**When to revisit:** If we add a second app target (e.g., a CLI tool or Blazor web version), extract `Core` and `Infrastructure` projects at that point.

---

## Project Status: Week 1 Priorities

**Deckard Assessment Complete (2026-03-17T16:21:00Z)**

| Task | Status | Owner | Unblocks |
|------|--------|-------|----------|
| IPromptBuilder implementation | Pending | Rachael | AI service |
| IPartsAiService implementation | Pending | Rachael | AI search UI |
| Semantic Kernel NuGet integration | Pending | Rachael | AI service |
| LegendEntry model + parsing | Pending | Roy | Domain model completion |
| Seed data mechanism | Pending | Roy | Pris's UI dev |
| xUnit test project setup | Pending | Roy | CI/CD confidence |
| Result card action buttons | Pending | Pris | Core UX |
| Favorites/History UI screen | Pending | Pris | User retention |

**Codebase Maturity:** ~60% complete  
**Core Working:** PDF ingestion → parsing → SQLite storage → hybrid search → search UI  
**Gaps:** AI layer, manual viewer, compare flow, secondary UI screens

---

---

## Week 1 Agent Decisions (2026-03-17T16:44:08Z)

### AI Layer Implementation Choices (Rachael)

**Context:** AI layer was missing. Interfaces existed but had no implementations.

**Decisions:**
1. **Semantic Kernel 1.54.0** as orchestration layer — keeps us decoupled from specific provider
2. **JSON-mode response format** — reliable structured output with defensive markdown stripping
3. **API key via environment variables** — `OPENAI_API_KEY`, `OPENAI_MODEL` read at startup with fallback defaults
4. **Grounded-only system prompt** — model can only reference candidates passed in context; must ask for clarification when ambiguous
5. **ManualNavigationService uses IPartsRepository** — delegates page/illustration lookups to existing repository
6. **Fixed PdfIngestionService build break** — fully qualified `UglyToad.PdfPig.Content.Page` to resolve MAUI ambiguity

**Open items:** SK.Core 1.54.0 NU1904 vulnerability needs monitoring; API key config should move to settings page; add retry/timeout policies.

---

### Backend Hardening Decisions (Roy)

**Storage model:** All 9 tables from `docs/02-architecture.md` now created.

**Seed data convention:**
- Manual ID: `seed-911-912-1965-1969`
- File path uses `seed://` scheme
- `SeedDataService.SeedIfEmptyAsync()` is idempotent
- Registered as singleton in DI

**Test project structure:**
- Located at `tests/PartsCopilot.Tests/`
- Uses source file linking (MAUI incompatibility workaround)
- Main csproj excludes `tests/**` via `DefaultItemExcludes`
- New source files need `<Compile Include>` link in test csproj

**New repository methods:** `IPartsRepository` now includes Save/Get for LegendEntries, VehicleTypes, EngineTypes, TransmissionTypes.

---

### UI Completion — Shell, Favorites, Part Details (Pris)

**Shell restructure:**
- From single ShellContent to TabBar with three tabs: Home, Search, Favorites
- `part-details` registered as detail route (not tab)
- Navigation uses `IQueryAttributable` for data passing

**FavoriteEntry schema extended:**
- Added Model, PageNumber, Illustration fields
- Enables favorites to display without extra DB lookups
- Migration needed if production data exists (SQLite AddColumn)

**Navigation patterns:**
1. Tab navigation is primary pattern — Home/Search/Favorites are equal peers
2. `ManualViewer` placeholder: `OpenPageCommand` shows DisplayAlert with page/illustration info
3. Compare placeholder: PartDetailsPage has Compare button wired to placeholder alert

**Pre-existing issue:** `tests/` directory compiled into main project — should add `<Compile Remove="tests/**" />` to csproj.

---

### 2026-03-17T17:09:58Z: Development tooling — Enable MauiDevFlow for remote debugging
**By:** Pris (UI Dev)  
**What:** Integrated MauiDevFlow v0.23.1 (`Redth.MauiDevFlow.Agent`) into PartsCopilot for remote app debugging and visual inspection during development.  
**Why:**
1. Enables remote visual tree inspection from command line without manual IDE debugging
2. Screenshot verification and UI interaction testing (tap, fill, navigate) for simulator/device testing
3. Live property inspection and modification without full rebuild cycles
4. Accelerates develop-deploy-inspect-fix feedback loop, especially critical for mobile testing

**Configuration:**
- Debug-only NuGet package (wildcard `*` for automatic updates)
- Registered via `builder.AddMauiDevFlowAgent()` in `#if DEBUG` block in MauiProgram.cs
- Mac Catalyst debug build uses separate `Platforms/MacCatalyst/Entitlements.Debug.plist` with `com.apple.security.network.server` entitlement
- Broker-based port discovery — no manual `.mauidevflow` configuration needed

**Platform Coverage:**
- ✅ Mac Catalyst (entitlements configured)
- ✅ iOS Simulator (out of box)
- ⚠️ Android Emulator (requires `adb` port forwarding — future documentation)

**Files Modified:**
- `PartsCopilot.csproj` — Added `Redth.MauiDevFlow.Agent` v0.23.1 (Debug)
- `MauiProgram.cs` — Added `builder.AddMauiDevFlowAgent()` in DEBUG block
- `Platforms/MacCatalyst/Entitlements.Debug.plist` — Created with network server capability

---

## Governance

- All meaningful changes require team consensus
- Document architectural decisions here
- Keep history focused on work, decisions focused on direction
