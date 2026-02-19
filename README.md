# Tascade

Tascade is a cross-platform desktop notes app with integrated task tracking, markdown editing, and an autosave-first workflow.

## Features

- Notes + tasks split layout
- Multiple notepad tabs
- Rename current tab inline
- Markdown editor with `Editor`, `Preview`, and `Split` modes
- Auto-switch to `Split` when markdown syntax is detected
- Task list: add, complete, delete, clear completed, and filter
- `Enter` to add a task
- Open / Save / Save As
- Export to TXT / Markdown / HTML
- Bundled JetBrains Mono font (no system install required)

## Download

Release assets are published on GitHub Releases:

- <https://github.com/SlowbernStudios/Tascade/releases>

## Build And Run

Prerequisite:

- .NET 9 SDK

Run locally:

```bash
dotnet run --project Tascade/Tascade.csproj
```

Build:

```bash
dotnet build Tascade/Tascade.csproj
```

## Release Packaging

Local packaging script:

```powershell
.\release.ps1 -Version v1.0.0
```

RIDs packaged by default:

- `win-x64`, `win-arm64`
- `linux-x64`, `linux-arm64`
- `osx-x64`, `osx-arm64`

## Signed Windows Releases

The project supports optional Authenticode signing for Windows publish output.

Local signed packaging:

```powershell
.\release.ps1 `
  -Version v1.0.0 `
  -EnableSigning `
  -CodeSignPfxPath "C:\path\codesign.pfx" `
  -CodeSignPfxPassword "your-password"
```

GitHub Actions release signing is supported via secrets:

- `CODESIGN_PFX_BASE64`
- `CODESIGN_PFX_PASSWORD`

## Automated GitHub Releases

Workflow: `.github/workflows/release.yml`

Trigger:

- Push a tag matching `v*`

Example:

```bash
git tag v1.0.0
git push origin v1.0.0
```

## Data Storage

- `%APPDATA%/Tascade/settings.json`
- `%APPDATA%/Tascade/recent_files.json`

## Contributing And Policies

- Contributing guide: `CONTRIBUTING.md`
- Code of Conduct: `CODE_OF_CONDUCT.md`
- Security policy: `SECURITY.md`

## License

MIT. See `LICENSE`.

