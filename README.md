# Tascade

Tascade is a cross-platform desktop notes app with built-in task tracking, a terminal-inspired UI, markdown editing, and autosave-first workflow.

## v1.0 Highlights

- Notes + tasks split-pane layout
- Multiple tabs/pages
- Rename current tab inline
- Markdown editor with Editor / Preview / Split modes
- Auto-switch to Split mode when markdown syntax is detected
- Task list with add, complete, delete, clear-completed, and filtering
- Enter-to-add task shortcut
- Autosave-backed local JSON storage
- Open / Save / Save As + export to TXT / Markdown / HTML
- JetBrains Mono bundled in app assets (no system font install required)

## Downloads

Release artifacts are published on GitHub Releases:

- <https://github.com/SlowbernStudios/Tascade/releases>

## Build And Run

### Prerequisites

- .NET 9 SDK

### Local Run

```bash
dotnet run --project Tascade/Tascade.csproj
```

### Local Release Packaging

Use the included script:

```powershell
.\release.ps1 -Version v1.0.0
```

This creates archives under `artifacts/<version>/` for:

- `win-x64`, `win-arm64`
- `linux-x64`, `linux-arm64`
- `osx-x64`, `osx-arm64`

## Automated GitHub Releases

This repo includes `.github/workflows/release.yml`.

Behavior:

- Trigger: push a tag matching `v*`
- Builds all supported RIDs
- Uploads platform archives to the GitHub Release

Example:

```bash
git tag v1.0.0
git push origin v1.0.0
```

## Data Storage

Settings and data are stored in JSON:

- Windows: `%APPDATA%/Tascade/settings.json`

Recent files are stored at:

- `%APPDATA%/Tascade/recent_files.json`

## Notes On Current Implementation

- Several menu commands in Edit/View are present but currently placeholders (for example Undo/Redo/Cut/Copy/Paste/Find/Replace/Print).
- Task display is already sorted in the filtered view (incomplete first, then creation time), so no separate Sort action is shown in the UI.

## Project Layout

```text
.
├── Tascade/
│   ├── Assets/
│   ├── Controls/
│   ├── Converters/
│   ├── Models/
│   ├── Services/
│   ├── ViewModels/
│   ├── Views/
│   ├── App.axaml
│   ├── Program.cs
│   └── Tascade.csproj
├── .github/workflows/release.yml
└── release.ps1
```

## License

MIT. See `LICENSE`.

## Changelog

### v1.0.0 (2026-02-18)

- First public release of Tascade
- Rebrand from TaskTango to Tascade
- Bundled JetBrains Mono font and custom app icons
- Cross-platform release automation (local script + GitHub Actions)
