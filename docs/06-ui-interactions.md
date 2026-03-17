# UI and Interaction Plan

## UX goal
The app must feel like an interactive workshop tool, not just a chat surface.

## Main screens

### 1. Home/Search
Elements:
- search bar
- quick action buttons
- recent searches
- favorites
- filter summary bar

### 2. Search Results / AI Assistant
Elements:
- question input
- active vehicle context chips
- result cards
- clarification prompt area
- compare mode toggle

### 3. Manual Viewer
Elements:
- current page number
- illustration number
- zoom controls
- next/previous navigation
- selected result metadata banner

### 4. Part Details
Elements:
- part number header
- description
- applicability and remarks
- quantity
- source page and illustration
- actions: open page, favorite, compare

### 5. Compare
Elements:
- two or three selected parts
- differences in model, year, quantity, and remarks highlighted

## Interaction patterns

### Search
User can enter:
- exact part number
- descriptive search phrase
- fitment question

Examples:
- `90110775100`
- `oil thermostat`
- `find crankcase studs for 1969 912`

### Guided filters
Provide pickers or chips for:
- model
- year
- variant
- engine
- region

### Result cards
Each card should have strong visual hierarchy:
1. part number
2. description
3. fitment summary
4. page and illustration
5. actions

### Clarification flow
If multiple close matches exist, show:
- short explanation
- clarification question
- quick reply buttons such as `911`, `912`, `1968`, `1969`, `USA`, `Targa`

## UI implementation notes
- Use MVVM bindings only
- Keep pages responsive for desktop and mobile
- Use collection views for result lists
- Use shell routes for navigation
- Support keyboard and touch interactions

## Performance notes
- avoid blocking UI during PDF processing
- cache result cards and page metadata
- support cancellation on search

## Accessibility
- minimum tap targets
- readable contrast
- semantic labels for action buttons
- text scaling friendly layouts
