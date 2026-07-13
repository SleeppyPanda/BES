# Beneath Enchanted Sky (BES)

Unity action RPG vertical slice. This README is the project tracking entry point: scene flow, module status, data assets, Unity editing rules, and current implementation notes.

Last updated: 2026-07-10

## Project Snapshot

| Item | Value |
|---|---|
| Engine | Unity 6 / 6000.3.x |
| UI reference resolution | 1920x1080 |
| Main runtime path | `MainMenu -> Loading -> Gameplay` |
| Main project root | `Assets/_Project` |
| Runtime data root | `Assets/_Project/Resources/Data` |
| Runtime assembly | `Assets/_Project/Scripts/BES.asmdef` |
| Editor assembly | `Assets/_Project/Scripts/Editor/BES.Editor.asmdef` |

## Workflow Rules

- Edit UI prefab and scene layout directly in Unity.
- Do not use `BESUIPrefabBuilder` to rebuild existing UI. It can overwrite manually assigned prefab references.
- New UI images should be assigned in Unity through serialized fields, manifests, or scene/prefab objects.
- Runtime code may populate dynamic lists such as quest cards, rewards, gacha result cards, chat messages, and map markers.
- Gameplay data should prefer ScriptableObject assets in `Resources/Data` so it can be tuned from Unity.

## Repository Layout

```text
Assets/_Project/
|-- Art/                  UI/art source assets
|-- Data/                 editor/source data copies
|-- Resources/Data/       runtime-loadable ScriptableObject data
|-- Scenes/               MainMenu, Loading, Gameplay, PrototypeScene
|-- Scripts/
|   |-- AI/               enemy AI
|   |-- Core/             bootstrap, scene loading, events, GameManager
|   |-- Editor/           setup/import tools only
|   |-- Gameplay/         player, combat, world, save, inventory
|   |-- Narrative/        dialogue, quest, NPC memory, relationship
|   `-- UI/               HUD, panels, manifests, UI controllers
|-- Tests/                unit/playmode test area
`-- UI/                   prefabs and UI assets
```

## Scene Flow

| Scene | Purpose | Main scripts |
|---|---|---|
| `MainMenu` | account/dev mode/menu entry | `MainMenuController`, `SceneLoader` |
| `Loading` | async load transition and progress UI | `LoadingScreenUI`, `SceneLoader` |
| `Gameplay` | playable open-world slice | `GameplaySceneBootstrap`, HUD/UI systems |
| `PrototypeScene` | sandbox/prototype area | optional |

Expected transition:

```text
MainMenu
  -> fade out
  -> Loading
  -> load full Gameplay scene
  -> fade in / Gameplay
```

## Core Systems

| System | Status | Main files |
|---|---|---|
| Bootstrap/GameManager | active | `Core/Bootstrapper.cs`, `Core/GameManager.cs` |
| Scene loading | active | `Core/SceneLoader.cs`, `Core/SceneNames.cs` |
| Event bus | active | `Core/GameEvents.cs` |
| Save/load | active | `Gameplay/Save/SaveSystem.cs`, `GameAutoSave.cs`, `SaveData.cs` |
| Input fallback | active | `Gameplay/PlayerInputReader.cs` |
| Player movement | active | `Gameplay/PlayerMotor.cs`, `StaminaSystem.cs`, `ThirdPersonCamera.cs` |
| Party swap | active | `UI/Data/PartyRoster.cs`, `Gameplay/PartySwapController.cs` |
| Visual test switch | active | `Gameplay/PartyCharacterVisualSwitcher.cs` |

## Gameplay Combat

Current combat input is separated into four move slots per character.

| Input | Character data field | Runtime handler |
|---|---|---|
| Left Mouse | `leftClickAttackId` | `BasicAttackController` |
| Right Mouse | `rightClickAttackId` | `BasicAttackController` |
| Q | `skill1Id` | `SkillController` |
| E | `skill2Id` | `SkillController` |

Main files:

- `Assets/_Project/Scripts/UI/Data/CharacterDatabase.cs`
- `Assets/_Project/Resources/Data/CharacterDatabase.asset`
- `Assets/_Project/Scripts/Gameplay/Combat/CharacterCombatProfile.cs`
- `Assets/_Project/Scripts/Gameplay/Combat/BasicAttackController.cs`
- `Assets/_Project/Scripts/Gameplay/Combat/SkillController.cs`
- `Assets/_Project/Scripts/Gameplay/Combat/CombatVfx.cs`
- `Assets/_Project/Scripts/Gameplay/Combat/EnemyHealth.cs`
- `Assets/_Project/Scripts/Gameplay/Combat/EnemyDamageFeedback.cs`

Current character attack mapping:

| Character | Left Mouse | Right Mouse | Q | E |
|---|---|---|---|---|
| `hero_01` | `attack_void_edge_left` | `attack_void_burst_right` | `skill_void_slash` | `skill_guard_break` |
| `hero_02` | `attack_flare_cuts_left` | `attack_flare_lunge_right` | `skill_quick_cut` | `skill_flare_dash` |
| `hero_03` | `attack_guard_sweep_left` | `attack_earth_slam_right` | `skill_shield_wave` | `skill_ground_lock` |
| `hero_04` | `attack_arc_shot_left` | `attack_marked_burst_right` | `skill_arc_bolt` | `skill_focus_shot` |
| `char_limited_01` | `attack_star_edge_left` | `attack_lunar_cleave_right` | `skill_starfall` | `skill_lunar_drive` |
| `hero_05` | `attack_spark_jab_left` | `attack_rookie_blast_right` | `skill_spark_step` | `skill_comet_burst` |

Skill icon setup:

- Open `Assets/_Project/Resources/Data/CharacterDatabase.asset`.
- For each character, assign `Skill1 Icon` for Q and `Skill2 Icon` for E.
- `SkillBarDriver` loads active character icons when the party changes.

## Gameplay HUD

| Area | Status | Main files |
|---|---|---|
| Health/Stamina bars | active, uses `Image.fillAmount` | `UI/HUDController.cs` |
| Minimap | active shell + map marker logic | `UI/MiniMapUI.cs` |
| Top-left buttons | active | `UI/HudCornerButtonsUI.cs` |
| HUD nav bar | event, battlepass, wish, bag, personal | `UI/HudNavBarUI.cs` |
| Party strip | active, click and keys 1-4 | `UI/PartyStripUI.cs` |
| Skill buttons | active, Q/E cooldown radial | `UI/SkillBarUI.cs`, `UI/SkillBarDriver.cs` |
| Chat shell | active local UI | `UI/ChatBoxUI.cs` |
| Quest tracker | active one tracked quest | `UI/QuestTrackerUI.cs` |
| Quest panel | active list/detail/rewards | `UI/QuestLogUI.cs`, `UI/QuestCardUI.cs`, `UI/QuestRewardItemUI.cs` |

Notes:

- Mana bar and level text are removed/hidden from gameplay HUD.
- Health/stamina fill objects keep their Unity-set RectTransform size. Runtime changes only `Image.fillAmount`.
- Corner-positioned UI must use correct anchors and pivots in Canvas to stay stable at 1920x1080.

## Main UI Panels

| Panel | Status | Main files |
|---|---|---|
| Inventory/Bag | active shell/data | `UI/InventoryUI.cs`, `Gameplay/Inventory/*` |
| Wish/Gacha | active x1/x10/local pity | `UI/WishUI.cs`, `UI/GachaCardUI.cs` |
| Team | active shell | `UI/TeamSetupUI.cs` |
| Weapon | active shell/data | `UI/WeaponScreenUI.cs`, `UI/EquipmentUI.cs` |
| Event | active shell/check-in | `UI/EventUI.cs` |
| Map | active map/teleport shell | `UI/GameMapUI.cs`, `Gameplay/World/Teleport*` |
| Settings | placeholder/shell | UI prefab/scene |
| Battle Pass | placeholder/shell | `UI/BattlePassUI.cs` |
| Personal/Profile | placeholder/shell | `UI/PlayerProfileUI.cs` |

## Quest And Narrative

| System | Status | Main files |
|---|---|---|
| Quest definitions | active | `Narrative/QuestDefinition.cs`, `QuestDatabase.cs` |
| Quest runtime | active | `Narrative/QuestManager.cs` |
| Quest objective auto-check | active | `Narrative/QuestObjectiveTracker.cs` |
| Quest HUD tracker | active | `UI/QuestTrackerUI.cs` |
| Quest panel | active | `UI/QuestLogUI.cs` |
| Dialogue graph | active | `Narrative/DialogueNode.cs`, `DialogueSystem.cs` |
| NPC interaction | active | `Narrative/NPCInteractable.cs`, `QuestMarker.cs` |
| NPC memory/relationship | active | `NPCMemoryStore.cs`, `RelationshipSystem.cs` |

Quest panel behavior:

- Active quests are displayed in order: Story Quest, Commission Quest, World Quest.
- All quest cards use one reusable card prefab.
- Completed quests are removed from active list and lower cards push upward through layout rebuild.
- `Navigate` tracks the selected quest and updates the gameplay tracker.

Runtime test quests are currently registered in `QuestManager` for panel testing.

## World Systems

| System | Status | Main files |
|---|---|---|
| Regions | active | `Gameplay/World/WorldRegion.cs` |
| Teleport | active | `TeleportPoint.cs`, `TeleportService.cs` |
| World map | active shell | `UI/GameMapUI.cs` |
| Collectibles | active | `Gameplay/World/Collectible.cs` |
| Enemy spawn region | active data-driven component | `Gameplay/World/EnemySpawnRegion.cs` |
| World integration | active | `Gameplay/World/WorldIntegrationManager.cs` |

Enemy spawn rule:

- Spawn regions should be configured in Unity.
- Do not rely on UI/prefab builders to generate gameplay objects.
- Region size, enemy prefab, count range, and spawn behavior should remain inspector-adjustable.

## Runtime Data Assets

| Asset | Path | Purpose |
|---|---|---|
| CharacterDatabase | `Resources/Data/CharacterDatabase.asset` | characters, stats, attack ids, skill ids, skill icons |
| ItemDatabase | `Resources/Data/ItemDatabase.asset` | items, rewards, inventory |
| QuestDatabase | `Resources/Data/QuestDatabase.asset` | quest assets |
| WeaponDatabase | `Resources/Data/WeaponDatabase.asset` | weapons/equipment |
| ArtifactDatabase | `Resources/Data/ArtifactDatabase.asset` | artifacts |
| DefaultGachaBanner | `Resources/Data/DefaultGachaBanner.asset` | gacha pool |
| DefaultEvent | `Resources/Data/DefaultEvent.asset` | event/check-in |
| HUDSpriteManifest | `Resources/Data/HUDSpriteManifest.asset` | HUD icons/sprites |
| UIScreenBackgroundManifest | `Resources/Data/UIScreenBackgroundManifest.asset` | panel backgrounds |
| CharacterPortraitManifest | `Resources/Data/CharacterPortraitManifest.asset` | portrait sprites |

## Controls

| Action | Input |
|---|---|
| Move | WASD / Arrow keys |
| Look | Mouse |
| Jump | Space |
| Sprint | Left Shift |
| Left attack | Left Mouse |
| Right attack | Right Mouse |
| Skill Q | Q |
| Skill E | E |
| Dodge | Left Ctrl / C fallback |
| Interact | F / E fallback |
| Party swap | 1, 2, 3, 4 |
| Inventory | I |
| Character | C |
| Map | M |
| Wish | G / UI button |
| Quest panel | V / mission button |
| Close panel | Esc |

## Save Data

Save is JSON-based through `SaveSystem`.

Expected saved groups:

- player position, health, mana, stamina
- current region, discovered regions, unlocked teleports
- active/completed quests and quest step progress
- inventory items
- party slots, active character, unlocked characters, party health
- wallet/gacha/event/meta progression
- NPC memory and relationship data

## Editor Tools

| Tool | Status | Rule |
|---|---|---|
| `BESProjectSetup` | allowed for initial project/data setup | use carefully |
| `BESUIAssetImporter` | allowed for importing/mapping art assets | use carefully |
| `BESLoadingSceneBuilder` | allowed for loading scene setup if needed | use carefully |
| `BESUIPrefabBuilder` | disabled/deprecated for existing UI | do not rebuild assigned prefabs |
| `BESUIHudMapper` | helper only | do not overwrite manual bindings unless intentional |

## Current Implementation Tracker

| Area | Status | Notes |
|---|---|---|
| Main menu | in progress | click-to-begin, account/dev mode, panels |
| Loading scene | active | logo, shadow, progress bar/text, fade |
| Gameplay HUD | active | manual prefab editing required |
| Combat four-action model | active | left/right/Q/E separated per character |
| Enemy AI test damage | active | test enemy can damage player |
| Enemy hit feedback | active | material flash/VFX pulse |
| Party health swap | active | each slot tracks current/max HP |
| Quest panel | active | grouped active list + rewards |
| Wish panel | active | x1/x10, card hover, animation |
| Save on quit | active | via save/autosave systems |
| World map teleport | active shell | teleport points need scene setup |
| Region enemy spawn | active component | configure regions in Unity |

## Known Gaps / Next Work

| Gap | Impact |
|---|---|
| Production character models/animations missing | current combat is functional test VFX, not final animation |
| Settings/Event/BattlePass/Personal content incomplete | panels are shells/placeholders |
| World map art and teleport point polish needed | UI logic exists, visual/data needs Unity setup |
| Quest content is test-heavy | replace runtime test quests with real content assets later |
| Network/server features not implemented | chat/gacha/save are local MVP behavior |
| Need Unity Editor compile validation | `dotnet build BES.slnx` cannot build until Unity regenerates `.csproj` files |

## Verification Notes

Common check after code changes:

```text
git diff --check
```

Unity-side checks:

- Open `CharacterDatabase.asset` and verify all attack/skill ids show in Inspector.
- Enter Gameplay and test Left Mouse, Right Mouse, Q, E for each party slot.
- Swap characters with 1-4 and verify skill icons/cooldowns change.
- Open Quest panel, complete/remove a quest, verify cards push upward.
- Check health/stamina fill size after taking damage or spending stamina.

## Documentation

Additional docs live under `docs/` when present:

- `docs/BUILD.md`
- `docs/CONVENTIONS.md`
- `docs/TECHNICAL.md`
- `docs/QA_CHECKLIST.md`
- `docs/design/*`
