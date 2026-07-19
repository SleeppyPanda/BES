# BES Island Generated Scene Manifest

Target scene:
- `Assets/Scenes/BES_Island_GameReady.unity`

Generation entry point:
- Unity menu: `BES > Generate Complete Island Scene`
- Batch method: `BESIslandSceneGenerator.GenerateScene`

Sketch-to-scene layout:
- Global: 512m x 512m terrain frame with an irregular/asymmetric coastline while preserving the approved gameplay anchors.
- Center: circular social plaza, large fountain, shallow fountain lake ring, stone pavement, benches, flower beds, player spawn, emote zone, welcome NPC.
- North: dense green forest, natural path, deer/rabbit/bird spawner markers, bird audio placeholder, flying bird system, resource gathering trigger zones.
- West: cherry blossom forest, procedural sakura trees, Japanese torii gate, sakura VFX prefab, photo spot and sitting zone.
- South: swimming pool, stone deck, lounge chairs, umbrellas, sitting triggers, recreation NPC.
- East: fishing harbor, wooden docks, the only fishing boat/boat interaction on the map, fisherman NPC, fishing minigame zones.
- Northeast: raised cliff viewpoint, circular wooden platform, gazebo, photo/cutscene trigger, photographer guide NPC.
- Southeast: separated lower camping island, two wooden bridges, tents, campfire, market table and food props, merchant/fisherman/photographer NPCs, no boat prop.

Technical coverage:
- Unity Terrain system with generated heightmap and terrain layers.
- Asset-directed redesign pass using existing imported packs: Ghibli-style arch, street lamps, stone slabs, signs, wagon, altar, big tree, flora/resource prefabs, water VFX, fire VFX, pool beach ball, pirate cargo/cannon/pallet props, tents, campfire, fountain, benches, food props, and stylized vegetation.
- Beauty-render redesign pass hides debug labels/interactions in preview, uses a closer isometric camera, prevents visible ocean-plane borders, and increases warm stylized lighting contrast.
- Visual polish pass with real terrain textures, curved stone slab paths, terrain grass details, modular paver rings, planters, banners, lanterns, pool coping, dock posts, viewpoint railings, clustered vegetation, cliff rock walls, and denser coastline foam strips.
- Clean hierarchy groups for `Terrain`, `Environment`, `NPCs`, `Animals`, `Interactions_And_Spawn`, `VFX`, and bake/optimization markers.
- URP-compatible materials generated under `Assets/Environment/Generated`.
- URP Global Volume generated as `PostFX_GlobalVolume_Bloom_Color_Vignette` using bloom, color adjustments, and vignette for a brighter stylized fantasy presentation.
- Object placement CSV is exported to `Assets/Environment/Generated/BES_PlacedObjects.csv`.
- QA checklist is exported to `Assets/Environment/Generated/BES_QA_Checklist.md`.
- Day/night support via `BESDayNightCycle`.
- Interaction markers via `BESInteractionZone`.
- NPC/animal placeholders via `BESNpcMarker`.
- NavMesh-ready marker object attempts to add `NavMeshSurface` through reflection when the AI Navigation package is present.
- LOD and occlusion placeholders are included for later bake workflow.

Notes:
- Several animals and NPCs are intentional placeholders so final character models can be swapped without changing gameplay locations.
- Prefab paths use existing imported asset packs where available and fallback to primitives if a referenced prefab is missing.
