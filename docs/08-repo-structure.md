# Proposed Repository Structure

```text
PartsCopilot/
  PartsCopilot.sln
  src/
    PartsCopilot.App/
      App.xaml
      AppShell.xaml
      MauiProgram.cs
      Views/
        HomePage.xaml
        SearchPage.xaml
        ManualViewerPage.xaml
        PartDetailsPage.xaml
        ComparePage.xaml
      ViewModels/
        HomeViewModel.cs
        SearchViewModel.cs
        ManualViewerViewModel.cs
        PartDetailsViewModel.cs
        CompareViewModel.cs
      Resources/
      Controls/
        PartResultCard.xaml
      Converters/
    PartsCopilot.Core/
      Models/
      Interfaces/
      Services/
      Ranking/
      Prompts/
    PartsCopilot.Infrastructure/
      Data/
      Repositories/
      Pdf/
      Ai/
      Search/
      Logging/
    PartsCopilot.Tests/
      Ranking/
      Prompting/
      Parsing/
      Search/
  docs/
    architecture.md
    prompting.md
    ingestion.md
```

## Project responsibilities

### `PartsCopilot.App`
UI, navigation, view models, state management.

### `PartsCopilot.Core`
Domain models, interfaces, ranking rules, prompt contracts.

### `PartsCopilot.Infrastructure`
SQLite, PDF extraction, AI integration, repositories, search implementations.

### `PartsCopilot.Tests`
Unit tests and parser tests.
