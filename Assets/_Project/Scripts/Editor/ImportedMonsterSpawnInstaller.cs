#if UNITY_EDITOR
using BES.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace BES.EditorTools
{
    [InitializeOnLoad]
    static class ImportedMonsterSpawnInstaller
    {
        const string ScenePath = "Assets/Scenes/BES_Island_GameReady.unity";
        const string ModelPath = "Assets/3d/base.obj";
        const string MaterialPath = "Assets/3d/Monster_Base.mat";
        const string PrefabFolder = "Assets/_Project/Prefabs";
        const string PrefabPath = PrefabFolder + "/Enemy_ImportedMonster.prefab";
        const string SpawnRootName = "EnemySpawnRegions_ImportedMonster";

        static ImportedMonsterSpawnInstaller() => EditorApplication.delayCall += TryAutoInstall;

        [MenuItem("BES/Gameplay/Install Imported Monster Spawns")]
        public static void InstallFromMenu() => Install(true);

        static void TryAutoInstall()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += TryAutoInstall;
                return;
            }

            Install(false);
        }

        static void Install(bool logResult)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath) == null)
                return;

            // Keep project settings valid even when the scene/prefab was installed by an older installer run.
            EnsureTag("Enemy");
            EnsureLayer("Enemy");

            var scene = SceneManager.GetSceneByPath(ScenePath);
            var openedTemporarily = !scene.IsValid() || !scene.isLoaded;
            if (openedTemporarily)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

            var existing = FindInScene(scene, SpawnRootName);
            if (existing != null)
            {
                if (logResult) Debug.Log("[BES] Imported monster spawn regions are already installed.");
                if (openedTemporarily) EditorSceneManager.CloseScene(scene, true);
                return;
            }

            var prefab = CreateOrUpdatePrefab();
            if (prefab == null)
            {
                if (openedTemporarily) EditorSceneManager.CloseScene(scene, true);
                return;
            }

            CreateSpawnRegions(scene, prefab);
            DisableBootstrapTestEnemy(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[BES] Imported monster prefab and 3 random spawn regions installed in BES_Island_GameReady.");

            if (openedTemporarily)
                EditorSceneManager.CloseScene(scene, true);
        }

        static GameObject CreateOrUpdatePrefab()
        {
            EnsureFolder(PrefabFolder);
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            var material = CreateOrUpdateMaterial();
            var root = new GameObject("Enemy_ImportedMonster");

            try
            {
                EnsureTag("Enemy");
                EnsureLayer("Enemy");
                root.tag = "Enemy";
                var enemyLayer = LayerMask.NameToLayer("Enemy");
                if (enemyLayer >= 0)
                    root.layer = enemyLayer;

                var visual = (GameObject)PrefabUtility.InstantiatePrefab(model);
                visual.name = "Visual";
                visual.transform.SetParent(root.transform, false);
                SetLayerRecursively(visual, root.layer);
                foreach (var renderer in visual.GetComponentsInChildren<Renderer>(true))
                    renderer.sharedMaterial = material;

                var collider = root.AddComponent<CapsuleCollider>();
                collider.center = new Vector3(0f, 1f, 0f);
                collider.height = 2f;
                collider.radius = 0.55f;

                var agent = root.AddComponent<NavMeshAgent>();
                agent.radius = 0.5f;
                agent.height = 2f;
                agent.speed = 3.5f;
                agent.angularSpeed = 720f;
                agent.acceleration = 12f;

                root.AddComponent<EnemyHealth>();
                root.AddComponent<EnemyDamageFeedback>();
                root.AddComponent<EnemyAI>();
                return PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        static Material CreateOrUpdateMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (material == null)
            {
                material = new Material(shader) { name = "Monster_Base" };
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            else if (shader != null)
            {
                material.shader = shader;
            }

            var diffuse = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/3d/texture_diffuse.png");
            var normal = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/3d/texture_normal.png");
            var metallic = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/3d/texture_metallic.png");
            SetTexture(material, "_BaseMap", "_MainTex", diffuse);
            SetTexture(material, "_BumpMap", null, normal);
            SetTexture(material, "_MetallicGlossMap", null, metallic);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0.35f);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.45f);
            material.EnableKeyword("_NORMALMAP");
            material.EnableKeyword("_METALLICSPECGLOSSMAP");
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            return material;
        }

        static void CreateSpawnRegions(Scene scene, GameObject prefab)
        {
            var parent = FindInScene(scene, "05_Interactions_And_Spawn");
            var root = new GameObject(SpawnRootName);
            SceneManager.MoveGameObjectToScene(root, scene);
            if (parent != null)
                root.transform.SetParent(parent.transform, false);

            CreateRegion(root.transform, scene, prefab, "SpawnRegion_NorthForest", "region_north_forest", "north_forest", "NORTH_DenseGreenForest_Resources_Animals", 18f, 2, 4);
            CreateRegion(root.transform, scene, prefab, "SpawnRegion_WestSakura", "region_west_sakura", "west_sakura", "WEST_CherryBlossomForest_Torii_PhotoSpots", 16f, 1, 3);
            CreateRegion(root.transform, scene, prefab, "SpawnRegion_SouthEastCamp", "region_southeast_camp", "southeast_camp", "SOUTHEAST_SeparatedCampingIsland_Bridges", 14f, 1, 3);
        }

        static void CreateRegion(Transform parent, Scene scene, GameObject prefab, string name, string regionId, string subRegionId, string anchorName, float size, int min, int max)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var anchor = FindInScene(scene, anchorName);
            go.transform.position = GetAreaCenter(anchor);

            var area = go.AddComponent<BoxCollider>();
            area.isTrigger = true;
            area.center = new Vector3(0f, 10f, 0f);
            area.size = new Vector3(size, 20f, size);

            var spawner = go.AddComponent<EnemySpawnRegion>();
            var serialized = new SerializedObject(spawner);
            serialized.FindProperty("regionId").stringValue = regionId;
            serialized.FindProperty("subRegionId").stringValue = subRegionId;
            serialized.FindProperty("spawnArea").objectReferenceValue = area;
            serialized.FindProperty("spawnedParent").objectReferenceValue = parent;
            serialized.FindProperty("minSpawnCount").intValue = min;
            serialized.FindProperty("maxSpawnCount").intValue = max;
            serialized.FindProperty("respawnWhenCleared").boolValue = true;
            serialized.FindProperty("respawnDelay").floatValue = 45f;
            var prefabs = serialized.FindProperty("enemyPrefabs");
            prefabs.arraySize = 1;
            prefabs.GetArrayElementAtIndex(0).objectReferenceValue = prefab;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static Vector3 GetAreaCenter(GameObject anchor)
        {
            if (anchor == null)
                return Vector3.zero;

            var renderers = anchor.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                return anchor.transform.position;
            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);
            return new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        }

        static void DisableBootstrapTestEnemy(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            foreach (var bootstrap in root.GetComponentsInChildren<GameplaySceneBootstrap>(true))
            {
                var serialized = new SerializedObject(bootstrap);
                var property = serialized.FindProperty("spawnTestEnemyOnStart");
                if (property == null) continue;
                property.boolValue = false;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        static GameObject FindInScene(Scene scene, string name)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var found = FindRecursive(root.transform, name);
                if (found != null) return found.gameObject;
            }
            return null;
        }

        static Transform FindRecursive(Transform current, string name)
        {
            if (current.name == name) return current;
            for (var i = 0; i < current.childCount; i++)
            {
                var found = FindRecursive(current.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }

        static void SetTexture(Material material, string primary, string fallback, Texture texture)
        {
            if (material.HasProperty(primary)) material.SetTexture(primary, texture);
            else if (!string.IsNullOrEmpty(fallback) && material.HasProperty(fallback)) material.SetTexture(fallback, texture);
        }

        static void EnsureFolder(string path)
        {
            var parts = path.Split('/');
            var current = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        static void SetLayerRecursively(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform) SetLayerRecursively(child.gameObject, layer);
        }

        static void EnsureTag(string tagName)
        {
            foreach (var existing in InternalEditorUtility.tags)
                if (existing == tagName) return;

            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var tags = tagManager.FindProperty("tags");
            tags.InsertArrayElementAtIndex(tags.arraySize);
            tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tagName;
            tagManager.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
        }

        static void EnsureLayer(string layerName)
        {
            if (LayerMask.NameToLayer(layerName) >= 0) return;

            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tagManager.FindProperty("layers");
            for (var i = 8; i < layers.arraySize; i++)
            {
                var layer = layers.GetArrayElementAtIndex(i);
                if (!string.IsNullOrEmpty(layer.stringValue)) continue;
                layer.stringValue = layerName;
                tagManager.ApplyModifiedProperties();
                AssetDatabase.SaveAssets();
                return;
            }

            Debug.LogWarning("[BES] Cannot create Enemy layer because all user layer slots are occupied.");
        }
    }
}
#endif
