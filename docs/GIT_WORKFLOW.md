# Git Workflow (US-002)

## Branches
- `main` — stable, demo/release builds only
- `dev` — integration branch for active sprint work
- `feature/<ticket-or-topic>` — individual user story or task branches

## Flow
1. Branch from `dev`: `git checkout -b feature/us-035-character-controller`
2. Commit with message format: `US-035: add character controller movement`
3. Open PR into `dev`
4. After QA on `dev`, merge `dev` → `main` at sprint end

## Rules
- Never force-push `main`
- Keep commits focused on one user story when possible
- Do not commit `Library/`, `Temp/`, `UserSettings/`, or local API keys

## Recommended First Setup
```bash
git checkout -b dev
git push -u origin dev
```
