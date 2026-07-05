# UI Layout Reference (1920×1080)

Canvas Scaler: **1920×1080**, Match **0** (width).

## Gameplay HUD (`Main play.png`)

| Region | Anchor | Size | Position |
|--------|--------|------|----------|
| Mini-map | Top-left | 200×200 | (24, -24) |
| Quest tracker | Top-left | 360×120 | (24, -240) |
| HUD bars (HP/Stamina/Mana) | Bottom-center | 520×80 | (0, 36) |
| Interact prompt | Bottom-center | 480×48 | (0, 96) |
| Party strip | Right-center | 220×420 | (-24, 0) |
| Skill bar | Bottom-center | 320×160 | (0, 120) |
| Top nav icons (8) | Top-right | 500×64 | (-24, -24) |

Nav icons: Inventory, Character, Map, **Weapon**, Wish, Team, Event, Artifacts.

HUD slice assignment: `HUDSpriteManifest` — **BES → Setup Project**.

## Full-screen overlays

Character, Weapon, Artifacts, Team, Event, Wish: stretch full canvas with mockup background + atoms (slots, markers, day calendar).

## Main Menu (`Start.png`)

| Hit area | Anchor | Size | Position |
|----------|--------|------|----------|
| Click to begin | Bottom-center | 560×72 | (0, 500) |
| Server picker | Bottom-center | 220×52 | (0, 320) |
| Continue (if save) | Bottom-center | 220×36 | (0, 380) |
| Event | Bottom-right | 56×56 | inset (48, 128) |
| Quit | Bottom-left | 56×56 | inset (48, 48) |
| Profile | Bottom-right | 56×56 | inset (48, 208) |
| Settings | Bottom-right | 56×56 | inset (48, 48) |

Overlays: **ServerPicker**, **Settings**, **PlayerProfile**, **Event** (shared EventUI).

## Hotkeys

| Action | Key |
|--------|-----|
| Inventory | Tab / I |
| Character | C |
| World Map | M |
| Weapon | F2 |
| Wish | F3 |
| Team | F4 |
| Event | F5 |
| Artifacts | F6 |
| Close menu | Esc |
| Interact | F |

Set Game view to **1920×1080** scale 1x for accurate preview.
