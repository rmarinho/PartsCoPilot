# PartsCopilot MAUI App Pack for Copilot CLI

This pack contains ready-to-paste planning and prompting files for building a cross-platform `.NET 10 MAUI` app that uses AI to help users find parts inside the uploaded Porsche 911/912 parts manual.

## Files

- `01-product-plan.md` — product goals, scope, MVP, roadmap
- `02-architecture.md` — technical architecture and data flow
- `03-copilot-master-prompt.md` — main prompt to give Copilot CLI
- `04-task-breakdown.md` — implementation tasks in execution order
- `05-ingestion-strategy.md` — how to parse and normalize the PDF manual
- `06-ui-interactions.md` — interactive UI guidance for MAUI
- `07-json-contracts.md` — suggested DTOs and JSON contracts
- `08-repo-structure.md` — proposed solution and project layout
- `09-copilot-followup-prompts.md` — smaller follow-up prompts for Copilot CLI

## Suggested usage with Copilot CLI

1. Start with `03-copilot-master-prompt.md`.
2. Then use `04-task-breakdown.md` to ask Copilot CLI to implement one vertical slice at a time.
3. Use `09-copilot-followup-prompts.md` for focused generation of individual services, screens, and models.

## Product summary

The source manual contains structured part tables with columns like `Pos`, `Part Number`, `Description`, `Remark`, `Qty`, and `Model`, plus illustration numbers and summary pages for vehicle, engine, and transmission applicability. That makes it suitable for a retrieval-grounded parts assistant rather than a generic chatbot.
