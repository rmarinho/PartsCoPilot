# Decision: MAUI Type Shims for ViewModel/Converter Testing

**Author:** Roy (Backend Dev)
**Date:** 2026-03-18
**Status:** Implemented
**PR:** #34

## Context

The test project targets plain `net11.0` (no MAUI workload) and uses source file linking (`<Compile Include="../../...">`) to compile production code. ViewModels and Converters reference MAUI-specific types (`IValueConverter`, `IQueryAttributable`, `Shell`, `Color`, `ImageSource`, `FilePicker`, `DevicePlatform`) that don't exist in the test project's target framework.

## Decision

Created `MauiShims.cs` with minimal stub implementations of MAUI types in their correct namespaces (`Microsoft.Maui.Controls`, `Microsoft.Maui.Graphics`, `Microsoft.Maui.Storage`, `Microsoft.Maui.Devices`). Added `GlobalUsings.cs` with `global using` directives so linked source files resolve the types automatically.

## Alternatives Considered

1. **Reference the MAUI project directly** — Not possible; MAUI projects can't be referenced by plain `net11.0` test projects
2. **Use MAUI test runner** — Would require MAUI workload installation and device/simulator; too heavyweight for unit tests
3. **Extract ViewModels into a separate class library** — Major refactoring of the project structure

## Consequences

- ViewModel and Converter tests run fast (~500ms for 283 tests) without any MAUI infrastructure
- Shims must be updated if ViewModels start using new MAUI types (e.g., adding `Navigation`, `DisplayAlert`)
- Shim implementations are intentionally minimal (empty methods, no-op behavior) — they exist only to satisfy the compiler
- The approach scales well; adding new ViewModels just requires a `<Compile Include>` entry

## Related

- AppModels.cs had duplicate type definitions (LegendEntry, VehicleType, EngineType, TransmissionType) that also existed in ManualModels.cs with different property names. Removed duplicates from AppModels.cs, keeping canonical definitions in ManualModels.cs.
