# Art Production Pipeline (E06)

## MVP Deliverables
- [ ] Concept art — protagonist + Creation City mood board
- [ ] Greybox hero — capsule replaced by rigged model when ready
- [ ] Environment blockout — 3 regions in Gameplay scene
- [ ] Animation set — Idle, Walk, Run, Attack (minimum)
- [ ] UI icons — HP/MP/stamina, item rarity frames

## Folder Structure
- `Assets/_Project/Art/Characters/`
- `Assets/_Project/Art/Environment/`
- `Assets/_Project/Art/UI/`

## Integration
Replace primitive meshes in scene with prefabs without changing gameplay scripts.
Tag player prefab as `Player` and assign InputActionAsset on `PlayerInputReader`.

## Tools
Blender for modeling/rigging; export FBX to Unity URP.
