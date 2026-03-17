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

### 2026-03-17: Week 1 Work Complete — Status Reassessment

- **Major progress:** All AI layer implementations complete (PartsAiService, PromptBuilder, ManualNavigationService live). Backend complete (seed data, models, repository extensions). UI shell complete (HomePage, SearchPage, PartDetailsPage, FavoritesPage, all 4 ViewModels). Test framework in place (xUnit project exists with 3 test suites).
- **Current maturity:** ~90% of codebase scaffolding done; core search/AI/storage pipeline fully functional.
- **TOP 3 NEXT PRIORITIES:**
  1. **Manual Viewer UI Implementation** (Owner: Pris) — PartDetailsPage → View button must render actual PDF page. Currently placeholder. Unblocks: User can now see source material from results.
  2. **Test Coverage Expansion** (Owner: Roy) — Existing test classes are empty framework. Need: PartsRepository CRUD tests, parser edge cases, AI service response validation. Unblocks: CI confidence, regression safety.
  3. **Compare Parts Flow** (Owner: Pris) — Secondary feature, but needed for UX polish. Render two PartDetails side-by-side or in modal. Unblocks: App's secondary differentiator (manual cross-reference).
- **Risk mitigation:** ManualViewerPage needs PDF rendering library decision (PdfSharp? SkiaSharp? Platform-specific?). API key security move to settings page. No retry/timeout policies on AI calls yet.

### 2025-07-18: Full Backlog Decomposition — 25 Issues Created

- **Action:** Deep codebase analysis and GitHub issue creation for all remaining production-readiness work.
- **Issues created:** 25 issues across 5 priority levels (P0×2, P1×8, P2×10, P3×5)
- **Critical findings (P0):**
  1. Prompt injection vulnerability in PromptBuilder — user input not escaped (#1)
  2. N+1 query in PartsRepository.SearchPartsAsync — loads all parts into memory (#2)
- **Key architecture gaps identified:**
  - No database migration system — schema changes break existing DBs (#4)
  - No caching layer — every query hits SQLite fresh (#20)
  - Parser has quantity extraction bug (regex group concatenation) (#12)
  - AI context window unbounded — could exceed model limits (#9)
- **UI/UX gaps:** Dark mode incomplete (#8), accessibility minimal (#5), hardcoded strings (#7), default icons (#18), no skeleton loading (#17)
- **Missing infrastructure:** No README.md (#11), No Settings page (#6), no input validation (#21), no search pagination (#13)
- **Assignment distribution:** Roy: 14 issues, Pris: 14 issues, Rachael: 5 issues, Deckard: 3 issues (some shared)
- **Overall assessment:** Core pipeline works well. App is ~90% scaffolded but needs hardening, polish, and production tooling before shipping.
- **Decision written to:** `.squad/decisions.md` (merged 2026-03-17T18:13:09Z)
