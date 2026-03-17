# Squad Decisions

## Active Decisions

### 2026-03-17T16:22:00Z: User directive — Premium model for all agent spawns
**By:** Rui Marinho (via Copilot)  
**What:** Use claude-opus-4.6 for all agent spawns — no cost optimization, premium model everywhere.  
**Why:** User request — captured for team memory.

### 2026-03-17T16:21:00Z: Architecture decision — Keep single-project structure
**By:** Deckard (Lead)  
**What:** Keep the current flat single-project layout. Do NOT restructure to multi-project.  
**Why:**
1. ~25 source files would all need to move and have namespaces rewritten
2. Current folder structure (`Models/`, `Services/`, `Data/`, `ViewModels/`, `Views/`) already provides clean separation of concerns
3. MVVM pattern, DI, repository pattern, and interface-based services are all properly implemented
4. Restructuring risk: introduces bugs in a working pipeline for zero user-facing benefit
5. This app is a single-target product, not a shared library ecosystem — multi-project adds overhead without payoff at this scale

**When to revisit:** If we add a second app target (e.g., a CLI tool or Blazor web version), extract `Core` and `Infrastructure` projects at that point.

---

## Project Status: Week 1 Priorities

**Deckard Assessment Complete (2026-03-17T16:21:00Z)**

| Task | Status | Owner | Unblocks |
|------|--------|-------|----------|
| IPromptBuilder implementation | Pending | Rachael | AI service |
| IPartsAiService implementation | Pending | Rachael | AI search UI |
| Semantic Kernel NuGet integration | Pending | Rachael | AI service |
| LegendEntry model + parsing | Pending | Roy | Domain model completion |
| Seed data mechanism | Pending | Roy | Pris's UI dev |
| xUnit test project setup | Pending | Roy | CI/CD confidence |
| Result card action buttons | Pending | Pris | Core UX |
| Favorites/History UI screen | Pending | Pris | User retention |

**Codebase Maturity:** ~60% complete  
**Core Working:** PDF ingestion → parsing → SQLite storage → hybrid search → search UI  
**Gaps:** AI layer, manual viewer, compare flow, secondary UI screens

---

## Governance

- All meaningful changes require team consensus
- Document architectural decisions here
- Keep history focused on work, decisions focused on direction
