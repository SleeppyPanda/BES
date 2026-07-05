# BES Coding Conventions (US-005)

## C# Style
- Use PascalCase for public types, methods, and properties.
- Use camelCase for private fields with `[SerializeField]` allowed for inspector exposure.
- One public type per file; file name matches type name.
- Prefer explicit namespaces: `BES.Core`, `BES.Gameplay`, `BES.Narrative`, `BES.UI`, `BES.AI`.

## Unity Conventions
- Project content lives under `Assets/_Project/`.
- Scenes: `Assets/_Project/Scenes/`
- ScriptableObject data: `Assets/_Project/Data/`
- Prefabs: `Assets/_Project/Prefabs/`
- Do not modify Unity template folders unless necessary.

## Naming
- GameObjects: `PascalCase` with role prefix (`Player`, `NPC_Guard`, `Boss_VoidGuardian`).
- Tags: `Player`, `Enemy`.
- Layers: `Enemy` for hostile units.
- ScriptableObject ids: snake_case (`main_awakening`, `herb_common`).

## Architecture Rules
- Cross-system communication uses `GameEvents` (Event Bus).
- Persistent state goes through `GameManager` systems and `SaveSystem`.
- Avoid singletons except documented entry points (`GameManager`, `SceneLoader`, `DialogueSystem`, `CombatManager`).

## Git
- See [GIT_WORKFLOW.md](GIT_WORKFLOW.md).
