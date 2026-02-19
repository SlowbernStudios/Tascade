# Contributing

Thanks for contributing to Tascade.

## Development Setup

1. Install .NET 9 SDK.
2. Clone the repo.
3. Build:
   ```bash
   dotnet build Tascade/Tascade.csproj
   ```
4. Run:
   ```bash
   dotnet run --project Tascade/Tascade.csproj
   ```

## Branch And PR Workflow

1. Create a branch from `main`.
2. Keep commits focused and descriptive.
3. Open a pull request using the PR template.
4. Ensure build passes before requesting review.

## Coding Guidelines

- Use clear names and keep methods focused.
- Keep changes minimal and scoped to the feature/fix.
- Avoid unrelated refactors in the same PR.
- Update docs when behavior changes.

## Commit Messages

Prefer conventional-style summaries, for example:

- `feat: add tab rename persistence`
- `fix: prevent task checkbox double-toggle`
- `docs: update release instructions`

## Issue Triage Labels

This repo uses:

- `good first issue`
- `help wanted`
- `bug`
- `enhancement`
- `discussion`
- `security`

