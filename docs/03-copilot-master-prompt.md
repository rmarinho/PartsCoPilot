# Copilot CLI Master Prompt

Use this prompt in Copilot CLI as the main implementation brief.

---

Create a production-style `.NET 10 MAUI` application named `PartsCopilot` that runs on Windows, Android, iOS, and macOS.

## Product goal
Build an interactive AI-assisted parts finder for a classic Porsche parts manual PDF. The app must help the user search for parts using natural language and exact part numbers, filter by vehicle context, and open the relevant manual page and illustration.

## Source manual assumptions
The manual is structured with:
- illustration groups such as `Illustration: 101-05`
- part tables with columns like `Pos`, `Part Number`, `Description`, `Remark`, `Qty`, and `Model`
- summary pages for vehicle, engine, and transmission applicability
- fitment notes and legends

Design the app so it treats the PDF as a searchable source of structured catalog data, not as one giant text blob.

## Architecture requirements
Use:
- `.NET 10`
- `.NET MAUI`
- `CommunityToolkit.Mvvm`
- MVVM pattern
- dependency injection
- `SQLite` for local persistence
- a PDF text extraction service abstraction
- `Semantic Kernel` for AI orchestration
- a model client abstraction using OpenAI Responses API or equivalent

## Functional requirements
Implement these capabilities:

1. Search by exact part number
2. Search by natural language
3. Filter by model, year, variant, engine, and region
4. Show structured results as cards
5. Open the PDF page associated with a result
6. Show illustration number and page number in results
7. Support follow-up AI questions grounded in retrieved context
8. Support compare view for multiple matches
9. Save recent searches and favorites

## Non-functional requirements
- Clean architecture and separation of concerns
- Testable services and repositories
- No hard-coded model logic in UI
- Strong DTOs and typed results
- Async APIs throughout
- Cancellation token support where appropriate
- Logging hooks for ingestion and AI requests

## Data model guidance
Create models similar to:
- `PartRecord`
- `ManualPage`
- `IllustrationGroup`
- `VehicleContext`
- `SearchQuery`
- `SearchCandidate`
- `AiAnswer`
- `AiMatch`
- `LegendEntry`

## Search pipeline
Implement a hybrid search flow:

1. Accept user query and filters.
2. Query local structured data first.
3. Rank candidate rows.
4. Build a retrieval-grounded AI prompt from top candidates.
5. Ask the model for structured JSON.
6. Render the result in the UI.

Exact part number matches must outrank semantic matches.

## Prompting requirements
Create a system prompt that instructs the model to:
- never invent part numbers or fitment
- only answer from retrieved context
- prefer exact matches
- return structured JSON
- ask for clarification when the query is ambiguous

## JSON response contract
Use a contract like:

```json
{
  "answer": "string",
  "matches": [
    {
      "partNumber": "string",
      "description": "string",
      "position": "string",
      "illustration": "string",
      "page": 0,
      "qty": "string",
      "model": "string",
      "remark": "string",
      "fitment": "string",
      "confidence": 0.0
    }
  ],
  "needsClarification": false,
  "clarificationQuestion": "string"
}
```

## UX requirements
Provide these screens:

- Home/Search screen
- AI search chat screen
- PDF manual viewer screen
- Part details screen
- Compare parts screen
- Favorites/history screen

The UX should feel interactive and tool-like. It should not look like a plain chatbot.

## Requested deliverables
Generate:

1. Solution and project structure
2. Core models
3. Interfaces for services
4. Initial repositories
5. Sample ingestion pipeline
6. Prompt builder
7. AI service abstraction
8. Search ViewModel
9. Search page XAML
10. Result card UI
11. Manual viewer page scaffold
12. Sample seeded data for development
13. Unit test project with tests for ranking and prompt building

## Coding style
- Use file-scoped namespaces
- Use `record` where appropriate
- Use nullable reference types
- Prefer constructor injection
- Keep methods small and cohesive
- Add XML comments only for public contracts or non-obvious logic

## Output format
Generate code in logical steps. Start with the solution structure, domain models, interfaces, and application flow skeleton. Then add the UI and infrastructure pieces.

---
