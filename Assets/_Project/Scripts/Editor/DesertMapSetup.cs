#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Unity.AI.Navigation;
using BES.Gameplay;

namespace BES.EditorTools
{
    public static class DesertMapSetup
    {
        const string IslandScenePath = "Assets/Scenes/BES_Island_GameReady.unity";
        const string DesertScenePath = "Assets/Scenes/desert map.unity";
        const string BigPrefabPath = "Assets/_Project/Prefabs/Enemy_MeshyMonster.prefab";
        const string BabyPrefabPath = "Assets/_Project/Prefabs/Enemy_BabyMonster.prefab";
        const string SpawnRootName = "EnemySpawnRegions_Desert";

        [MenuItem("BES/Gameplay/Setup Desert Map")]
        public static void RunSetup()
        {
            SetupScene(true);
        }

        public static void SetupScene(bool logResult = true)
        {
            // 1. Double check scene copy exists
            if (!File.Exists(DesertScenePath))
            {
                string sourcePath = "Assets/DuNguyn/Egypt Props Pack/Scene/Demo_EgyptProps.unity";
                if (!File.Exists(sourcePath))
                {
                    if (logResult) Debug.LogError($"[BES Desert Setup] Source demo scene not found at: {sourcePath}");
                    return;
                }
                File.Copy(sourcePath, DesertScenePath, true);
                if (File.Exists(sourcePath + ".meta"))
                {
                    File.Copy(sourcePath + ".meta", DesertScenePath + ".meta", true);
                }
                AssetDatabase.Refresh();
            }

            // 2. Open desert map scene
            var desertScene = EditorSceneManager.OpenScene(DesertScenePath, OpenSceneMode.Single);

            // 3. Find or copy GameplayBootstrap
            var existingBootstrap = GameObject.Find("GameplayBootstrap");
            if (existingBootstrap != null)
            {
                Object.DestroyImmediate(existingBootstrap);
            }

            // Open island scene additively to grab copy of configured GameplayBootstrap
            if (File.Exists(IslandScenePath))
            {
                var islandScene = EditorSceneManager.OpenScene(IslandScenePath, OpenSceneMode.Additive);
                var sourceBootstrap = GameObject.Find("GameplayBootstrap");
                if (sourceBootstrap != null)
                {
                    var copyBootstrap = Object.Instantiate(sourceBootstrap);
                    copyBootstrap.name = "GameplayBootstrap";
                    SceneManager.MoveGameObjectToScene(copyBootstrap, desertScene);
                    if (logResult) Debug.Log("[BES Desert Setup] Successfully copied GameplayBootstrap from Island scene.");
                }
                else
                {
                    if (logResult) Debug.LogWarning("[BES Desert Setup] GameplayBootstrap not found in Island scene. Creating dummy bootstrap.");
                    var dummy = new GameObject("GameplayBootstrap");
                    dummy.AddComponent<GameplaySceneBootstrap>();
                    SceneManager.MoveGameObjectToScene(dummy, desertScene);
                }
                EditorSceneManager.CloseScene(islandScene, true);
            }
            else
            {
                var dummy = new GameObject("GameplayBootstrap");
                dummy.AddComponent<GameplaySceneBootstrap>();
                SceneManager.MoveGameObjectToScene(dummy, desertScene);
            }

            // 3.5. Add Mesh Colliders to environment renderers so raycasting works
            var allMeshRenderers = Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
            int collidersAdded = 0;
            foreach (var mr in allMeshRenderers)
            {
                if (mr.gameObject.scene == desertScene && mr.gameObject.name != "Skybox" && mr.gameObject.name != "Sky")
                {
                    if (mr.GetComponent<Collider>() == null)
                    {
                        mr.gameObject.AddComponent<MeshCollider>();
                        collidersAdded++;
                    }
                }
            }
            if (logResult) Debug.Log($"[BES Desert Setup] Added {collidersAdded} MeshColliders to environment elements.");

            // 4. Scans all meshes in scene to calculate map center and ground point
            Vector3 mapCenter = Vector3.zero;
            var renderers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            int count = 0;
            foreach (var r in renderers)
            {
                if (r.gameObject.scene == desertScene && r.gameObject.name != "Skybox" && r.gameObject.name != "Sky")
                {
                    mapCenter += r.bounds.center;
                    count++;
                }
            }
            if (count > 0)
            {
                mapCenter /= count;
            }
            
            // Raycast down to find ground point from mapCenter
            mapCenter = GetGroundPoint(mapCenter);
            if (logResult) Debug.Log($"[BES Desert Setup] Map center ground coordinates calculated: {mapCenter}");

            // Move GameplayBootstrap to mapCenter to spawn player there
            var bootstrapGo = GameObject.Find("GameplayBootstrap");
            if (bootstrapGo != null)
            {
                bootstrapGo.transform.position = mapCenter + Vector3.up * 0.5f;
            }

            // 5. Setup NavMeshSurface and build NavMesh
            var existingSurface = Object.FindAnyObjectByType<NavMeshSurface>();
            if (existingSurface != null)
            {
                Object.DestroyImmediate(existingSurface.gameObject);
            }

            var navSurfaceGo = new GameObject("NavMeshSurface");
            SceneManager.MoveGameObjectToScene(navSurfaceGo, desertScene);
            navSurfaceGo.transform.position = mapCenter;
            var surface = navSurfaceGo.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.All;
            surface.useGeometry = NavMeshCollectGeometry.RenderMeshes;
            
            // Mark ground environment elements static for navmesh baking (including Vietnamese naming conventions in Egypt Props Pack)
            foreach (var r in renderers)
            {
                if (r.gameObject.scene == desertScene && (
                    r.gameObject.name.ToLower().Contains("floor") || 
                    r.gameObject.name.ToLower().Contains("ground") || 
                    r.gameObject.name.ToLower().Contains("stair") || 
                    r.gameObject.name.ToLower().Contains("rock") || 
                    r.gameObject.name.ToLower().Contains("prop") ||
                    r.gameObject.name.ToLower().Contains("nen") ||
                    r.gameObject.name.ToLower().Contains("nha") ||
                    r.gameObject.name.ToLower().Contains("tru") ||
                    r.gameObject.name.ToLower().Contains("da")))
                {
                    GameObjectUtility.SetStaticEditorFlags(r.gameObject, StaticEditorFlags.NavigationStatic);
                }
            }
            
            surface.BuildNavMesh();
            if (logResult) Debug.Log("[BES Desert Setup] NavMesh successfully baked for Desert Map.");

            // 6. Setup Monster Spawn Regions
            var existingSpawns = GameObject.Find(SpawnRootName);
            if (existingSpawns != null)
            {
                Object.DestroyImmediate(existingSpawns);
            }

            var spawnRoot = new GameObject(SpawnRootName);
            SceneManager.MoveGameObjectToScene(spawnRoot, desertScene);
            spawnRoot.transform.position = Vector3.zero;

            var bigPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BigPrefabPath);
            var babyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BabyPrefabPath);

            if (bigPrefab == null || babyPrefab == null)
            {
                if (logResult) Debug.LogError($"[BES Desert Setup] Prefabs missing. Big: {bigPrefab != null}, Baby: {babyPrefab != null}");
            }

            // Create spawn regions at safe offset ground locations
            // Region 1: Desert Ruins (Center-North)
            var ruinsCenter = GetGroundPoint(mapCenter + new Vector3(0f, 0f, 15f));
            CreateRegion(desertScene, spawnRoot.transform, "SpawnRegion_DesertRuins_Big", "region_desert_ruins_big", "sub_ruins_big",
                ruinsCenter, 15f, 1, 1, bigPrefab, null);
            CreateRegion(desertScene, spawnRoot.transform, "SpawnRegion_DesertRuins_Baby", "region_desert_ruins_baby", "sub_ruins_baby",
                ruinsCenter, 15f, 2, 2, babyPrefab, null);

            // Region 2: Pyramid Gate (West)
            var pyramidCenter = GetGroundPoint(mapCenter + new Vector3(-25f, 0f, 10f));
            CreateRegion(desertScene, spawnRoot.transform, "SpawnRegion_PyramidGate_Big", "region_pyramid_gate_big", "sub_gate_big",
                pyramidCenter, 15f, 1, 1, bigPrefab, null);
            CreateRegion(desertScene, spawnRoot.transform, "SpawnRegion_PyramidGate_Baby", "region_pyramid_gate_baby", "sub_gate_baby",
                pyramidCenter, 15f, 2, 2, babyPrefab, null);

            // Region 3: Oasis Border (East-South)
            var oasisCenter = GetGroundPoint(mapCenter + new Vector3(20f, 0f, -20f));
            CreateRegion(desertScene, spawnRoot.transform, "SpawnRegion_OasisBorder_Baby", "region_oasis_border_baby", "sub_oasis_baby",
                oasisCenter, 15f, 2, 3, babyPrefab, null);

            // 7. Save scene
            EditorSceneManager.MarkSceneDirty(desertScene);
            EditorSceneManager.SaveScene(desertScene);
            if (logResult) Debug.Log("[BES Desert Setup] Desert map scene configuration complete and saved successfully.");
        }

        static Vector3 GetGroundPoint(Vector3 searchPos)
        {
            RaycastHit hit;
            if (Physics.Raycast(searchPos + Vector3.up * 100f, Vector3.down, out hit, 500f))
            {
                return hit.point + Vector3.up * 0.1f;
            }
            return new Vector3(searchPos.x, 0.5f, searchPos.z);
        }

        static void CreateRegion(Scene scene, Transform parent, string name, string regionId, string subRegionId,
            Vector3 spawnCenter, float size, int min, int max, GameObject prefabA, GameObject prefabB)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = spawnCenter;

            var area = go.AddComponent<BoxCollider>();
            area.isTrigger = true;
            area.center = Vector3.zero;
            area.size = new Vector3(size, 0.5f, size);

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

            var prefabs = serialized.FindProperty("enemyPrefabs");
            if (prefabA != null && prefabB != null)
            {
                prefabs.arraySize = 2;
                prefabs.GetArrayElementAtIndex(0).objectReferenceValue = prefabA;
                prefabs.GetArrayElementAtIndex(1).objectReferenceValue = prefabB;
            }
            else if (prefabA != null)
            {
                prefabs.arraySize = 1;
                prefabs.GetArrayElementAtIndex(0).objectReferenceValue = prefabA;
            }
            else if (prefabB != null)
            {
                prefabs.arraySize = 1;
                prefabs.GetArrayElementAtIndex(0).objectReferenceValue = prefabB;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
