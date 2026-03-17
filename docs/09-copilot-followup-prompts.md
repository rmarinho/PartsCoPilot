# Follow-up Prompts for Copilot CLI

## Prompt 1 — Generate domain models and interfaces
Create the domain models and service interfaces for a `.NET 10 MAUI` app named `PartsCopilot`. Use records where appropriate, nullable reference types, and clean architecture boundaries. Include models for `PartRecord`, `VehicleContext`, `SearchQuery`, `SearchCandidate`, `AiAnswer`, `AiMatch`, `ManualPage`, and `IllustrationGroup`. Also generate interfaces for ingestion, search, prompt building, AI orchestration, and manual navigation.

## Prompt 2 — Generate SQLite repository layer
Generate the SQLite infrastructure for `PartsCopilot`, including database initialization and repositories for `PartRecord`, recent searches, and favorites. Keep the repository contracts in the core project and implementations in infrastructure. Use async methods and cancellation tokens where practical.

## Prompt 3 — Generate PDF ingestion services
Generate a PDF ingestion pipeline for `PartsCopilot` that extracts page text, detects illustration markers like `Illustration: 101-05`, classifies pages, and parses part rows into normalized `PartRecord` entries. Add logging and return parsing diagnostics.

## Prompt 4 — Generate search and ranking services
Generate a hybrid search service for `PartsCopilot` that prefers exact part number matches, then exact phrase matches, then model/year compatibility, and finally semantic overlap. Keep ranking deterministic and testable.

## Prompt 5 — Generate prompt builder and AI service
Generate a retrieval-grounded prompt builder and an AI orchestration service for `PartsCopilot`. The AI must only answer from retrieved candidates and must return strongly typed JSON matching the `AiAnswer` contract.

## Prompt 6 — Generate Search page UI
Generate the `SearchPage.xaml` and `SearchViewModel` for `PartsCopilot`. The page should contain a search bar, filter controls, a results list, loading states, error states, and result card actions for open page, compare, and favorite.

## Prompt 7 — Generate manual viewer scaffold
Generate a `ManualViewerPage` and `ManualViewerViewModel` scaffold for `PartsCopilot`. It should support page navigation, show current page metadata, and receive a selected result to navigate to the correct page.

## Prompt 8 — Generate tests
Generate unit tests for ranking, prompt construction, and AI response parsing for `PartsCopilot`. Keep tests concise and meaningful.
