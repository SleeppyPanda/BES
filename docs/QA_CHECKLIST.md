# QA Checklist (E12)

## Gameplay (US-061)
- [ ] Walk, sprint (stamina drain/regen), jump
- [ ] Third-person camera rotation
- [ ] No fall-through floor on demo map

## Combat (US-062)
- [ ] Basic attack combo hits enemy
- [ ] Skill 1/2 consume mana and respect cooldown
- [ ] Dodge grants i-frames (Left Ctrl)
- [ ] Boss phase transition triggers

## AI NPC (US-063)
- [ ] Interact with NPC in range (E / Interact)
- [ ] Fallback dialogue returns in-character response
- [ ] Memory stores last player message
- [ ] Affinity increases after chat

## Quest (US-064)
- [ ] Main quest starts on gameplay load
- [ ] Branch choice updates `CurrentBranch`
- [ ] Completing quest grants item reward
- [ ] Ending id set on final main quest step

## Save/Load (US-065)
- [ ] Save writes JSON to persistentDataPath
- [ ] Continue restores inventory, quests, position

## Regression (US-068)
- Re-run all checks after each bug fix sprint
