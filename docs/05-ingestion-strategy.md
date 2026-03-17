# PDF Ingestion Strategy

## Goal
Transform the parts manual PDF into structured records that can be searched deterministically and used as grounded context for AI answers.

## Important rule
Do not store only full-page text. Extract structured part rows and metadata.

## Expected manual patterns
The manual appears to contain:
- page markers
- illustration identifiers like `Illustration: 101-05`
- tabular rows with fields such as `Pos`, `Part Number`, `Description`, `Remark`, `Qty`, and `Model`
- summary pages for engines, vehicles, and transmissions
- legends and applicability notes

## Extraction stages

### Stage 1 — Page extraction
For each PDF page, capture:
- source file id or path
- page number
- raw text
- whether the page contains an image or illustration marker
- detected section title
- detected illustration number

### Stage 2 — Page classification
Classify each page as one of:
- summary page
- legend page
- illustration page
- part table page
- mixed page

### Stage 3 — Row detection
From part table pages, detect rows and normalize fields into:
- `Position`
- `PartNumber`
- `Description`
- `Remark`
- `Quantity`
- `Model`
- `Page`
- `Illustration`
- `Section`

### Stage 4 — Applicability extraction
Parse and store separate records for:
- model applicability
- engine applicability
- transmission applicability
- chassis or engine range notes
- country or market notes

### Stage 5 — Linking
Create lookup links between:
- part rows and manual pages
- part rows and illustration groups
- summary metadata and part rows when possible

## Normalization rules

### Part numbers
Normalize for search while preserving display format.
Examples:
- raw: `901 107 751 00`
- normalized: `90110775100`

### Text
Store both:
- original display text
- normalized search text in lowercase without excessive punctuation

### Dates and ranges
Preserve exact text for fitment ranges such as:
- `from engine no.`
- `up to chassis no.`
- `-68`
- `69`

## Suggested raw DTOs

```csharp
public sealed record RawPdfPage(
    int PageNumber,
    string RawText,
    string? Illustration,
    string? Section,
    string PageType);

public sealed record ParsedPartRow(
    string Position,
    string PartNumber,
    string Description,
    string? Remark,
    string? Quantity,
    string? Model,
    int Page,
    string? Illustration,
    string? Section);
```

## Recommended approach
Start simple:
- page text extraction
- regex and line-based parsing
- manual heuristics for illustration markers and headers
- development seed data for fast UI progress

Only add embeddings later if deterministic extraction is not enough.

## Validation checks
During ingestion, log:
- pages processed
- pages with illustration detected
- pages classified as part tables
- parsed rows count
- rows missing part number
- rows missing description

## MVP ingestion definition of done
- can parse manual pages into a local database
- can retrieve rows by exact part number
- can retrieve rows by description keywords
- can map row to page and illustration
