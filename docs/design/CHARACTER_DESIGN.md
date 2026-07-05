# Character & Enemy Design (E04)

## Main Character (MVP)
- Element (Phase 2): multi-element unlock
- Weapon (Phase 2): sword placeholder
- Stats baseline: HP 100, ATK 15, DEF 5, Mana 100, Stamina 100
- Animations target: Idle, Walk, Run, Attack (E06 art pipeline)

## Companions (Design only — MVP)
- **Mira** — support/healer archetype
- **Kael** — scout/rogue archetype

## NPCs
### Story NPCs
- Guard Lian — authoritative, cautious
- High Priestess Mira — calm, secretive

### AI Demo NPC
- Mercenary Kael — witty, remembers player topics; uses AI/fallback dialogue

## Enemies
### Common Enemy — Void Slime
- HP: 50, ATK: 8, DEF: 2
- Behavior: chase → melee attack

### Boss — Void Guardian
- HP: 50 (scaled in demo), 2 phases
- Phase 2: increased damage, scale up
- Special attack every 6 seconds

## MVP Art Note
Use primitive/greybox meshes until E06 character model is ready. Replace via prefab swap without code changes.
