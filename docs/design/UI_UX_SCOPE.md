# UI/UX Scope — BES 2.0 Integrated

Reference mockups: `Downloads/BES 2.0` → `Assets/_Project/Art/UI/`

## Screen status (Shell / Composite / Complete)

| Screen | Mockup | Access | Functional | Visual |
|--------|--------|--------|------------|--------|
| Main Menu | Start.png | — | Complete | Composite |
| Loading | Loading . . . ..png | Auto | Complete | Composite |
| Gameplay HUD | HUD slices + Main play ref | Always | Complete | Composite |
| Mini-map | HUD/Group* frame | — | Complete | Composite |
| Quest Tracker | Mission.png | — | Complete | Composite |
| Interact Prompt | Interaction.png | F | Complete | Composite |
| Dialogue | Interaction.png + portrait | F @ NPC | Complete | Composite |
| Inventory | Character Profile Overall | Tab / I | Complete | Composite |
| Character Profile | Character Profile Overall | C | Complete | Composite |
| Weapon Details | Weapon.png | F2 / HUD | Complete | Composite |
| Weapon Enhance / Rank / Refine | Weapon/*.png | Flow | Complete | Composite |
| World Map | Event sence.png | M | Complete | Composite |
| Team Setup | Team Set up.png | F4 | Complete | Composite |
| Event / Check-in | Event Check in.png | F5 / Main Menu | Complete | Composite |
| Wish / Gacha | Wish.png | F3 | Complete | Composite |
| Artifacts | Artifacts.png | F6 | Complete | Composite |
| Player Profile | Username PLayer.png | Main Menu | Complete | Composite |
| Settings | Overlay | Main Menu | Complete | Shell |
| Server Picker | Overlay | Main Menu server btn | Complete | Shell |

**Legend:** Shell = logic + placeholder widgets; Composite = mockup BG + positioned atoms; Complete = full gameplay wiring (future: pixel-perfect Figma parity).

## HUD composite pipeline

- **Manifest:** `Assets/_Project/Data/UI/HUDSpriteManifest.asset` (20 slots incl. navWeapon)
- **Runtime load:** `Resources/Data/HUDSpriteManifest`
- **Setup:** **BES → Setup Project** (import, map, rebuild prefabs, scenes)
- **Runtime fallback:** `GameplayHudLayout.Reapply()` on Play

## Architecture

- Prefabs: `Assets/_Project/UI/Prefabs/Screens/`
- Atoms: `Assets/_Project/UI/Prefabs/Atoms/` (DaySlot, TeamSlot, MapMarker, WeaponSlot, …)
- Sprites: `Assets/_Project/Art/UI/`
- Navigation: `UINavigationController` (layers 0–3)
- Canvas: `UICanvasFit` — 1920×1080, Shrink mode

## Phase 3 (future)

- Live gacha server, real wallet sync
- 3D character preview RenderTexture polish
- Party swap gameplay integration
- Skill bar cooldown / binding gameplay
