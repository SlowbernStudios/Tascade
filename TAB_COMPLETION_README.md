# Tab Completion Notes (Current State)

This document tracks the current auto-complete implementation status in Tascade v1.0.

## Status

- Core auto-complete services and control exist in the codebase.
- The primary markdown editor currently uses a standard `TextBox`, not `AutoCompleteTextBox`.
- In short: completion infrastructure is present, but not yet fully wired into the default editing surface.

## Implemented Components

### `AutoCompleteService`

Location: `Tascade/Services/AutoCompleteService.cs`

Current behavior:

- Suggestion types:
  - Word
  - Command (`:w`, `:q`, `:wq`, `:q!`, `:help`)
  - File path
  - Snippet (`todo`, `fixme`, `date`)
- Triggering:
  - Trigger characters (default `:`, `/`, `\`, `.`)
  - Minimum word-length checks
- Configurable through `AutoCompleteSettings` in `Tascade/Models/AppSettings.cs`

### `AutoCompleteTextBox`

Location:

- `Tascade/Controls/AutoCompleteTextBox.axaml`
- `Tascade/Controls/AutoCompleteTextBox.axaml.cs`

Current behavior:

- Popup suggestion list
- Keyboard navigation (up/down/enter/tab/escape)
- Manual triggers (`Ctrl+Space`, `Ctrl+N`, `Ctrl+P`)
- Connects to `VimModeService` completion events

### `VimModeService` Completion Hooks

Location: `Tascade/Services/VimModeService.cs`

Current completion hooks:

- Normal mode: `Tab`, `Ctrl+N`, `Ctrl+P`
- Insert mode: `Ctrl+Tab`, `Ctrl+N`, `Ctrl+P`
- Command mode: `Tab` completes command candidates

## Configuration Model

`AutoCompleteSettings` in `Tascade/Models/AppSettings.cs` provides:

- enable/disable global completion
- per-provider toggles
- maximum suggestions
- trigger character list
- minimum word length
- case sensitivity

## Next Integration Step

To make completion active in daily editing, replace the main editor `TextBox` usage in the markdown editing path with `AutoCompleteTextBox` and bind:

- `Text`
- `AutoCompleteService`
- `VimModeService`

## Scope Note

This file is intentionally implementation-focused and should be updated when completion becomes fully active in the primary editor flow.

