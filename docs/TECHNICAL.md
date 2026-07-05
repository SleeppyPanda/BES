# Technical Documentation (US-070)

## Stack
- Unity 6000.3 LTS (Unity 6)
- URP 17.x
- Input System 1.19
- TextMeshPro 3.2

## Architecture
```
Core (GameManager, SceneLoader, GameEvents, Bootstrapper)
  ├── Gameplay (Movement, Combat, Inventory, Save, World)
  ├── Narrative (Dialogue, Quest, AI, Memory, Relationship)
  └── UI (HUD, Menu, Dialogue, Inventory, Character Profile)
```

## Key Entry Points
- `Bootstrapper` — auto-creates `[BES] GameSystems` at runtime
- `BES/Setup Project` menu — creates scenes, folders, build settings
- `MainMenu` scene — New Game / Continue
- `Gameplay` scene — full MVP demo

## Save File
- Path: `Application.persistentDataPath/bes_save.json`
- Format: JSON via `JsonUtility` with serializable pair lists

## AI Dialogue
- MVP uses `AIDialogueService.GenerateFallbackResponse`
- Optional: set API key on component for future LLM integration (US-047)

## Controls (Keyboard)
| Action | Key |
|--------|-----|
| Move | WASD |
| Jump | Space |
| Sprint | Left Shift |
| Attack | Left Mouse |
| Skill 1 | Q |
| Skill 2 | E |
| Dodge | Left Ctrl |
| Interact | F (Hold) |
| Inventory | Tab / I |
| Character Menu | C |
| World Map | M |

## Phase 2 (not in MVP)
Element system, party swap, gacha, multiplayer, weapon enhance/refine UI
