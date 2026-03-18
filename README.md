# PartsCopilot

[![.NET Build](https://github.com/rmarinho/PartsCopilot/actions/workflows/ci.yml/badge.svg)](https://github.com/rmarinho/PartsCopilot/actions/workflows/ci.yml)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

**AI-assisted parts catalog for classic Porsche 911/912 manuals** — Find the right part, fast, using natural language search powered by semantic AI.

![Homepage](docs/images/placeholder-home.png) ![Search Results](docs/images/placeholder-search.png) ![Part Details](docs/images/placeholder-details.png)

## What It Does

PartsCopilot transforms classic Porsche 911/912 parts manuals into an intelligent, searchable database. Upload a PDF manual, then search for parts using natural language ("find all carburetors for 1968 911") or exact part numbers. The app combines full-text search with AI-powered semantic ranking to surface the most relevant parts from the manual, along with illustrations, compatibility info, and cross-references.

### Key Features

- **AI Search:** Natural language queries grounded in the actual manual content (no hallucinations)
- **Hybrid Ranking:** Combines exact matches, text search, and semantic similarity for relevance
- **Manual Viewer:** View extracted page text with illustration references inline
- **Part Comparison:** Side-by-side comparison of two parts with color-coded differences
- **Favorites:** Save frequently searched parts for quick access
- **Cross-Platform:** Runs on Android, iOS, Mac Catalyst, and Windows

## Prerequisites

### System Requirements

- **macOS** (13+) for building Mac Catalyst and iOS versions
- **Windows** (10.0.17763.0+) for Windows builds
- **Linux** for building Android targets

### Developer Setup

1. **.NET 11 Preview SDK** — [Download](https://aka.ms/dotnet/preview)
   ```bash
   dotnet --version  # Should show 11.0.x-preview
   ```

2. **Workloads** — Install platform-specific workloads:
   ```bash
   # macOS (Mac Catalyst + iOS):
   dotnet workload install maui-maccatalyst maui-ios
   
   # Android (any platform):
   dotnet workload install maui-android
   
   # Windows:
   dotnet workload install maui-windows
   ```

3. **Platform-Specific Requirements:**
   - **macOS:** Xcode 15+ (install via App Store or `xcode-select --install`)
   - **Android:** Android SDK API 21+, JDK 21 (managed by `dotnet workload` installer)
   - **iOS:** iOS 15.0+ deployment target
   - **Windows:** Windows App SDK 1.4+

### Optional: AI Features

To enable AI-powered search, you'll need:
- OpenAI API key ([get one here](https://platform.openai.com/api-keys))
- Set environment variable: `export OPENAI_API_KEY=sk-...`
- Optional: `export OPENAI_MODEL=gpt-4` (defaults to gpt-3.5-turbo)

## Getting Started

### Clone & Restore

```bash
git clone https://github.com/rmarinho/PartsCopilot.git
cd PartsCopilot
dotnet restore
```

### Build

**For Mac Catalyst (macOS):**
```bash
dotnet build -f net11.0-maccatalyst -c Release
```

**For iOS Simulator:**
```bash
dotnet build -f net11.0-ios -c Release
```

**For Android Emulator:**
```bash
dotnet build -f net11.0-android -c Release
```

**For Windows:**
```bash
dotnet build -f net11.0-windows10.0.19041.0 -c Release
```

### Run

**Mac Catalyst:**
```bash
dotnet run -f net11.0-maccatalyst
```

**iOS Simulator (requires Xcode):**
```bash
dotnet run -f net11.0-ios
```

**Android Emulator:**
```bash
dotnet run -f net11.0-android
```

**Windows:**
```bash
dotnet run -f net11.0-windows10.0.19041.0
```

### First Steps in the App

1. **Home Screen** — Displays app intro and quick-start guide
2. **Search Screen** — Enter a part number or natural language query
3. **Results** — Tap a part card to view full details
4. **Details** — View illustrations, full description, and cross-references
5. **Favorites** — Save parts for later by tapping the heart icon

## Architecture

### Overview

The app follows a layered MVVM + clean architecture pattern:

```
UI Layer (MAUI Views + ViewModels)
    ↓
Application Services (Orchestration)
    ↓
Domain Services (Search, AI, Parsing)
    ↓
Infrastructure (Database, PDF, Files)
```

### Key Layers

| Layer | Responsibility | Technologies |
|-------|---|---|
| **Presentation** | XAML pages, ViewModels, data binding | MAUI, CommunityToolkit.Mvvm |
| **Application** | Workflow orchestration, DTO mapping | Dependency Injection |
| **Domain** | Business logic, search algorithms, AI prompts | Semantic Kernel, LLMs |
| **Infrastructure** | SQLite, file I/O, PDF extraction | sqlite-net-pcl, PdfPig |

### Core Services

#### `IPdfIngestionService`
Extracts raw text and page metadata from uploaded PDF manuals using PdfPig.

#### `IManualParser`
Parses extracted text into structured part records (part numbers, descriptions, illustrations).  
Implements: `PorscheClassicManualParser` for the classic 911/912 format.

#### `IPartsRepository`
Local SQLite database for storing and querying:
- `PartRecords` — part numbers, descriptions, specs
- `ManualPages` — raw text, illustrations, sections
- `VehicleTypes`, `EngineTypes`, `TransmissionTypes` — metadata
- `SearchHistory` — user searches
- `Favorites` — user-saved parts

#### `ISearchService`
Hybrid search combining:
1. **Exact match** on part number
2. **Text search** on description/fields
3. **Semantic ranking** via Semantic Kernel + OpenAI

#### `IPartsAiService`
Calls LLM (OpenAI) to interpret queries and rank candidates. Uses `IPromptBuilder` to create grounded prompts that can only reference retrieved parts (prevents hallucinations).

#### `IManualNavigationService`
Maps part records to PDF pages and illustrations for the manual viewer.

### Data Flow

1. **Ingestion** → User selects PDF → `IPdfIngestionService` extracts pages → `IManualParser` structures data → `IPartsRepository` stores in SQLite
2. **Search** → User enters query → `ISearchService` retrieves candidates → `IPartsAiService` ranks via AI → UI renders results
3. **Manual Viewer** → User taps "View Page" → `IManualNavigationService` looks up page number → `IPartsRepository` fetches text → UI displays

### Technology Stack

- **UI Framework:** .NET MAUI 11.0 preview
- **MVVM Toolkit:** CommunityToolkit.Mvvm 8.4.0
- **Database:** SQLite 1.9.172 (sqlite-net-pcl)
- **PDF Extraction:** PdfPig 0.1.13
- **AI Orchestration:** Semantic Kernel 1.73.0
- **Logging:** Microsoft.Extensions.Logging
- **Testing:** xUnit 2.4.1, Moq

## Testing

### Run All Tests

```bash
dotnet test
```

### Run Specific Test Class

```bash
dotnet test --filter ClassName=PartsRepositoryTests
```

### Run with Verbose Output

```bash
dotnet test -v d
```

### Test Structure

Tests are located in `tests/PartsCopilot.Tests/` and are organized by service:

- **PartsRepositoryTests** — Database queries, Save/Get operations
- **HybridSearchServiceTests** — Search ranking, relevance scoring
- **PartsAiServiceTests** — Prompt generation, AI response parsing
- **ManualParserTests** — PDF parsing, part extraction

**Note:** Tests use xUnit and can run independently of the UI layer.

## Project Structure

```
PartsCopilot/
├── Views/                      # MAUI XAML pages (HomePage, SearchPage, etc.)
├── ViewModels/                 # MVVM ViewModels (HomeViewModel, SearchViewModel, etc.)
├── Services/                   # Domain & application services
│   ├── Interfaces.cs           # ISearchService, IPdfIngestionService, etc.
│   ├── HybridSearchService.cs  # Multi-strategy search implementation
│   ├── PdfIngestionService.cs  # PDF extraction via PdfPig
│   └── PorscheClassicManualParser.cs  # Manual parsing logic
├── Models/                     # Domain models
│   ├── AppModels.cs           # PartRecord, ManualMetadata, FavoriteEntry, etc.
│   ├── SearchModels.cs        # SearchQuery, SearchResult
│   ├── AiModels.cs            # AI prompt/response DTOs
│   └── ManualModels.cs        # ManualPage, IllustrationGroup
├── Data/                       # Data layer
│   ├── AppDatabase.cs         # SQLite connection & initialization
│   ├── PartsRepository.cs     # CRUD operations
│   ├── UserDataRepository.cs  # Favorites, search history
│   └── Entities.cs            # Database entity definitions
├── MauiProgram.cs             # Dependency injection & MAUI bootstrap
├── AppShell.xaml              # App navigation shell
├── App.xaml                   # Global app resources
├── PartsCopilot.csproj        # Project file (TFMs, workloads, NuGet refs)
├── docs/                      # Architecture & design docs
├── tests/                     # xUnit test suite
└── Resources/                 # App icons, fonts, images

```

## Contributing

### Code Style

- **Naming:** PascalCase for public members, camelCase for local variables
- **Nullability:** Enable nullable reference types; prefer records and required fields
- **Dependencies:** Use interface-based DI; avoid tight coupling
- **Async:** Use `async`/`await` throughout; prefer `CancellationToken` parameters

### Before You Commit

1. **Build:** `dotnet build` — must pass with 0 errors
2. **Test:** `dotnet test` — all tests must pass
3. **Format:** Code should follow MAUI conventions (no special formatter required)

### PR Workflow

1. Create a feature branch: `git checkout -b squad/XX-issue-title`
2. Make changes and test locally
3. Commit with a clear message: `git commit -m "Fix N+1 query in PartsRepository.SearchAsync"`
4. Push: `git push origin squad/XX-issue-title`
5. Open a PR with context (what, why, how tested)

### Issues to Work On

Check [GitHub Issues](https://github.com/rmarinho/PartsCopilot/issues?q=is%3Aopen+is%3Aissue) for:
- **P0 (Blockers):** Security, data correctness
- **P1 (Quality):** Testing, docs, accessibility
- **P2 (Polish):** UI refinement, dark mode, localization
- **P3 (Future):** Platform testing, distribution

## Roadmap

### MVP (Current)
- ✅ PDF ingestion & parsing
- ✅ Hybrid search (exact + semantic)
- ✅ Manual viewer (text-based)
- ✅ Part comparison
- ✅ Favorites/history

### V1 (Production)
- 🔄 PDF page rendering (diagrams)
- 🔄 Dark mode
- 🔄 Accessibility (VoiceOver, TalkBack)
- 🔄 Settings page (API key management)
- 🔄 Comprehensive test coverage

### V2 (Enhancement)
- 📋 Multiple manuals support
- 📋 Localization
- 📋 Cloud sync
- 📋 Offline mode optimization

## Troubleshooting

### Build Fails with `.NET 11 not found`
Install .NET 11 preview SDK:
```bash
# macOS (Homebrew):
brew install --cask dotnet-sdk11-preview

# Or download from: https://aka.ms/dotnet/preview
```

### Workload Installation Fails
```bash
dotnet workload restore
```

### Database Issues
Delete the local database and reseed:
```bash
rm ~/Library/Application\ Support/PartsCopilot/partscopilot.db3
```

### AI Search Not Working
- Verify API key: `echo $OPENAI_API_KEY`
- Check Settings page for API key configuration (once implemented)
- Review AI service logs in Debug output

## License

This project is licensed under the **MIT License** — see [LICENSE](LICENSE) for details.

## Support

- **Issues:** [GitHub Issues](https://github.com/rmarinho/PartsCopilot/issues)
- **Discussions:** [GitHub Discussions](https://github.com/rmarinho/PartsCopilot/discussions)
- **Documentation:** [docs/](docs/) folder for architecture, design decisions, and technical specs

---

Built with ❤️ by Rui Marinho and the PartsCopilot team. AI-assisted parts retrieval for classic cars.
