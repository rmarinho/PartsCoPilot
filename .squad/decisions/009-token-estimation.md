# Decision: Token Estimation and Context Window Management

**Date:** 2026-03-17  
**Author:** Rachael (AI Dev)  
**Status:** Implemented (PR #30)

## Context

PromptBuilder had no awareness of token limits. If search returns 100+ candidates with page snippets, prompts could exceed model context windows (128K for GPT-4o, 16K for GPT-4o-mini), causing silent truncation or API errors.

## Decision

Implement token counting and context window management with these components:

### 1. TokenEstimator
- **Approach:** Character-based heuristic (~4 chars/token)
- **Rationale:** Avoided tiktoken .NET dependency (external library adds complexity). Heuristic is simple, fast, and provides conservative upper bound.
- **Implementation:** `Math.Ceiling(text.Length / 4.0)` ensures we never underestimate.

### 2. ContextBudget
- **Configuration:** Separate limits for candidates (4000 tokens) and snippets (1000 tokens)
- **Rationale:** Candidates are more valuable (structured data) than snippets (context). Different budgets allow tuning.
- **Location:** Moved from Models to Services namespace to eliminate duplicate definitions.

### 3. ContextTrimmer
- **Strategy:** Sort by relevance score descending, preserve high-relevance, drop low-relevance
- **Rationale:** User questions target specific parts. High-relevance results most likely to answer query. Dropping low-relevance minimizes impact on answer quality.
- **Description truncation:** Cap at 200 chars with '...' suffix for long descriptions
- **Logging:** Warning logged when candidates/snippets dropped (helps debugging)

### 4. PromptBuilder Integration
- **Backward compatibility:** Two constructors — with trimmer (new) and without (legacy)
- **Rationale:** Existing code continues to work unchanged. New code opts into trimming.

## Alternatives Considered

1. **tiktoken .NET library** — Rejected: External dependency, adds complexity, overkill for our needs
2. **Fixed candidate count limit** — Rejected: Doesn't account for variable-length descriptions
3. **Token budget per candidate** — Rejected: More complex, less flexible than global budget

## Testing

- 7 TokenEstimatorTests: Null/empty input, edge cases, realistic text
- 10 ContextTrimmerTests: Empty input, budget enforcement, relevance ordering, truncation
- 8 PromptBuilderContextWindowTests: Integration with trimmer, backward compatibility

## Impact

- Prevents context window overruns and API errors
- Prioritizes high-relevance results — improves answer quality under budget constraints
- Configurable budgets allow tuning per model (GPT-4o vs GPT-4o-mini)
- Backward compatible — no breaking changes

## Future Work

- Model-specific budgets could be moved to appsettings.json for runtime configuration
- Consider tiktoken if heuristic proves inaccurate (monitor via logging)
- PartsAiService implementation pending (mentioned in original issue but not yet implemented)
