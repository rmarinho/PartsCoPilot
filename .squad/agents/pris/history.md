# Pris — History

## Core Context

- **Project:** A .NET MAUI AI-assisted parts catalog app for classic Porsche 911/912 manuals
- **Role:** UI Dev
- **Joined:** 2026-03-17T15:54:43.743Z

## Learnings

<!-- Append learnings below -->

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
