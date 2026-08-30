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

            // 4. Quét toàn bộ meshes trong scene để tính tâm bản đồ và giới hạn đường viền
            Vector3 mapCenter = Vector3.zero;
            var renderers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            int count = 0;

            float minX = float.MaxValue, maxX = float.MinValue;
            float minZ = float.MaxValue, maxZ = float.MinValue;

            foreach (var r in renderers)
            {
                if (r.gameObject.scene == desertScene)
                {
                    string lowerName = r.gameObject.name.ToLower();
                    if (!lowerName.Contains("sky") && !lowerName.Contains("dome") && !lowerName.Contains("sun") && !lowerName.Contains("camera") && !lowerName.Contains("light"))
                    {
                        mapCenter += r.bounds.center;
                        count++;

                        var b = r.bounds;
                        if (b.min.x < minX) minX = b.min.x;
                        if (b.max.x > maxX) maxX = b.max.x;
                        if (b.min.z < minZ) minZ = b.min.z;
                        if (b.max.z > maxZ) maxZ = b.max.z;
                    }
                }
            }

            if (count > 0)
            {
                mapCenter /= count;

                // Tạo 4 bức tường giới hạn tàng hình bao quanh viền bản đồ (Map Boundaries)
                minX -= 1.5f;
                maxX += 1.5f;
                minZ -= 1.5f;
                maxZ += 1.5f;

                var existingBoundaries = GameObject.Find("MapBoundaries");
                if (existingBoundaries != null)
                {
                    Object.DestroyImmediate(existingBoundaries);
                }

                var boundariesGo = new GameObject("MapBoundaries");
                SceneManager.MoveGameObjectToScene(boundariesGo, desertScene);
                boundariesGo.transform.position = Vector3.zero;

                float wallHeight = 100f;
                float wallThickness = 2f;

                // Tường phía Bắc (North)
                var north = new GameObject("Boundary_North");
                north.transform.SetParent(boundariesGo.transform, false);
                north.transform.position = new Vector3((minX + maxX) / 2f, wallHeight / 2f, maxZ + wallThickness / 2f);
                var collNorth = north.AddComponent<BoxCollider>();
                collNorth.size = new Vector3(maxX - minX + wallThickness * 2f, wallHeight, wallThickness);

                // Tường phía Nam (South)
                var south = new GameObject("Boundary_South");
                south.transform.SetParent(boundariesGo.transform, false);
                south.transform.position = new Vector3((minX + maxX) / 2f, wallHeight / 2f, minZ - wallThickness / 2f);
                var collSouth = south.AddComponent<BoxCollider>();
                collSouth.size = new Vector3(maxX - minX + wallThickness * 2f, wallHeight, wallThickness);

                // Tường phía Tây (West)
                var west = new GameObject("Boundary_West");
                west.transform.SetParent(boundariesGo.transform, false);
                west.transform.position = new Vector3(minX - wallThickness / 2f, wallHeight / 2f, (minZ + maxZ) / 2f);
                var collWest = west.AddComponent<BoxCollider>();
                collWest.size = new Vector3(wallThickness, wallHeight, maxZ - minZ + wallThickness * 2f);

                // Tường phía Đông (East)
                var east = new GameObject("Boundary_East");
                east.transform.SetParent(boundariesGo.transform, false);
                east.transform.position = new Vector3(maxX + wallThickness / 2f, wallHeight / 2f, (minZ + maxZ) / 2f);
                var collEast = east.AddComponent<BoxCollider>();
                collEast.size = new Vector3(wallThickness, wallHeight, maxZ - minZ + wallThickness * 2f);

                if (logResult) Debug.Log($"[BES Desert Setup] Generated invisible map boundaries: X[{minX:F1} to {maxX:F1}], Z[{minZ:F1} to {maxZ:F1}]");
            }
            
            // Raycast down to find ground point from mapCenter
            mapCenter = GetGroundPoint(mapCenter);
            if (logResult) Debug.Log($"[BES Desert Setup] Map center ground coordinates calculated: {mapCenter}");

            // Move GameplayBootstrap to mapCenter to spawn player there
            var bootstrapGo = GameObject.Find("GameplayBootstrap");
            if (bootstrapGo != null)
            {
                bootstrapGo.transform.position = mapCenter + Vector3.up * 0.5f;
                
                // Tắt tự động sinh capsule đỏ kiểm thử (spawnTestEnemyOnStart = false)
                var bootstrap = bootstrapGo.GetComponent<GameplaySceneBootstrap>();
                if (bootstrap != null)
                {
                    var serializedBootstrap = new SerializedObject(bootstrap);
                    serializedBootstrap.FindProperty("spawnTestEnemyOnStart").boolValue = false;
                    serializedBootstrap.ApplyModifiedProperties();
                }
            }

            // Xóa cục capsule đỏ kiểm thử static nếu có trong scene
            var existingTestEnemy = GameObject.Find("Enemy_TestDamage");
            if (existingTestEnemy != null)
            {
                Object.DestroyImmediate(existingTestEnemy);
                if (logResult) Debug.Log("[BES Desert Setup] Destroyed static Enemy_TestDamage capsule from scene.");
            }

            // Dọn dẹp và chuẩn hóa Camera chính (Main Camera)
            var allCameras = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Camera mainCam = null;
            foreach (var cam in allCameras)
            {
                if (cam.gameObject.scene != desertScene) continue;
                if (mainCam == null && (cam.name.ToLower().Contains("main") || cam.CompareTag("MainCamera")))
                {
                    mainCam = cam;
                }
                else if (mainCam == null)
                {
                    mainCam = cam;
                }
                else
                {
                    // Destroy extra demo showcase camera
                    Object.DestroyImmediate(cam.gameObject);
                }
            }

            if (mainCam == null)
            {
                var camGo = new GameObject("Main Camera");
                SceneManager.MoveGameObjectToScene(camGo, desertScene);
                camGo.tag = "MainCamera";
                mainCam = camGo.AddComponent<Camera>();
                camGo.AddComponent<AudioListener>();
            }

            mainCam.name = "Main Camera";
            mainCam.tag = "MainCamera";
            mainCam.transform.position = mapCenter + new Vector3(0f, 3f, -6f);
            mainCam.transform.LookAt(mapCenter + Vector3.up * 1.2f);
            
            var tpc = mainCam.GetComponent<ThirdPersonCamera>();
            if (tpc == null)
                tpc = mainCam.gameObject.AddComponent<ThirdPersonCamera>();

            // Thiết lập vật liệu Skybox sa mạc hoàng hôn lãng mạn vào RenderSettings
            var skyboxMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Map/AureDevGames/Water Stylized Shader Orto & Perspective Camera/Textures/Skybox/SkyboxAtardecer.mat");
            if (skyboxMat != null)
            {
                RenderSettings.skybox = skyboxMat;
                DynamicGI.UpdateEnvironment();
                if (logResult) Debug.Log("[BES Desert Setup] Applied Sunset Skybox material to RenderSettings.");
            }
            else
            {
                if (logResult) Debug.LogWarning("[BES Desert Setup] Sunset Skybox material not found at path.");
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
            
            // Only mark renderers that are at the playable platform elevation (Y >= 2.0f)
            // Anything below 2.0f is underworld/water/bottom plane and must NOT have NavMesh!
            float minPlayableY = 2.0f;
            foreach (var r in renderers)

            {
                if (r.gameObject.scene == desertScene)
                {
                    bool isPlayableHeight = r.bounds.max.y >= minPlayableY;
                    bool isGroundName = (
                        r.gameObject.name.ToLower().Contains("floor") || 
                        r.gameObject.name.ToLower().Contains("ground") || 
                        r.gameObject.name.ToLower().Contains("stair") || 
                        r.gameObject.name.ToLower().Contains("rock") || 
                        r.gameObject.name.ToLower().Contains("prop") ||
                        r.gameObject.name.ToLower().Contains("nen") ||
                        r.gameObject.name.ToLower().Contains("nha") ||
                        r.gameObject.name.ToLower().Contains("tru") ||
                        r.gameObject.name.ToLower().Contains("da"));

                    if (isPlayableHeight && isGroundName)
                    {
                        GameObjectUtility.SetStaticEditorFlags(r.gameObject, StaticEditorFlags.NavigationStatic);
                    }
                    else
                    {
                        var flags = GameObjectUtility.GetStaticEditorFlags(r.gameObject);
                        flags &= ~StaticEditorFlags.NavigationStatic;
                        GameObjectUtility.SetStaticEditorFlags(r.gameObject, flags);
                    }
                }
            }
            
            surface.BuildNavMesh();
            if (logResult) Debug.Log("[BES Desert Setup] NavMesh successfully baked for Desert Map (Playable Upper Level only).");

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

            // Sinh các vùng Spawn quái phân bổ đều khắp các khu vực riêng biệt trên bản đồ sa mạc
            // Khu vực 1: Tàn tích Cổ Phía Bắc (North Ruins) - Canh gác bởi Quái To
            var northCenter = GetGroundPoint(mapCenter + new Vector3(0f, 0f, 16f));
            CreateRegion(desertScene, spawnRoot.transform, "SpawnRegion_NorthRuins_Big", "region_desert_ruins_big", "sub_ruins_big",
                northCenter, 1, 1, bigPrefab, null);

            // Khu vực 2: Đấu Trường Phía Tây (West Arena) - Bầy quái con
            var westCenter = GetGroundPoint(mapCenter + new Vector3(-16f, 0f, 6f));
            CreateRegion(desertScene, spawnRoot.transform, "SpawnRegion_WestArena_Baby", "region_pyramid_gate_baby", "sub_gate_baby",
                westCenter, 2, 2, babyPrefab, null);

            // Khu vực 3: Cổng Đá Phía Nam (South Gate) - Quái To canh cổng
            var southCenter = GetGroundPoint(mapCenter + new Vector3(6f, 0f, -16f));
            CreateRegion(desertScene, spawnRoot.transform, "SpawnRegion_SouthGate_Big", "region_pyramid_gate_big", "sub_gate_big",
                southCenter, 1, 1, bigPrefab, null);

            // Khu vực 4: Ốc Đảo Phía Đông (East Oasis) - Bầy quái con
            var eastCenter = GetGroundPoint(mapCenter + new Vector3(16f, 0f, -6f));
            CreateRegion(desertScene, spawnRoot.transform, "SpawnRegion_EastOasis_Baby", "region_oasis_border_baby", "sub_oasis_baby",
                eastCenter, 2, 3, babyPrefab, null);

            // Khu vực 5: Hàng Cột Tây Nam (Southwest Colonnade) - Bầy quái con
            var swCenter = GetGroundPoint(mapCenter + new Vector3(-14f, 0f, -14f));
            CreateRegion(desertScene, spawnRoot.transform, "SpawnRegion_SWColonnade_Baby", "region_desert_ruins_baby", "sub_ruins_baby",
                swCenter, 2, 2, babyPrefab, null);

            // 7. Save scene
            EditorSceneManager.MarkSceneDirty(desertScene);
            EditorSceneManager.SaveScene(desertScene);
            if (logResult) Debug.Log("[BES Desert Setup] Desert map scene configuration complete and saved successfully.");
        }

        static Vector3 GetGroundPoint(Vector3 searchPos)
        {
            RaycastHit hit;
            if (Physics.Raycast(searchPos + Vector3.up * 50f, Vector3.down, out hit, 100f))
            {
                return hit.point + Vector3.up * 0.05f;
            }
            return new Vector3(searchPos.x, 0.5f, searchPos.z);
        }

        static void CreateRegion(Scene scene, Transform parent, string name, string regionId, string subRegionId,
            Vector3 spawnCenter, int min, int max, GameObject prefabA, GameObject prefabB)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = spawnCenter;

            // Generate 4 verified, widely-spaced grounded Transform spawn points on the solid platform
            var spawnPointsList = new System.Collections.Generic.List<Transform>();
            Vector3[] offsets = new Vector3[]
            {
                new Vector3(-2.8f, 0f, -2.8f),
                new Vector3(2.8f, 0f, -2.8f),
                new Vector3(-2.8f, 0f, 2.8f),
                new Vector3(2.8f, 0f, 2.8f)
            };

            for (int i = 0; i < offsets.Length; i++)
            {
                var ptGo = new GameObject($"Point_{i + 1}");
                ptGo.transform.SetParent(go.transform, false);
                Vector3 candidate = spawnCenter + offsets[i];
                if (Physics.Raycast(candidate + Vector3.up * 5f, Vector3.down, out RaycastHit ptHit, 10f))
                {
                    if (Mathf.Abs(ptHit.point.y - spawnCenter.y) <= 1.5f)
                    {
                        candidate = ptHit.point + Vector3.up * 0.05f;
                    }
                }
                ptGo.transform.position = candidate;
                spawnPointsList.Add(ptGo.transform);
            }

            var spawner = go.AddComponent<EnemySpawnRegion>();
            var serialized = new SerializedObject(spawner);
            serialized.FindProperty("regionId").stringValue = regionId;
            serialized.FindProperty("subRegionId").stringValue = subRegionId;
            serialized.FindProperty("spawnArea").objectReferenceValue = null; // Use verified spawnPoints directly
            serialized.FindProperty("spawnedParent").objectReferenceValue = parent;
            serialized.FindProperty("minSpawnCount").intValue = min;
            serialized.FindProperty("maxSpawnCount").intValue = max;
            serialized.FindProperty("respawnWhenCleared").boolValue = true;
            serialized.FindProperty("respawnDelay").floatValue = 30f;
            serialized.FindProperty("patrolRadiusOverride").floatValue = 12f;

            var pointsProp = serialized.FindProperty("spawnPoints");
            pointsProp.arraySize = spawnPointsList.Count;
            for (int i = 0; i < spawnPointsList.Count; i++)
            {
                pointsProp.GetArrayElementAtIndex(i).objectReferenceValue = spawnPointsList[i];
            }

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
