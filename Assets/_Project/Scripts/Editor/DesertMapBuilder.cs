#if UNITY_EDITOR
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Unity.AI.Navigation;
using BES.Gameplay;

namespace BES.EditorTools
{
    public static class DesertMapBuilder
    {
        const string PrefabsRoot = "Assets/DuNguyn/Egypt Props Pack/Prefabs";
        const string DesertScenePath = "Assets/Scenes/desert map.unity";
        const string IslandScenePath = "Assets/Scenes/BES_Island_GameReady.unity";
        const string BigPrefabPath = "Assets/_Project/Prefabs/Enemy_MeshyMonster.prefab";
        const string BabyPrefabPath = "Assets/_Project/Prefabs/Enemy_BabyMonster.prefab";

        [MenuItem("BES/Gameplay/Rebuild Full Desert Map (Image 1)")]
        public static void Build()
        {
            BuildFullMap(true);
        }

        public static void BuildFullMap(bool logResult = true)
        {
            // 1. Open scene and clean up
            var scene = EditorSceneManager.OpenScene(DesertScenePath, OpenSceneMode.Single);
            foreach (var go in scene.GetRootGameObjects())
            {
                Object.DestroyImmediate(go);
            }

            // 2. Load Prefabs Dictionary
            var prefabs = LoadAllPrefabs();
            if (prefabs.Count == 0)
            {
                if (logResult) Debug.LogError("[BES Desert Builder] No prefabs found at " + PrefabsRoot);
                return;
            }

            // 3. Create Root Hierarchy Groups
            var mapRoot = new GameObject("00_Desert_Map_Assembled");
            SceneManager.MoveGameObjectToScene(mapRoot, scene);

            var envGroup = CreateGroup("01_Environment", mapRoot.transform);
            var groundGroup = CreateGroup("02_Ground_Pavement", mapRoot.transform);
            var buildingsGroup = CreateGroup("03_Buildings", mapRoot.transform);
            var pillarsGroup = CreateGroup("04_Pillars_And_Avenue", mapRoot.transform);
            var villageGroup = CreateGroup("05_Village_Houses", mapRoot.transform);
            var rocksGroup = CreateGroup("06_Border_Rocks", mapRoot.transform);
            var gameplayGroup = CreateGroup("07_Gameplay_Systems", mapRoot.transform);

            // 4. Environment & Lighting Setup
            if (prefabs.TryGetValue("SM_SkyBox", out var skyPrefab))
            {
                var sky = (GameObject)PrefabUtility.InstantiatePrefab(skyPrefab, envGroup);
                sky.transform.position = Vector3.zero;
                sky.transform.localScale = Vector3.one * 6.0f;
            }

            if (prefabs.TryGetValue("SM_Plane", out var planePrefab))
            {
                var plane = (GameObject)PrefabUtility.InstantiatePrefab(planePrefab, envGroup);
                plane.transform.position = new Vector3(0f, -2.5f, 0f);
                plane.transform.localScale = new Vector3(75f, 1f, 75f);
            }

            // Directional Sunlight (Warm Golden Desert)
            var lightGo = new GameObject("Directional Light");
            lightGo.transform.SetParent(envGroup);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1.0f, 0.94f, 0.82f);
            light.intensity = 1.35f;
            light.shadows = LightShadows.Soft;
            lightGo.transform.rotation = Quaternion.Euler(45f, -35f, 0f);

            // Main Isometric Gameplay Camera (Scaled up x2.5 for wide view)
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            camGo.transform.SetParent(envGroup);
            camGo.transform.position = new Vector3(-20f, 50f, -70f);
            camGo.transform.rotation = Quaternion.Euler(32f, 20f, 0f);
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.fieldOfView = 50f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 1000f;
            camGo.AddComponent<AudioListener>();

            // Tile size & variation lists
            float T = 5.0f; // Scaled up tile spacing (Image 1 x2.5)
            var floorTiles = GetPrefabList(prefabs, "SM_O_Nen_v2_");
            var pondTiles = GetPrefabList(prefabs, "SM_O_Nen_v1_");
            var raisedTiles = GetPrefabList(prefabs, "SM_O_Nen_v3_");
            var rocks = GetPrefabList(prefabs, "SM_Da_");

            // 5. Ground Platforms Construction (Exact layout matching Image 1, scaled x2.5)
            float outpostY = 0.5f;
            float pondY = -0.75f;
            
            // 5.1 Main Central Plaza (Z: -6..8, X: -10..10 at Y = 0m)
            for (int x = -10; x <= 10; x++)
            {
                for (int z = -6; z <= 8; z++)
                {
                    // Center pond area at (X: -2..2, Z: 0..4)
                    if (x >= -2 && x <= 2 && z >= 0 && z <= 4)
                    {
                        var pondP = PickRandom(pondTiles);
                        if (pondP != null)
                        {
                            var pt = (GameObject)PrefabUtility.InstantiatePrefab(pondP, groundGroup);
                            pt.transform.position = new Vector3(x * T, pondY, z * T);
                            pt.transform.localScale = Vector3.one * 2.5f;
                        }
                    }
                    else
                    {
                        var tileP = PickRandom(floorTiles);
                        if (tileP != null)
                        {
                            var ft = (GameObject)PrefabUtility.InstantiatePrefab(tileP, groundGroup);
                            ft.transform.position = new Vector3(x * T, 0f, z * T);
                            ft.transform.localScale = Vector3.one * 2.5f;
                        }
                    }
                }
            }

            // 5.2 Raised Pyramid Terrace (Center-North: X: -6..6, Z: 9..18 at Y = 3.0m)
            float pyrY = 3.0f;
            for (int x = -6; x <= 6; x++)
            {
                for (int z = 9; z <= 18; z++)
                {
                    var tileP = PickRandom(raisedTiles) ?? PickRandom(floorTiles);
                    if (tileP != null)
                    {
                        var rt = (GameObject)PrefabUtility.InstantiatePrefab(tileP, groundGroup);
                        rt.transform.position = new Vector3(x * T, pyrY, z * T);
                        rt.transform.localScale = Vector3.one * 2.5f;
                    }
                }
            }
            // Pyramid Front Stairs
            if (prefabs.TryGetValue("SM_NhaPhu_06", out var stairP))
            {
                PlaceProp(stairP, groundGroup, new Vector3(0f, 0f, 8.5f * T), Quaternion.Euler(0f, 0f, 0f), Vector3.one * 2.5f);
            }

            // 5.3 Raised West Sun Temple Terrace (X: -14..-8, Z: 6..14 at Y = 2.5m)
            float westY = 2.5f;
            for (int x = -14; x <= -8; x++)
            {
                for (int z = 6; z <= 14; z++)
                {
                    var tileP = PickRandom(raisedTiles) ?? PickRandom(floorTiles);
                    if (tileP != null)
                    {
                        var wt = (GameObject)PrefabUtility.InstantiatePrefab(tileP, groundGroup);
                        wt.transform.position = new Vector3(x * T, westY, z * T);
                        wt.transform.localScale = Vector3.one * 2.5f;
                    }
                }
            }
            if (prefabs.TryGetValue("SM_NhaPhu_06", out var stairW))
            {
                PlaceProp(stairW, groundGroup, new Vector3(-8f * T, 0f, 6.5f * T), Quaternion.Euler(0f, 90f, 0f), Vector3.one * 2.5f);
            }

            // 5.4 Raised East Scroll Temple Terrace (X: 8..14, Z: 5..13 at Y = 2.0m)
            float eastY = 2.0f;
            for (int x = 8; x <= 14; x++)
            {
                for (int z = 5; z <= 13; z++)
                {
                    var tileP = PickRandom(raisedTiles) ?? PickRandom(floorTiles);
                    if (tileP != null)
                    {
                        var et = (GameObject)PrefabUtility.InstantiatePrefab(tileP, groundGroup);
                        et.transform.position = new Vector3(x * T, eastY, z * T);
                        et.transform.localScale = Vector3.one * 2.5f;
                    }
                }
            }
            if (prefabs.TryGetValue("SM_NhaPhu_06", out var stairE))
            {
                PlaceProp(stairE, groundGroup, new Vector3(8f * T, 0f, 5.5f * T), Quaternion.Euler(0f, -90f, 0f), Vector3.one * 2.5f);
            }

            // 5.5 South Village Street Plaza (X: -8..8, Z: -12..-7 at Y = 0m)
            for (int x = -8; x <= 8; x++)
            {
                for (int z = -12; z <= -7; z++)
                {
                    var tileP = PickRandom(floorTiles);
                    if (tileP != null)
                    {
                        var st = (GameObject)PrefabUtility.InstantiatePrefab(tileP, groundGroup);
                        st.transform.position = new Vector3(x * T, 0f, z * T);
                        st.transform.localScale = Vector3.one * 2.5f;
                    }
                }
            }

            // 5.6 Far West Outpost Platform (X: -22..-15, Z: 2..8 at Y = 0.5m)
            for (int x = -22; x <= -15; x++)
            {
                for (int z = 2; z <= 8; z++)
                {
                    var tileP = PickRandom(floorTiles);
                    if (tileP != null)
                    {
                        var ot = (GameObject)PrefabUtility.InstantiatePrefab(tileP, groundGroup);
                        ot.transform.position = new Vector3(x * T, outpostY, z * T);
                        ot.transform.localScale = Vector3.one * 2.5f;
                    }
                }
            }
            if (prefabs.TryGetValue("SM_NhaPhu_06", out var stairOutpost))
            {
                PlaceProp(stairOutpost, groundGroup, new Vector3(-15f * T, 0f, 5f * T), Quaternion.Euler(0f, 90f, 0f), Vector3.one * 2.5f);
            }

            // 6. Major Buildings Placement (Exact Positions from Image 1, scaled x2.5)
            // 6.1 Grand Central Pyramid (Top-Center of Image 1)
            if (prefabs.TryGetValue("SM_Nha_01", out var pyramidP))
            {
                PlaceProp(pyramidP, buildingsGroup, new Vector3(0f, pyrY, 14f * T), Quaternion.Euler(0f, 0f, 0f), Vector3.one * 2.5f);
            }

            // 6.2 Sun Temple with Golden Sundial (Top-Left, Number 3 in Image 1)
            if (prefabs.TryGetValue("SM_Nha_02", out var sunTempleP))
            {
                PlaceProp(sunTempleP, buildingsGroup, new Vector3(-11f * T, westY, 10.5f * T), Quaternion.Euler(0f, 0f, 0f), Vector3.one * 2.5f);
            }

            // 6.3 Scroll Temple with White Banner (Top-Right, Number 2 in Image 1)
            if (prefabs.TryGetValue("SM_Nha_03", out var scrollTempleP))
            {
                PlaceProp(scrollTempleP, buildingsGroup, new Vector3(11f * T, eastY, 9.5f * T), Quaternion.Euler(0f, 0f, 0f), Vector3.one * 2.5f);
            }

            // 7. Colonnade & Pillars (Left of pond, Number 5 in Image 1)
            if (prefabs.TryGetValue("SM_Tru_01", out var pillarP))
            {
                // 4 Tall Columns forming the Gateway in Zone 5 of Image 1
                PlaceProp(pillarP, pillarsGroup, new Vector3(-4.5f * T, 0f, 1.5f * T), Quaternion.identity, Vector3.one * 2.5f);
                PlaceProp(pillarP, pillarsGroup, new Vector3(-4.5f * T, 0f, 4.0f * T), Quaternion.identity, Vector3.one * 2.5f);
                PlaceProp(pillarP, pillarsGroup, new Vector3(-7.0f * T, 0f, 1.5f * T), Quaternion.identity, Vector3.one * 2.5f);
                PlaceProp(pillarP, pillarsGroup, new Vector3(-7.0f * T, 0f, 4.0f * T), Quaternion.identity, Vector3.one * 2.5f);

                // Flanking Entrance Columns
                PlaceProp(pillarP, pillarsGroup, new Vector3(-2.5f * T, pyrY, 10.5f * T), Quaternion.identity, Vector3.one * 2.5f);
                PlaceProp(pillarP, pillarsGroup, new Vector3(2.5f * T, pyrY, 10.5f * T), Quaternion.identity, Vector3.one * 2.5f);
            }

            if (prefabs.TryGetValue("SM_Tru_02", out var obeliskP))
            {
                PlaceProp(obeliskP, pillarsGroup, new Vector3(-9.5f * T, westY, 7.5f * T), Quaternion.identity, Vector3.one * 2.5f);
                PlaceProp(obeliskP, pillarsGroup, new Vector3(9.5f * T, eastY, 6.5f * T), Quaternion.identity, Vector3.one * 2.5f);
            }

            if (prefabs.TryGetValue("SM_Tru_03", out var pedestalP))
            {
                PlaceProp(pedestalP, pillarsGroup, new Vector3(0f, pyrY, 10.5f * T), Quaternion.identity, Vector3.one * 2.5f);
            }

            // 8. Village Houses Placement (Foreground, Number 4 in Image 1)
            if (prefabs.TryGetValue("SM_NhaPhu_01", out var house1))
                PlaceProp(house1, villageGroup, new Vector3(-5.5f * T, 0f, -8.5f * T), Quaternion.Euler(0f, 0f, 0f), Vector3.one * 2.5f);

            if (prefabs.TryGetValue("SM_NhaPhu_02", out var house2))
                PlaceProp(house2, villageGroup, new Vector3(-1.5f * T, 0f, -8.0f * T), Quaternion.Euler(0f, 0f, 0f), Vector3.one * 2.5f);

            if (prefabs.TryGetValue("SM_NhaPhu_04", out var house4))
                PlaceProp(house4, villageGroup, new Vector3(2.5f * T, 0f, -8.0f * T), Quaternion.Euler(0f, 0f, 0f), Vector3.one * 2.5f);

            if (prefabs.TryGetValue("SM_NhaPhu_03", out var house3))
                PlaceProp(house3, villageGroup, new Vector3(6.5f * T, 0f, -8.5f * T), Quaternion.Euler(0f, 0f, 0f), Vector3.one * 2.5f);

            if (prefabs.TryGetValue("SM_NhaPhu_05", out var house5))
                PlaceProp(house5, villageGroup, new Vector3(0.5f * T, 0f, -11.0f * T), Quaternion.Euler(0f, 0f, 0f), Vector3.one * 2.5f);

            // Far West Outpost Houses
            if (prefabs.TryGetValue("SM_NhaPhu_01", out var houseW1))
                PlaceProp(houseW1, villageGroup, new Vector3(-19.5f * T, outpostY, 6.0f * T), Quaternion.Euler(0f, 0f, 0f), Vector3.one * 2.5f);

            if (prefabs.TryGetValue("SM_NhaPhu_02", out var houseW2))
                PlaceProp(houseW2, villageGroup, new Vector3(-17.0f * T, outpostY, 3.5f * T), Quaternion.Euler(0f, 0f, 0f), Vector3.one * 2.5f);

            // 9. Perimeter Border Rocks
            Vector3[] rockPositions = new Vector3[]
            {
                new Vector3(-8.5f * T, 0f, -12.5f * T),
                new Vector3(8.5f * T, 0f, -12.5f * T),
                new Vector3(-14.5f * T, westY, 14.5f * T),
                new Vector3(14.5f * T, eastY, 13.5f * T),
                new Vector3(-6.5f * T, pyrY, 18.5f * T),
                new Vector3(6.5f * T, pyrY, 18.5f * T),
                new Vector3(-22.5f * T, outpostY, 8.5f * T),
                new Vector3(-22.5f * T, outpostY, 1.5f * T),
                new Vector3(-10.5f * T, 0f, -5.5f * T),
                new Vector3(10.5f * T, 0f, -5.5f * T)
            };

            foreach (var rpos in rockPositions)
            {
                var rp = PickRandom(rocks);
                if (rp != null)
                {
                    PlaceProp(rp, rocksGroup, rpos, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), Vector3.one * 2.5f);
                }
            }

            // 10. Add Mesh Colliders to Ground & Props for NavMesh & Physics (Exclude environment skybox/plane)
            var targetGroups = new Transform[] { groundGroup, buildingsGroup, pillarsGroup, villageGroup, rocksGroup };
            foreach (var grp in targetGroups)
            {
                var renderers = grp.GetComponentsInChildren<MeshRenderer>(true);
                foreach (var mr in renderers)
                {
                    if (mr.GetComponent<Collider>() == null)
                    {
                        var mc = mr.gameObject.AddComponent<MeshCollider>();
                        mc.convex = false;
                    }
                }
            }

            // 10.5. Mark all meshes in targetGroups as static for NavMesh baking
            foreach (var grp in targetGroups)
            {
                var meshGos = grp.GetComponentsInChildren<Transform>(true);
                foreach (var t in meshGos)
                {
                    GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.NavigationStatic);
                }
            }

            // 11. Setup NavMeshSurface
            var navSurfaceGo = new GameObject("NavMeshSurface");
            navSurfaceGo.transform.SetParent(gameplayGroup);
            navSurfaceGo.transform.position = Vector3.zero;
            var surface = navSurfaceGo.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.All;
            surface.useGeometry = NavMeshCollectGeometry.RenderMeshes;
            surface.BuildNavMesh();
            if (logResult) Debug.Log("[BES Desert Builder] NavMesh successfully baked!");

            // 12. Setup Gameplay Bootstrap
            if (File.Exists(IslandScenePath))
            {
                var islandScene = EditorSceneManager.OpenScene(IslandScenePath, OpenSceneMode.Additive);
                var sourceBootstrap = GameObject.Find("GameplayBootstrap");
                if (sourceBootstrap != null)
                {
                    var copyBootstrap = Object.Instantiate(sourceBootstrap);
                    copyBootstrap.name = "GameplayBootstrap";
                    SceneManager.MoveGameObjectToScene(copyBootstrap, scene);
                    copyBootstrap.transform.SetParent(gameplayGroup);
                    copyBootstrap.transform.position = new Vector3(0f, 0.5f, -4.5f * T); // spawn player in center village street
                }
                EditorSceneManager.CloseScene(islandScene, true);
            }

            // 13. Setup Monster Spawn Regions
            var spawnRoot = new GameObject("EnemySpawnRegions_Desert");
            spawnRoot.transform.SetParent(gameplayGroup);
            spawnRoot.transform.position = Vector3.zero;

            var bigPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BigPrefabPath);
            var babyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BabyPrefabPath);

            // Region 1: Grand Pyramid (Guarded by 1 Big Monster + 2 Baby Monsters)
            // Centered on the open plaza right in front of the pyramid stairs
            Vector3 pyrPos = new Vector3(0f, 0.1f, 6.5f * T);
            CreateSpawnRegion(scene, spawnRoot.transform, "SpawnRegion_Pyramid_Big", "reg_pyr_big", "sub_pyr_big", pyrPos, 6f, 1, 1, bigPrefab, 25f);
            CreateSpawnRegion(scene, spawnRoot.transform, "SpawnRegion_Pyramid_Baby", "reg_pyr_baby", "sub_pyr_baby", pyrPos, 6f, 2, 2, babyPrefab, 25f);

            // Region 2: West Sun Altar (1 Big Monster + 2 Baby Monsters)
            // Centered on the open plaza area to the left of the pond
            Vector3 westPos = new Vector3(-6f * T, 0.1f, 2f * T);
            CreateSpawnRegion(scene, spawnRoot.transform, "SpawnRegion_WestAltar_Big", "reg_west_big", "sub_west_big", westPos, 6f, 1, 1, bigPrefab, 25f);
            CreateSpawnRegion(scene, spawnRoot.transform, "SpawnRegion_WestAltar_Baby", "reg_west_baby", "sub_west_baby", westPos, 6f, 2, 2, babyPrefab, 25f);

            // Region 3: South Village Street (3 Baby Monsters Patrolling)
            // Centered on the open village street
            Vector3 villagePos = new Vector3(0f, 0.1f, -6f * T);
            CreateSpawnRegion(scene, spawnRoot.transform, "SpawnRegion_Village_Baby", "reg_vil_baby", "sub_vil_baby", villagePos, 6f, 3, 3, babyPrefab, 25f);

            // 14. Save Assembled Scene
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            
            if (SceneView.lastActiveSceneView != null)
            {
                SceneView.lastActiveSceneView.LookAt(new Vector3(-4f, 6f, 4f), Quaternion.Euler(30f, 25f, 0f), 55f);
            }

            if (logResult) Debug.Log("[BES Desert Builder] Complete Desert Island Map successfully constructed, styled, and saved to " + DesertScenePath);
        }

        static void PlaceProp(GameObject prefab, Transform parent, Vector3 localPos, Quaternion localRot, Vector3 localScale = default)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot;
            if (localScale != default)
            {
                go.transform.localScale = localScale;
            }
        }

        static Transform CreateGroup(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        static Dictionary<string, GameObject> LoadAllPrefabs()
        {
            var dict = new Dictionary<string, GameObject>();
            if (!Directory.Exists(PrefabsRoot)) return dict;

            var files = Directory.GetFiles(PrefabsRoot, "*.prefab", SearchOption.AllDirectories);
            foreach (var f in files)
            {
                string norm = f.Replace('\\', '/');
                string name = Path.GetFileNameWithoutExtension(norm);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(norm);
                if (prefab != null)
                {
                    dict[name] = prefab;
                }
            }
            return dict;
        }

        static List<GameObject> GetPrefabList(Dictionary<string, GameObject> dict, string prefix)
        {
            var list = new List<GameObject>();
            foreach (var kvp in dict)
            {
                if (kvp.Key.StartsWith(prefix))
                {
                    list.Add(kvp.Value);
                }
            }
            return list;
        }

        static GameObject PickRandom(List<GameObject> list)
        {
            if (list == null || list.Count == 0) return null;
            return list[Random.Range(0, list.Count)];
        }

        static void CreateSpawnRegion(Scene scene, Transform parent, string name, string regionId, string subRegionId,
            Vector3 spawnCenter, float size, int min, int max, GameObject prefab, float patrolRadius)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = spawnCenter;

            var area = go.AddComponent<BoxCollider>();
            area.isTrigger = true;
            area.center = Vector3.zero;
            area.size = new Vector3(size, 1f, size);

            var spawner = go.AddComponent<EnemySpawnRegion>();
            var serialized = new SerializedObject(spawner);
            serialized.FindProperty("regionId").stringValue = regionId;
            serialized.FindProperty("subRegionId").stringValue = subRegionId;
            serialized.FindProperty("spawnArea").objectReferenceValue = area;
            serialized.FindProperty("spawnedParent").objectReferenceValue = parent;
            serialized.FindProperty("minSpawnCount").intValue = min;
            serialized.FindProperty("maxSpawnCount").intValue = max;
            serialized.FindProperty("respawnWhenCleared").boolValue = true;
            serialized.FindProperty("respawnDelay").floatValue = 30f;
            serialized.FindProperty("patrolRadiusOverride").floatValue = patrolRadius;

            var prefabs = serialized.FindProperty("enemyPrefabs");
            prefabs.arraySize = 1;
            prefabs.GetArrayElementAtIndex(0).objectReferenceValue = prefab;

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
