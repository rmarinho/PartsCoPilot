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

### 2026-03-17T17:17:02Z: User directive — Always validate implementation
**By:** Rui Marinho (via Copilot)  
**What:** Always validate that everything works when implementing something — make sure builds pass and changes are verified.  
**Why:** User request — captured for team memory.

---

### 2026-03-17T17:24:04Z: Manual Viewer — Text-Based Rendering for MVP (IMPLEMENTED)
**By:** Pris (UI Dev)  
**Status:** Implemented  
**Context:** Users need to view actual manual pages when they tap "Open Page" on a part. The app uses PdfPig for ingestion but PdfPig is an extraction library, not a renderer — it cannot produce page images.

**Options Considered:**
1. Text-based viewer (extracted content) — Display `ManualPage.RawText` from SQLite
2. Image-based (pre-render pages) — Render PDF pages to images at import time
3. WebView-based PDF viewer — Load PDF in a WebView using pdf.js
4. Third-party MAUI PDF viewer — e.g., Syncfusion, DevExpress

**Decision:** **Option 1: Text-based viewer.** The extracted text is already in the database from the ingestion pipeline. This approach:
- Adds zero new dependencies (no NuGet packages, no JS libraries)
- Works cross-platform with no platform-specific code
- Is fast — text from SQLite is instant vs. loading/rendering PDF files
- Supports prev/next page navigation naturally
- Displays illustration and section metadata alongside content

**Trade-offs:**
- No visual fidelity — diagrams/illustrations from the original PDF are not shown
- Layout not preserved — tabular data may not align perfectly in plain text

**When to revisit:** When users request diagram viewing or lightweight MAUI PDF renderer becomes available. Option 2 (pre-rendered images at import) would be the natural next step.

**Files Changed:**
- `ViewModels/ManualViewerViewModel.cs` — new
- `Views/ManualViewerPage.xaml` + `.xaml.cs` — new
- `MauiProgram.cs` — DI registration
- `AppShell.xaml.cs` — route registration
- `ViewModels/SearchViewModel.cs` — OpenPage wired to navigation
- `ViewModels/PartDetailsViewModel.cs` — OpenPage wired to navigation

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

### 2026-03-17T17:38:13Z: Compare Parts Flow — In-Page Search for Part B Selection (IMPLEMENTED)
**By:** Pris (UI Dev)  
**Status:** Completed  
**Context:** Compare Parts feature required to allow users to select a second part (Part B) for side-by-side comparison against Part A.

**Decision:** Embedded search panel within ComparePartsPage.
- Search bar + results list inline — no multi-page navigation
- Search scoped to same manual (ManualId) to keep results relevant
- Part A always visible for context
- Swap/Change buttons allow iteration without navigation

**Comparison Engine:**
- 9-field side-by-side layout: Model, Year, Engine, Transmission, Supplier, Cost, Weight, Availability, Notes
- Color-coding: Green (`#1A4CAF50`) for matching fields, Orange (`#1AFF9800`) for differences
- Case-insensitive, whitespace-trimmed string comparison

**Files Changed:**
- `ViewModels/ComparePartsViewModel.cs` — new
- `Views/ComparePartsPage.xaml` + `.xaml.cs` — new
- `Converters/ValueConverters.cs` — CompareFieldBackgroundConverter
- `App.xaml` — converter registration
- `MauiProgram.cs` — DI registration
- `AppShell.xaml.cs` — route registration
- `ViewModels/PartDetailsViewModel.cs` — CompareCommand integrated

**Validation:**
- ✅ Build: 0 errors (maccatalyst)
- ✅ Feature fully wired end-to-end
- ✅ All Deckard Week 1 priorities now complete

---

### 2026-03-17T17:38:13Z: SemanticKernel Upgrade 1.54.0 → 1.73.0 + AI Service Hardening (IMPLEMENTED)
**By:** Roy (Backend Dev)  
**Status:** Completed  
**Context:** Build warning: NU1904 — Microsoft.SemanticKernel.Core 1.54.0 has critical severity vulnerability (GHSA-2ww3-72rp-wpp4).

**Decision:** Upgrade SemanticKernel to 1.73.0 (latest stable) + harden PartsAiService with resilience patterns.

**Upgrade:**
- Microsoft.SemanticKernel: 1.54.0 → 1.73.0 (minor version bump, backward compatible)
- NU1904 vulnerability eliminated

**Resilience:**
- **Timeout:** 30s per LLM call
- **Retries:** Up to 3 with exponential backoff
  - 1st: 1s, 2nd: 3s, 3rd: 8s
- **Scope:** Transient HTTP errors only (429, 502, 503, 504, 408)
- **Implementation:** BCL-only (no external retry libraries)

**Files Changed:**
- `PartsCopilot.csproj` — version bump
- `Services/PartsAiService.cs` — retry + timeout logic

**Validation:**
- ✅ `dotnet build -f net11.0-maccatalyst` — 0 errors, NU1904 gone
- ✅ `dotnet test` — 72/72 tests passing
- ✅ No breaking API changes (SK follows semver)

**Impact:**
- Rachael: SK kernel builder API unchanged
- Pris: No UI impact
- All: Vulnerability resolved, AI calls resilient

---

## Governance

- All meaningful changes require team consensus
- Document architectural decisions here
- Keep history focused on work, decisions focused on direction
