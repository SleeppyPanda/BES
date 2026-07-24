# BES Island QA Checklist

## Layout
- [ ] Map frame is 512m x 512m.
- [ ] Central plaza, north forest, west cherry grove, south pool, east fishing harbor, NE viewpoint and SE camping island match the approved sketch.
- [ ] Camping island is separate and connected by two small wooden bridges.
- [ ] Fountain appears only in the central plaza.
- [ ] Boats appear only in the east fishing harbor.
- [ ] Cherry trees appear only in the west cherry grove.
- [ ] Animals appear only in the north forest.

## Lighting And Rendering
- [ ] Bake lightmaps after final prefab swaps.
- [ ] Confirm `PostFX_GlobalVolume_Bloom_Color_Vignette` is active in URP.
- [ ] Confirm beauty preview camera shows the island as an isometric 3D diorama, not a flat debug map.
- [ ] Confirm labels, interaction cubes, spawner cubes and debug marker objects are hidden in `BES_Island_MapPreview.png`.
- [ ] Confirm no square ocean plane border is visible in the preview.
- [ ] Check bloom, fog and water transparency on mobile quality settings.

## Gameplay Readiness
- [ ] Bake NavMesh and verify all main stone paths are walkable.
- [ ] Confirm main stone paths are curved and visually lead between all 7 zones.
- [ ] Confirm `Player_Spawn_Center_Plaza` is centered and unobstructed.
- [ ] Test NPC interaction zones, fishing points, photo spots and sitting spots.
- [ ] Verify animal spawner markers are inside the north forest only.

## Optimization
- [ ] Swap placeholder primitive animals/NPCs with final LOD prefabs when available.
- [ ] Add colliders only to walkable/blocking props; keep small decorative props light.
- [ ] Mark static environment props for batching, occlusion and lightmap baking.
- [ ] Validate mobile draw calls after prefab replacement.
