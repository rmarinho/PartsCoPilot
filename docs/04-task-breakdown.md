# Task Breakdown for Copilot CLI

## Task 1 — Create solution skeleton
Create a solution with projects:
- `PartsCopilot.App`
- `PartsCopilot.Core`
- `PartsCopilot.Infrastructure`
- `PartsCopilot.Tests`

Use dependency injection and register services in `MauiProgram`.

## Task 2 — Create domain models
Add:
- `PartRecord`
- `ManualPage`
- `IllustrationGroup`
- `VehicleContext`
- `SearchQuery`
- `SearchCandidate`
- `AiAnswer`
- `AiMatch`
- `LegendEntry`

## Task 3 — Create service contracts
Add interfaces:
- `IPdfIngestionService`
- `IManualParsingService`
- `IPartsRepository`
- `IHybridSearchService`
- `IPromptBuilder`
- `IPartsAiService`
- `IManualNavigationService`

## Task 4 — Create SQLite persistence
Implement SQLite initialization and repositories for:
- part records
- manual pages
- recent searches
- favorites

## Task 5 — Create ingestion pipeline
Implement a PDF ingestion pipeline that:
- reads page text
- detects illustration headers
- captures page numbers
- extracts tabular row text
- saves raw chunks and normalized rows

## Task 6 — Create ranking logic
Implement deterministic ranking that prefers:
1. exact part number
2. exact description phrase
3. model/year compatibility
4. semantic overlap

## Task 7 — Create prompt builder
Implement a prompt builder that accepts:
- user question
- vehicle context
- top candidate rows
- page snippets

Return a grounded prompt for the model.

## Task 8 — Create AI service abstraction
Implement a service that:
- sends prompt to model API
- requests JSON output
- parses the JSON to `AiAnswer`
- handles invalid model responses safely

## Task 9 — Build search UI
Create:
- search entry
- filter chips or pickers
- results list
- loading state
- no-results state
- error state

## Task 10 — Build result cards
Each result card should display:
- part number
- description
- quantity
- model
- page
- illustration
- confidence
- action buttons: open page, compare, favorite

## Task 11 — Build manual viewer scaffold
Create a page that:
- loads the source PDF
- jumps to selected page
- shows current page metadata
- supports future annotation/highlight hooks

## Task 12 — Add compare flow
Implement selection and comparison of 2 to 3 candidate matches.

## Task 13 — Add favorites and search history
Persist searches and favorite part records locally.

## Task 14 — Add unit tests
Add tests for:
- part ranking
- query normalization
- prompt building
- model response parsing

## Task 15 — Add sample dev data
Seed a small in-memory or local dataset to allow UI development before full ingestion is complete.
