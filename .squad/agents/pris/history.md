# Pris — History

## Core Context

- **Project:** A .NET MAUI AI-assisted parts catalog app for classic Porsche 911/912 manuals
- **Role:** UI Dev
- **Joined:** 2026-03-17T15:54:43.743Z

## Learnings

<!-- Append learnings below -->

### 2026-03-17 — Week 1 Delivery: Compare Parts + AI Service Hardening — COMPLETED

**Pris: Compare Parts Flow**
- Built ComparePartsPage + ComparePartsViewModel for side-by-side part comparison
- 9-field comparison: Model, Year, Engine, Transmission, Supplier, Cost, Weight, Availability, Notes
- Green/orange color-coding for field matches/differences
- Embedded search for Part B selection (scoped to same manual, excludes Part A)
- Swap/Change buttons for user control without navigation away
- Wired CompareCommand in PartDetailsViewModel
- ✅ Build: 0 errors (maccatalyst), feature fully integrated

**Roy: SemanticKernel Upgrade + AI Resilience**
- Upgraded Microsoft.SemanticKernel 1.54.0 → 1.73.0 (NU1904 resolved)
- Added 30s timeout per LLM call
- Up to 3 retries with exponential backoff (1s, 3s, 8s) for transient HTTP errors (429, 502, 503, 504, 408)
- BCL-only implementation (no Polly)
- ✅ Build: 0 errors, 72/72 tests passing

**All three Deckard Week 1 priorities now complete:**
1. ✅ Compare Parts flow (Pris)
2. ✅ SemanticKernel vulnerability + hardening (Roy)
3. ✅ AI layer implementation (Rachael, prior session)

---

### Compare Parts Flow — COMPLETED

**What was built:**
- `ComparePartsPage.xaml` + `.xaml.cs` — side-by-side comparison view for two parts
- `ComparePartsViewModel.cs` — receives Part A via navigation, provides in-page search to pick Part B
- `CompareFieldBackgroundConverter` — color-codes comparison rows (green tint = match, orange tint = difference)
- Registered route `compare-parts` in AppShell (detail route, same pattern as `part-details` and `manual-viewer`)
- Registered VM + Page in DI (MauiProgram.cs)
- Wired existing `CompareCommand` in `PartDetailsViewModel` to navigate to compare-parts with current part

**UI approach:**
- Part A shown as fixed card at top; Part B selected via in-page search
- Search uses `IPartsRepository.SearchPartsAsync()` scoped to same manual; excludes Part A from results
- After Part B selection, comparison table shows 9 fields side-by-side: Part #, Description, Model, Illustration, Section, Page, Quantity, Position, Remark
- Each row background color-coded: green tint (#1A4CAF50) for matching fields, orange tint (#1AFF9800) for differences
- Swap button lets user flip Part A/B; Change button clears Part B to search again
- CollectionView for search results with tap-to-select; search triggered by Enter or button

**Architecture notes:**
- Navigation uses `IQueryAttributable` with `Part` (PartRecord) parameter — consistent with existing detail routes
- VM depends only on `IPartsRepository` (no search service needed — `SearchPartsAsync` already does the job)
- Comparison logic uses `NormalizedEqual` helper for case-insensitive/whitespace-trimmed string comparison
- `NotifyComparisonChanged()` fires all computed property notifications when parts change

**Build status:** ✅ 0 errors (Mac Catalyst net11.0)

### 2026-03-17 — Integration Points with Rachael & Roy
- **Rachael's ManualNavigationService:** Now wired to both SearchViewModel and PartDetailsViewModel `OpenPageCommand`. When user taps "Open Page", the service navigates with page/illustration data.
- **Roy's seed data (25 parts):** FavoritesPage and search results now populated by SeedDataService. UI queries IPartsRepository for all domain data.
- **Roy's new models:** PartDetailsPage displays LegendEntry, VehicleType, EngineType, TransmissionType info in rich detail view.
- **Roy's test suite:** ManualViewerViewModel's navigation is now covered by ManualNavigationServiceTests (GetPageAsync, GetIllustrationAsync). Roy's EdgeCaseTests validate idempotent seed data that powers Pris's UI.

### 2026-03-17 — Manual Viewer Page (MVP) — COMPLETED

**What was built:**
- `ManualViewerPage.xaml` + `.xaml.cs` — full page viewer for PDF manual content
- `ManualViewerViewModel.cs` — loads ManualPage from repository, supports prev/next navigation
- Registered route `manual-viewer` in AppShell (detail route, same pattern as `part-details`)
- Registered VM + Page in DI (MauiProgram.cs)
- Wired `OpenPageCommand` in both `SearchViewModel` and `PartDetailsViewModel` to navigate to manual-viewer with ManualId/PageNumber/Illustration params (replaces DisplayAlert placeholder)

**PDF rendering approach chosen: Text-based viewer (extracted content)**
- ManualPage.RawText already stored in SQLite from PdfPig ingestion pipeline
- No runtime PDF rendering needed — display extracted text in monospace font with styled container
- Zero new NuGet dependencies added
- Prev/next page navigation built into bottom bar
- Header shows manual title, page indicator, illustration badge, section badge
- Three states: loading (spinner), error (with Go Back), content (scrollable text)
- Used `Border` instead of deprecated `Frame` for .NET 11 compatibility

**Architecture notes:**
- Navigation uses `IQueryAttributable` with dictionary params: ManualId (string), PageNumber (int), Illustration (string?)
- VM queries `IPartsRepository.GetPageAsync()` and `GetManualAsync()` directly — ManualNavigationService not needed by VM since we navigate by ManualId+PageNumber
- Page count from `ManualMetadata.PageCount` drives prev/next enable/disable
- Route registered as detail route (not tab) — consistent with `part-details` pattern

**Build status:** ✅ 0 errors (Mac Catalyst)

### 2025-07-16 — UI Enhancement Sprint

**What was done:**
- Enhanced search result cards with favorite toggle (♡/♥) and Open Page action buttons
- Built FavoritesPage with tab-based UI (Favorites + Recent Searches)
- Built PartDetailsPage with full part info display and action buttons (favorite, open page, compare placeholder)
- Restructured Shell to TabBar with Home, Search, Favorites tabs
- Wired SearchAgain in history to navigate back to search with pre-filled query
- Extended FavoriteEntry/FavoriteEntity with Model, PageNumber, Illustration fields
- Added BoolToFavoriteIconConverter for heart toggle state
- Deleted dead MainPage.xaml/.cs (template leftover)
- SearchViewModel now checks favorite status for each result on search

**Architecture notes:**
- PartDetailsPage receives data via IQueryAttributable (dictionary-based navigation params)
- SearchPage now uses IQueryAttributable to accept a `query` param for search-again flow
- Shell routes: `home`, `search`, `favorites` are TabBar items; `part-details` is a registered detail route
- FavoritesPage uses a manual tab switcher (two buttons + visibility binding) rather than a TabView control
- All commands use CommunityToolkit.Mvvm [RelayCommand]; MAUIG2045 warnings about source-gen interop are expected and harmless

**Pre-existing issues found:**
- `tests/` directory is included in main project compilation (no `<Compile Remove="tests/**" />` in csproj) causing xUnit/FluentAssertions errors — not my domain to fix but worth flagging
- `DisplayAlert` deprecated in favor of `DisplayAlertAsync` in .NET 11 preview

### 2026-03-17 — Integration Points with Rachael & Roy
- **Rachael's ManualNavigationService:** Now wired to both SearchViewModel and PartDetailsViewModel `OpenPageCommand`. When user taps "Open Page", the service navigates with page/illustration data.
- **Roy's seed data (25 parts):** FavoritesPage and search results now populated by SeedDataService. UI queries IPartsRepository for all domain data.
- **Roy's new models:** PartDetailsPage displays LegendEntry, VehicleType, EngineType, TransmissionType info in rich detail view.

### 2026-03-17 — MauiDevFlow Setup
**What was done:**
- Added `Redth.MauiDevFlow.Agent` v0.23.1 NuGet package (debug-only condition)
- Registered MauiDevFlow agent in MauiProgram.cs inside `#if DEBUG` block
- Created `Platforms/MacCatalyst/Entitlements.Debug.plist` with `network.server` entitlement
- Configured csproj to use debug entitlements for Mac Catalyst debug builds

**Configuration notes:**
- Package uses wildcard version `*` to always pull latest stable
- Agent registration: `builder.AddMauiDevFlowAgent()` after all other services
- Mac Catalyst requires `com.apple.security.network.server` entitlement for agent HTTP server
- No `.mauidevflow` config file needed — broker handles port assignment automatically
- This is a standard MAUI app (not Blazor Hybrid), so only the Agent package is needed

**Platform support:**
- Mac Catalyst: Entitlements configured for debug builds
- iOS: No special setup needed
- Android: Will need `adb reverse/forward` when running on emulator
- This project targets net11.0 for iOS, Android, and Mac Catalyst

### Accessibility Audit + Dark Mode Support — COMPLETED (PR #29)

**Issues:** #5 (Accessibility) and #8 (Dark Mode)

**Accessibility (#5):**
- Added SemanticProperties.Description to all buttons, entries, labels, pickers, progress bars across HomePage and SearchPage
- Added SemanticProperties.Hint to all interactive controls (buttons, entries, pickers)
- Added SemanticProperties.HeadingLevel: Level1 for page title, Level2 for subtitle, Level3 for section headers and result card part numbers
- Added AutomationId to 31 controls for UI test automation
- Tab order is logical — follows DOM/layout order

**Dark Mode (#8):**
- Created 13 semantic color pairs in Colors.xaml with Light/Dark variants: PageBackground, CardBackground, TextPrimary, TextSecondary, TextMuted, ErrorText, BadgeText, PrimaryButton (bg+text), SecondaryButton (bg+text), GhostButtonText
- Replaced all hardcoded TextColor="Gray"/"White"/"Red" with AppThemeBinding references
- Cards, badges, buttons, metadata text all theme-aware
- Zero hardcoded color values remaining in Views/

**Build:** ✅ 0 errors (maccatalyst), only pre-existing MAUIG2045 warnings
**Branch:** squad/5-8-accessibility-dark-mode
**PR:** #29

---

### PR #33 — PDF Page Rendering in Manual Viewer (#3)
**Date:** 2025-07-18
**Issue:** #3 — Render actual PDF pages (not raw text)

**What I did:**
- Added PDFtoImage (PDFium wrapper) to pre-render PDF pages to PNG at import time
- Extended ManualPage/PageEntity with ImageData byte[] + migration 4
- Created PdfPageRenderer service with PDFium availability probing
- Updated ManualViewerPage with image display, pinch-to-zoom (1x-5x), pan, double-tap
- Added view mode toggle (image/text) with graceful fallback
- Fixed pre-existing duplicate type definitions in AppModels.cs

**Build:** ✅ 0 errors (maccatalyst), 187/188 tests pass (1 pre-existing failure)
**Branch:** squad/3-pdf-page-rendering
**PR:** #33

## Learnings

- **Branch management is critical**: Multiple local branches cause silent issues with git stash/pop. Always verify current branch with `git branch --show-current` before any operation.
- **PDFtoImage API namespaces**: Must use `PDFtoImage.Compatibility.Conversion` (not `PDFtoImage.Conversion`) — the non-Compatibility namespace uses different parameter types.
- **Migration idempotency**: Always check if tables exist before ALTER TABLE — tests may create partial DBs.
- **Save recovery copies**: When doing complex multi-file changes across branches, save files to /tmp as insurance.
