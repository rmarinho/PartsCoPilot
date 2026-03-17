# Deckard — History

## Core Context

- **Project:** A .NET MAUI AI-assisted parts catalog app for classic Porsche 911/912 manuals
- **Role:** Lead
- **Joined:** 2026-03-17T15:54:43.743Z

## Learnings

<!-- Append learnings below -->

### 2025-07-18: Initial Codebase Assessment

- **Codebase maturity:** ~85% real implementation, not scaffolding. Core pipeline (ingest → parse → store → search → UI) is fully functional.
- **Architecture:** Single-project with folder-based separation. Decided to KEEP this structure — restructuring to multi-project has cost with no user benefit at current scale.
- **Task status:** 4/15 done, 6/15 partial, 5/15 not started. Major gaps: AI layer (prompt builder + AI service), secondary UI screens (manual viewer, compare, favorites view), and proper unit tests.
- **Key decision:** Rachael (AI) is on the critical path — prompt builder and AI service gate the app's differentiator. Roy (Backend) and Pris (UI) can work in parallel from Week 1.
- **Risk noted:** No Semantic Kernel dependency yet, no xUnit project, no PDF rendering strategy for ManualViewerPage. MainPage.xaml is dead code.
- **Assessment written to:** `.squad/decisions/inbox/deckard-project-assessment.md`
