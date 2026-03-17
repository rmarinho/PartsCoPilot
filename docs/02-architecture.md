# Technical Architecture

## Stack

- `.NET 10`
- `.NET MAUI`
- `CommunityToolkit.Mvvm`
- `CommunityToolkit.Maui`
- `SQLite`
- `PdfPig` or equivalent PDF text extraction library
- `Semantic Kernel`
- OpenAI `Responses API`

## High-level architecture

```text
UI (MAUI Views + ViewModels)
    -> Application Services
        -> Search + Retrieval Layer
            -> SQLite / local index
            -> PDF page metadata
        -> AI Orchestration Layer
            -> Semantic Kernel
            -> Responses API
```

## Main app layers

### Presentation
Responsible for:
- search screen
- filters
- results cards
- manual viewer
- compare screen
- saved searches

### Application
Responsible for:
- coordinating search flow
- composing retrieval requests
- building prompts
- mapping model responses to DTOs

### Domain
Responsible for:
- part records
- vehicle context
- fitment rules
- search result ranking

### Infrastructure
Responsible for:
- PDF text extraction
- local database access
- local file access
- AI client integration

## Key services

### `IPdfIngestionService`
Loads the PDF, extracts pages, identifies illustration sections, and emits raw chunks.

### `IManualParsingService`
Parses raw chunks into structured rows and metadata.

### `IPartsRepository`
Stores and queries normalized parts records and page mappings.

### `IHybridSearchService`
Combines:
- exact part number matching
- metadata filtering
- text search
- semantic ranking

### `IPromptBuilder`
Builds retrieval-grounded prompts for the AI layer.

### `IPartsAiService`
Calls the model and returns structured JSON.

### `IManualNavigationService`
Maps result rows to PDF page and illustration locations.

## Search pipeline

1. User enters a question or part number.
2. App collects active filters.
3. Retrieval layer queries local structured data.
4. Best candidate rows and page snippets are selected.
5. Prompt builder creates a grounded request.
6. AI returns structured JSON.
7. UI renders result cards and source actions.

## Ranking strategy

Use a weighted rank score:

- exact part number match: highest
- exact description phrase match: high
- model/year compatibility: high
- illustration/section relevance: medium
- semantic similarity: medium
- vague textual overlap: low

## Storage model

### Tables
- `PartRecords`
- `ManualPages`
- `IllustrationGroups`
- `VehicleTypes`
- `EngineTypes`
- `TransmissionTypes`
- `LegendEntries`
- `SearchHistory`
- `Favorites`

## AI boundary

The AI should only:
- interpret natural language
- choose among retrieved candidates
- summarize differences
- explain fitment constraints
- ask for missing disambiguating details

The AI should not:
- invent rows
- infer unsupported fitment
- answer without retrieval context

## Deployment notes

- Keep ingestion local for v1
- Keep manual data on-device when possible
- Cache prompt results per query + filter state
- Add telemetry around failed queries and clarification prompts
