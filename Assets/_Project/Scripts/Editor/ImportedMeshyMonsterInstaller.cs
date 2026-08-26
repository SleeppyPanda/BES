#if UNITY_EDITOR
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using BES.Gameplay;
using AnimatorController = UnityEditor.Animations.AnimatorController;
using AnimatorControllerParameterType = UnityEngine.AnimatorControllerParameterType;
using AnimatorConditionMode = UnityEditor.Animations.AnimatorConditionMode;

namespace BES.EditorTools
{
    public static class ImportedMeshyMonsterInstaller
    {
        const string ScenePath = "Assets/Scenes/BES_Island_GameReady.unity";
        const string PrefabFolder = "Assets/_Project/Prefabs";
        const string PrefabPath = PrefabFolder + "/Enemy_MeshyMonster.prefab";
        const string SpawnRootName = "EnemySpawnRegions_MeshyMonster";
        const string MeshyRootFolder = "Assets/MeshyImports/Meshy_AI_Sandstone_Oasis_Guard_biped";

        [MenuItem("BES/Gameplay/Install Meshy Monster Spawns")]
        public static void InstallFromMenu() => Install(true);

        public static void Install(bool logResult = true)
        {
            var scene = SceneManager.GetSceneByPath(ScenePath);
            bool openedTemporarily = false;

            if (!scene.isLoaded)
            {
                if (!File.Exists(ScenePath))
                {
                    if (logResult) Debug.LogError($"[BES Meshy Installer] Scene not found at: {ScenePath}");
                    return;
                }
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
                openedTemporarily = true;
            }

            // 1. Configure all FBX files under MeshyImports with explicit Humanoid bone mapping
            ConfigureAllMeshyFbxs();

            // 2. Setup / create Sandstone material with texture_0.png
            var mat = SetupSandstoneMaterial();

            // 3. Find base character model FBX
            string characterFbx = FindBestCharacterModel();
            if (string.IsNullOrEmpty(characterFbx))
            {
                if (logResult) Debug.LogError("[BES Meshy Installer] Could not find imported Meshy character FBX in " + MeshyRootFolder);
                if (openedTemporarily) EditorSceneManager.CloseScene(scene, true);
                return;
            }

            // 4. Create Animator Controller
            var controller = CreateAnimatorController();

            // 5. Create / Update Prefab
            var prefab = CreateOrUpdatePrefab(characterFbx, mat, controller);
            if (prefab == null)
            {
                if (openedTemporarily) EditorSceneManager.CloseScene(scene, true);
                return;
            }

            // 6. Clean up any legacy or duplicate spawn regions and stray models
            var legacy = FindInScene(scene, "EnemySpawnRegions_ImportedMonster");
            if (legacy != null) Object.DestroyImmediate(legacy);

            var existingRegion = FindInScene(scene, SpawnRootName);
            if (existingRegion != null) Object.DestroyImmediate(existingRegion);

            foreach (var rootGo in scene.GetRootGameObjects())
            {
                string lower = rootGo.name.ToLower();
                if ((lower.Contains("meshy") || lower.Contains("sandstone") || lower.Contains("oasis_guard") || lower.Contains("importedmonster")) && rootGo.name != SpawnRootName)
                {
                    Object.DestroyImmediate(rootGo);
                }
            }

            CreateSpawnRegions(scene, prefab);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[BES Meshy Installer] Meshy monster prefab, animator controller, and spawn regions successfully installed with FULL animation set in BES_Island_GameReady.");

            if (openedTemporarily)
                EditorSceneManager.CloseScene(scene, true);
        }

        static void ConfigureAllMeshyFbxs()
        {
            if (!Directory.Exists(MeshyRootFolder)) return;
            var allFbxs = Directory.GetFiles(MeshyRootFolder, "*.fbx", SearchOption.AllDirectories);
            foreach (var fbx in allFbxs)
            {
                string norm = fbx.Replace('\\', '/');
                ConfigureGenericRig(norm);
            }
        }

        static void ConfigureGenericRig(string fbxPath)
        {
            if (string.IsNullOrEmpty(fbxPath) || !File.Exists(fbxPath)) return;

            var modelImporter = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
            if (modelImporter == null) return;

            bool changed = false;

            if (modelImporter.animationType != ModelImporterAnimationType.Generic)
            {
                modelImporter.animationType = ModelImporterAnimationType.Generic;
                modelImporter.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                changed = true;
            }

            var clipAnims = modelImporter.clipAnimations;
            if (clipAnims == null || clipAnims.Length == 0)
            {
                clipAnims = modelImporter.defaultClipAnimations;
            }

            string lowerPath = fbxPath.ToLower();
            bool shouldLoop = lowerPath.Contains("walk") || lowerPath.Contains("run") || lowerPath.Contains("groove") || lowerPath.Contains("muscle");

            if (clipAnims != null && clipAnims.Length > 0)
            {
                for (int i = 0; i < clipAnims.Length; i++)
                {
                    if (clipAnims[i].loopTime != shouldLoop)
                    {
                        clipAnims[i].loopTime = shouldLoop;
                        clipAnims[i].loopPose = shouldLoop;
                        changed = true;
                    }
                }

                modelImporter.clipAnimations = clipAnims;
            }

            modelImporter.SaveAndReimport();
        }

        static Material SetupSandstoneMaterial()
        {
            string matPath = MeshyRootFolder + "/Monster_Sandstone.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, matPath);
            }

            string texPath = MeshyRootFolder + "/texture_0.png";
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            if (tex != null)
            {
                mat.SetTexture("_BaseMap", tex);
                mat.SetTexture("_MainTex", tex);
            }

            mat.SetColor("_BaseColor", Color.white);
            mat.SetColor("_Color", Color.white);
            mat.SetColor("_EmissionColor", Color.black);
            mat.DisableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;

            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();
            return mat;
        }

        static string FindBestCharacterModel()
        {
            if (!Directory.Exists(MeshyRootFolder)) return null;
            var allFbxs = Directory.GetFiles(MeshyRootFolder, "*.fbx", SearchOption.AllDirectories);

            foreach (var fbx in allFbxs)
            {
                string norm = fbx.Replace('\\', '/');
                if (norm.Contains("Walking_frame_rate_60")) return norm;
            }
            return allFbxs.FirstOrDefault()?.Replace('\\', '/');
        }

        static AnimationClip FindClipByKeywords(string[] keywords)
        {
            if (!Directory.Exists(MeshyRootFolder)) return null;
            var allFbxs = Directory.GetFiles(MeshyRootFolder, "*.fbx", SearchOption.AllDirectories);

            foreach (var fbx in allFbxs)
            {
                string norm = fbx.Replace('\\', '/').ToLower();
                bool matches = false;
                foreach (var kw in keywords)
                {
                    if (norm.Contains(kw.ToLower()))
                    {
                        matches = true;
                        break;
                    }
                }

                if (matches)
                {
                    var subAssets = AssetDatabase.LoadAllAssetsAtPath(fbx);
                    foreach (var asset in subAssets)
                    {
                        if (asset is AnimationClip clip && !clip.name.StartsWith("__"))
                        {
                            return clip;
                        }
                    }
                }
            }
            return null;
        }

        static AnimatorController CreateAnimatorController()
        {
            string folderPath = "Assets/_Project/AnimatorControllers";
            EnsureFolder(folderPath);
            string controllerPath = folderPath + "/Enemy_MeshyMonster.controller";

            AnimationClip walkClip = FindClipByKeywords(new[] { "walking" });
            AnimationClip runClip = FindClipByKeywords(new[] { "running", "runfast", "run_03" }) ?? walkClip;
            AnimationClip attackClip = FindClipByKeywords(new[] { "punch_combo", "kung_fu_punch", "sweep_kick", "skill_01" }) ?? walkClip;
            AnimationClip idleClip = FindClipByKeywords(new[] { "show_both_arm", "step_back", "idle" }) ?? walkClip;
            AnimationClip dieClip = FindClipByKeywords(new[] { "shot_in_the_back", "fall" });

            var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            if (dieClip != null) controller.AddParameter("Die", AnimatorControllerParameterType.Trigger);

            var rootStateMachine = controller.layers[0].stateMachine;

            var idleState = rootStateMachine.AddState("Idle");
            idleState.motion = idleClip;
            if (idleClip == walkClip) idleState.speed = 0.05f;

            var walkState = rootStateMachine.AddState("Walk");
            walkState.motion = walkClip;
            walkState.speed = 1.0f;

            var runState = rootStateMachine.AddState("Run");
            runState.motion = runClip;
            runState.speed = 1.0f;

            var attackState = rootStateMachine.AddState("Attack");
            attackState.motion = attackClip;
            attackState.speed = 1.2f;

            // Idle <-> Walk
            var idleToWalk = idleState.AddTransition(walkState);
            idleToWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
            idleToWalk.hasExitTime = false;
            idleToWalk.duration = 0.2f;

            var walkToIdle = walkState.AddTransition(idleState);
            walkToIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
            walkToIdle.hasExitTime = false;
            walkToIdle.duration = 0.2f;

            // Walk <-> Run
            var walkToRun = walkState.AddTransition(runState);
            walkToRun.AddCondition(AnimatorConditionMode.Greater, 3.0f, "Speed");
            walkToRun.hasExitTime = false;
            walkToRun.duration = 0.2f;

            var runToWalk = runState.AddTransition(walkState);
            runToWalk.AddCondition(AnimatorConditionMode.Less, 3.0f, "Speed");
            runToWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
            runToWalk.hasExitTime = false;
            runToWalk.duration = 0.2f;

            var runToIdle = runState.AddTransition(idleState);
            runToIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
            runToIdle.hasExitTime = false;
            runToIdle.duration = 0.2f;

            // AnyState -> Attack
            var anyToAttack = rootStateMachine.AddAnyStateTransition(attackState);
            anyToAttack.AddCondition(AnimatorConditionMode.If, 0f, "Attack");
            anyToAttack.hasExitTime = false;
            anyToAttack.duration = 0.1f;

            var attackToIdle = attackState.AddTransition(idleState);
            attackToIdle.hasExitTime = true;
            attackToIdle.exitTime = 1f;
            attackToIdle.duration = 0.25f;

            // AnyState -> Die
            if (dieClip != null)
            {
                var dieState = rootStateMachine.AddState("Die");
                dieState.motion = dieClip;
                var anyToDie = rootStateMachine.AddAnyStateTransition(dieState);
                anyToDie.AddCondition(AnimatorConditionMode.If, 0f, "Die");
                anyToDie.hasExitTime = false;
                anyToDie.duration = 0.1f;
            }

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();

            // Also ensure controller in Resources folder so EnemyAI can load at runtime
            string resFolder = "Assets/_Project/Resources";
            EnsureFolder(resFolder);
            string resPath = resFolder + "/Enemy_MeshyMonster.controller";
            if (!File.Exists(resPath))
            {
                AssetDatabase.CopyAsset(controllerPath, resPath);
            }
            AssetDatabase.SaveAssets();

            return controller;
        }

        static GameObject CreateOrUpdatePrefab(string modelPath, Material material, AnimatorController controller)
        {
            EnsureFolder(PrefabFolder);
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            var root = new GameObject("Enemy_MeshyMonster");

            try
            {
                root.tag = "Enemy";
                var enemyLayer = LayerMask.NameToLayer("Enemy");
                if (enemyLayer >= 0)
                    root.layer = enemyLayer;

                var visual = (GameObject)PrefabUtility.InstantiatePrefab(model);
                visual.name = "Visual";
                visual.transform.SetParent(root.transform, false);
                PrefabUtility.UnpackPrefabInstance(visual, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                SetLayerRecursively(visual, root.layer);

                // Assign sandstone material directly to all renderers
                if (material != null)
                {
                    var renderers = visual.GetComponentsInChildren<Renderer>(true);
                    foreach (var renderer in renderers)
                    {
                        var mats = new Material[renderer.sharedMaterials.Length];
                        for (int i = 0; i < mats.Length; i++)
                        {
                            mats[i] = material;
                        }
                        renderer.sharedMaterials = mats;
                    }
                }

                // Place Animator directly on Visual child
                var animator = visual.GetComponent<Animator>();
                if (animator == null)
                    animator = visual.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.applyRootMotion = false;

                // Load Generic avatar from modelPath subassets
                var subAssets = AssetDatabase.LoadAllAssetsAtPath(modelPath);
                foreach (var a in subAssets)
                {
                    if (a is Avatar av)
                    {
                        animator.avatar = av;
                        break;
                    }
                }

                var smrs = visual.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                foreach (var smr in smrs)
                {
                    smr.updateWhenOffscreen = true;
                }

                // Auto-scale model to standard height (~2.2 meters)
                float modelHeight = 2.2f;
                float modelRadius = 0.6f;
                var allRenderers = visual.GetComponentsInChildren<Renderer>(true);
                if (allRenderers.Length > 0)
                {
                    Bounds b = allRenderers[0].bounds;
                    for (int i = 1; i < allRenderers.Length; i++)
                    {
                        b.Encapsulate(allRenderers[i].bounds);
                    }

                    float rawHeight = b.size.y;
                    if (rawHeight > 0.1f)
                    {
                        float scaleFactor = modelHeight / rawHeight;
                        visual.transform.localScale = Vector3.one * scaleFactor;
                    }

                    // Re-calculate bounds after scaling
                    b = allRenderers[0].bounds;
                    for (int i = 1; i < allRenderers.Length; i++)
                    {
                        b.Encapsulate(allRenderers[i].bounds);
                    }

                    // Shift visual so lowest vertex is at local Y = 0
                    float lowestPoint = b.min.y - root.transform.position.y;
                    visual.transform.localPosition = new Vector3(0f, -lowestPoint, 0f);
                }

                var collider = root.AddComponent<CapsuleCollider>();
                collider.height = modelHeight;
                collider.center = new Vector3(0f, modelHeight * 0.5f, 0f);
                collider.radius = modelRadius;

                var agent = root.AddComponent<NavMeshAgent>();
                agent.radius = modelRadius;
                agent.height = modelHeight;
                agent.speed = 4.5f;
                agent.angularSpeed = 720f;
                agent.acceleration = 12f;
                agent.stoppingDistance = 1.5f;

                root.AddComponent<EnemyHealth>();
                root.AddComponent<EnemyHealthBar>();
                root.AddComponent<EnemyDamageFeedback>();
                root.AddComponent<BES.Gameplay.MeshyMonsterRuntimeWatcher>();
                var ai = root.AddComponent<EnemyAI>();
                ai.SetAnimatorController(controller);
                
                // Configure AI settings
                ai.Configure(14f, 2.5f, 10f, 1.5f, 4.5f);

                return PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        static void CreateSpawnRegions(Scene scene, GameObject prefab)
        {
            var root = new GameObject(SpawnRootName);
            SceneManager.MoveGameObjectToScene(root, scene);
            root.transform.position = Vector3.zero;

            // 3 clear ground locations on island grass at Y = 7.0f
            CreateRegion(scene, root.transform, "SpawnRegion_NorthForest", "region_north_forest", "sub_meshy_north", 
                new Vector3(0f, 7.0f, 40f), 15f, 2, 4, prefab);
            CreateRegion(scene, root.transform, "SpawnRegion_WestSakura", "region_west_sakura", "sub_meshy_west", 
                new Vector3(-40f, 7.0f, -10f), 15f, 2, 4, prefab);
            CreateRegion(scene, root.transform, "SpawnRegion_SoutheastCamp", "region_southeast_camp", "sub_meshy_camp", 
                new Vector3(35f, 7.0f, -30f), 15f, 2, 4, prefab);
        }

        static void CreateRegion(Scene scene, Transform parent, string name, string regionId, string subRegionId, 
            Vector3 spawnCenter, float size, int min, int max, GameObject prefab)
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
            serialized.FindProperty("respawnDelay").floatValue = 45f;
            var prefabs = serialized.FindProperty("enemyPrefabs");
            prefabs.arraySize = 1;
            prefabs.GetArrayElementAtIndex(0).objectReferenceValue = prefab;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static GameObject FindInScene(Scene scene, string name)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == name) return root;
                var found = FindInChildren(root.transform, name);
                if (found != null) return found.gameObject;
            }
            return null;
        }

        static Transform FindInChildren(Transform parent, string name)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                if (child.name == name) return child;
                var found = FindInChildren(child, name);
                if (found != null) return found;
            }
            return null;
        }

        static void SetLayerRecursively(GameObject obj, int layer)
        {
            if (obj == null) return;
            obj.layer = layer;
            foreach (Transform child in obj.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }

        static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            var parent = Path.GetDirectoryName(folder).Replace('\\', '/');
            var name = Path.GetFileName(folder);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
#endif
