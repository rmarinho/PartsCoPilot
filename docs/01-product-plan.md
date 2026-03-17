# Product Plan — PartsCopilot

## Goal
Build an interactive `.NET 10 MAUI` application that runs on Windows, Android, iOS, and macOS and helps users find vehicle parts inside a PDF parts manual using a combination of structured search and AI-assisted retrieval.

## Product vision
The app should feel like a smart parts catalog assistant, not just a chat app. Users should be able to:

- search by natural language
- search by exact part number
- filter by model, year, variant, engine, and region
- inspect matching part rows
- jump directly to the relevant manual page and illustration
- compare similar parts
- ask follow-up questions about fitment or applicability

## Why this is a strong use case
The manual is structured into illustration groups and part tables, with applicability metadata across models and years. That allows a hybrid design:

- deterministic search for exact matches
- metadata filtering for fitment
- AI only for intent interpretation, disambiguation, summarization, and explanation

## Core principles

1. The model is not the database.
2. The app must always show source-backed results.
3. Exact part number hits outrank semantic matches.
4. Interactivity matters as much as raw search quality.
5. Every AI answer should be traceable to manual pages and illustration groups.

## Target users

- classic Porsche owners
- restoration shops
- mechanics
- collectors
- parts resellers
- enthusiasts using scanned manuals

## MVP scope

### In scope
- import and parse the uploaded PDF manual
- extract page-level text and illustration metadata
- normalize part rows into local storage
- support exact and fuzzy search
- support AI question answering over retrieved rows
- show results as cards with part number, description, quantity, model, page, and illustration
- open the source PDF page from a result
- allow model/year filters

### Out of scope for v1
- OCR-heavy workflows for bad scans
- camera-based photo recognition
- cloud sync
- multi-manual library
- marketplace integration
- user accounts

## Success criteria

- A user can ask: `Find oil thermostat for 1969 911 E`
- The app returns top matches with source page and illustration
- The app can explain why one candidate fits better than another
- The user can tap the result and open the corresponding manual page

## Product roadmap

### Phase 1 — Foundation
- MAUI app shell
- local data storage
- PDF ingestion pipeline
- manual viewer

### Phase 2 — Searchable catalog
- structured rows in SQLite
- exact search
- description search
- metadata filters

### Phase 3 — AI assistant
- retrieval pipeline
- grounded prompt generation
- structured JSON response
- result cards

### Phase 4 — Better interaction
- compare parts
- saved searches
- favorites
- follow-up chat in current page context

### Phase 5 — Advanced features
- multiple manuals
- cloud-backed indexing
- vision-assisted part identification
- replacement and supersession knowledge
