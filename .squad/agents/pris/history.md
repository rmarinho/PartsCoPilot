# Pris — History

## Core Context

- **Project:** A .NET MAUI AI-assisted parts catalog app for classic Porsche 911/912 manuals
- **Role:** UI Dev
- **Joined:** 2026-03-17T15:54:43.743Z

## Learnings

<!-- Append learnings below -->

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
