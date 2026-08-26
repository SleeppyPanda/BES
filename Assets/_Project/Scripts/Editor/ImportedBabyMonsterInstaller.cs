#if UNITY_EDITOR
using System.IO;
using System.Linq;
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
    public static class ImportedBabyMonsterInstaller
    {
        const string ScenePath = "Assets/Scenes/BES_Island_GameReady.unity";
        const string PrefabFolder = "Assets/_Project/Prefabs";
        const string PrefabPath = PrefabFolder + "/Enemy_BabyMonster.prefab";
        const string SpawnRootName = "EnemySpawnRegions_BabyMonster";
        const string MeshyRootFolder = "Assets/MeshyImports/Model quái con";

        [MenuItem("BES/Gameplay/Install Baby Monster Spawns")]
        public static void InstallFromMenu() => Install(true);

        public static void Install(bool logResult = true)
        {
            // Force AssetDatabase to detect extracted texture PNGs
            AssetDatabase.Refresh();

            var scene = SceneManager.GetSceneByPath(ScenePath);
            bool openedTemporarily = false;

            if (!scene.isLoaded)
            {
                if (!File.Exists(ScenePath))
                {
                    if (logResult) Debug.LogError($"[BES Baby Installer] Scene not found at: {ScenePath}");
                    return;
                }
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
                openedTemporarily = true;
            }

            // Resolve folder dynamically to bypass unicode/normalization issues
            string resolvedRoot = ResolveMeshyFolder();
            if (logResult) Debug.Log($"[BES Baby Installer] Resolved baby monster folder path to: {resolvedRoot}");

            // 1. Configure rig settings for FBX files in this directory (Generic)
            ConfigureAllMeshyFbxs(resolvedRoot);

            // 2. Create Animator Controller
            var controller = CreateAnimatorController(resolvedRoot);

            // 3. Create / Update Prefab
            var prefab = CreateOrUpdatePrefab(resolvedRoot, controller);
            if (prefab == null)
            {
                if (logResult) Debug.LogError("[BES Baby Installer] Failed to create or update prefab.");
                if (openedTemporarily) EditorSceneManager.CloseScene(scene, true);
                return;
            }

            // 4. Clean up duplicate/legacy spawns in Scene
            var existingRegion = FindInScene(scene, SpawnRootName);
            if (existingRegion != null) Object.DestroyImmediate(existingRegion);

            foreach (var rootGo in scene.GetRootGameObjects())
            {
                string lower = rootGo.name.ToLower();
                if ((lower.Contains("babymonster") || lower.Contains("enemy_babymonster")) && rootGo.name != SpawnRootName)
                {
                    Object.DestroyImmediate(rootGo);
                }
            }

            CreateSpawnRegions(scene, prefab);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[BES Baby Installer] Baby monster prefab, animator controller, and spawn regions successfully installed near parents.");

            if (openedTemporarily)
                EditorSceneManager.CloseScene(scene, true);
        }

        static string ResolveMeshyFolder()
        {
            string parent = "Assets/MeshyImports";
            if (!Directory.Exists(parent)) return MeshyRootFolder;
            
            var dirs = Directory.GetDirectories(parent);
            foreach (var dir in dirs)
            {
                string folderName = Path.GetFileName(dir).ToLower();
                if (folderName.Contains("quái") && folderName.Contains("con"))
                {
                    return dir.Replace('\\', '/');
                }
            }
            return MeshyRootFolder;
        }

        static void ConfigureAllMeshyFbxs(string resolvedRoot)
        {
            if (!Directory.Exists(resolvedRoot)) return;
            var allFbxs = Directory.GetFiles(resolvedRoot, "*.fbx", SearchOption.AllDirectories);
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
            bool shouldLoop = lowerPath.Contains("walk") || lowerPath.Contains("run");

            if (clipAnims != null && clipAnims.Length > 0)
            {
                for (int i = 0; i < clipAnims.Length; i++)
                {
                    if (clipAnims[i].loopTime != shouldLoop || 
                        !clipAnims[i].lockRootPositionXZ || 
                        !clipAnims[i].lockRootHeightY || 
                        !clipAnims[i].lockRootRotation)
                    {
                        clipAnims[i].loopTime = shouldLoop;
                        clipAnims[i].loopPose = shouldLoop;
                        clipAnims[i].lockRootPositionXZ = true;
                        clipAnims[i].lockRootHeightY = true;
                        clipAnims[i].lockRootRotation = true;
                        clipAnims[i].keepOriginalPositionXZ = true;
                        clipAnims[i].keepOriginalPositionY = true;
                        clipAnims[i].keepOriginalOrientation = true;
                        changed = true;
                    }
                }

                modelImporter.clipAnimations = clipAnims;
            }

            if (changed)
            {
                modelImporter.SaveAndReimport();
            }
        }

        static AnimationClip FindClipInFile(string resolvedRoot, string relativePath, string clipNameKeyword = null)
        {
            string fullPath = resolvedRoot + "/" + relativePath;
            if (!File.Exists(fullPath)) return null;

            var subAssets = AssetDatabase.LoadAllAssetsAtPath(fullPath);
            foreach (var asset in subAssets)
            {
                if (asset is AnimationClip clip && !clip.name.StartsWith("__"))
                {
                    if (string.IsNullOrEmpty(clipNameKeyword) || clip.name.ToLower().Contains(clipNameKeyword.ToLower()))
                    {
                        return clip;
                    }
                }
            }

            // Fallback to first clip
            foreach (var asset in subAssets)
            {
                if (asset is AnimationClip clip && !clip.name.StartsWith("__"))
                {
                    return clip;
                }
            }

            return null;
        }

        static AnimatorController CreateAnimatorController(string resolvedRoot)
        {
            string folderPath = "Assets/_Project/AnimatorControllers";
            EnsureFolder(folderPath);
            string controllerPath = folderPath + "/Enemy_BabyMonster.controller";

            AnimationClip walkClip = FindClipInFile(resolvedRoot, "Meshy_AI_Animation_Walking_frame_rate_60.fbx");
            AnimationClip attackClip = FindClipInFile(resolvedRoot, "enemy.glb", "UniRigArmatureAction") ?? walkClip;
            AnimationClip idleClip = walkClip; // fallback to walk clip with low speed

            var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);

            var rootStateMachine = controller.layers[0].stateMachine;

            var idleState = rootStateMachine.AddState("Idle");
            idleState.motion = idleClip;
            idleState.speed = 0.05f; // very slow for idle look

            var walkState = rootStateMachine.AddState("Walk");
            walkState.motion = walkClip;
            walkState.speed = 1.0f;

            var runState = rootStateMachine.AddState("Run");
            runState.motion = walkClip; // reuse walk clip with higher play speed
            runState.speed = 1.6f;

            var attackState = rootStateMachine.AddState("Attack");
            attackState.motion = attackClip;
            attackState.speed = 1.0f;

            // Transitions
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
            walkToRun.AddCondition(AnimatorConditionMode.Greater, 2.5f, "Speed");
            walkToRun.hasExitTime = false;
            walkToRun.duration = 0.2f;

            var runToWalk = runState.AddTransition(walkState);
            runToWalk.AddCondition(AnimatorConditionMode.Less, 2.5f, "Speed");
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
            anyToAttack.duration = 0.15f;

            var attackToIdle = attackState.AddTransition(idleState);
            attackToIdle.hasExitTime = true;
            attackToIdle.exitTime = 1f;
            attackToIdle.duration = 0.25f;

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();

            // Resources folder copy for EnemyAI dynamic fallback loading
            string resFolder = "Assets/_Project/Resources";
            EnsureFolder(resFolder);
            string resPath = resFolder + "/Enemy_BabyMonster.controller";
            if (File.Exists(resPath))
            {
                AssetDatabase.DeleteAsset(resPath);
            }
            AssetDatabase.CopyAsset(controllerPath, resPath);
            AssetDatabase.SaveAssets();

            return controller;
        }

        static GameObject CreateOrUpdatePrefab(string resolvedRoot, AnimatorController controller)
        {
            EnsureFolder(PrefabFolder);

            string modelPath = resolvedRoot + "/enemy.glb";
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);

            if (model == null)
            {
                Debug.LogWarning($"[BES Baby Installer] Failed to load GLB model at {modelPath}. Trying FBX fallback...");
                var fbxs = Directory.GetFiles(resolvedRoot, "*.fbx");
                if (fbxs.Length > 0)
                {
                    modelPath = fbxs[0].Replace('\\', '/');
                    model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
                }
            }

            if (model == null)
            {
                Debug.LogError("[BES Baby Installer] Could not load any model (GLB or FBX) from " + resolvedRoot);
                return null;
            }

            // Create or load Baby Material using extracted textures
            string matPath = resolvedRoot + "/Monster_Baby.mat";
            Material babyMat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (babyMat == null)
            {
                babyMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(babyMat, matPath);
            }

            // Set textures on the material
            string baseColorPath = resolvedRoot + "/Baked_BaseColor.png";
            var baseColorTex = AssetDatabase.LoadAssetAtPath<Texture2D>(baseColorPath);
            if (baseColorTex != null)
            {
                babyMat.SetTexture("_BaseMap", baseColorTex);
            }

            string normalPath = resolvedRoot + "/normal.png";
            var textureImporter = AssetImporter.GetAtPath(normalPath) as TextureImporter;
            if (textureImporter != null && textureImporter.textureType != TextureImporterType.NormalMap)
            {
                textureImporter.textureType = TextureImporterType.NormalMap;
                textureImporter.SaveAndReimport();
            }

            var normalTex = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
            if (normalTex != null)
            {
                babyMat.SetTexture("_BumpMap", normalTex);
                babyMat.EnableKeyword("_NORMALMAP");
            }

            EditorUtility.SetDirty(babyMat);
            AssetDatabase.SaveAssets();

            var root = new GameObject("Enemy_BabyMonster");

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

                // Assign the custom Baby Material with its correct textures
                var renderers = visual.GetComponentsInChildren<Renderer>(true);
                foreach (var r in renderers)
                {
                    var mats = r.sharedMaterials;
                    for (int i = 0; i < mats.Length; i++)
                    {
                        mats[i] = babyMat;
                    }
                    r.sharedMaterials = mats;
                }
                Debug.Log("[BES Baby Installer] Assigned custom Baby Material with extracted textures to renderers.");

                // Animator configuration on Visual child
                var animator = visual.GetComponent<Animator>();
                if (animator == null)
                    animator = visual.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.applyRootMotion = false;

                // Load Avatar from model subassets
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

                // Scale baby monster to standard height (~1.1 meters, half of the parent Oasis Guard)
                float modelHeight = 1.1f;
                float modelRadius = 0.35f;
                var allRenderers = visual.GetComponentsInChildren<Renderer>(true);
                if (allRenderers.Length > 0)
                {
                    Bounds b = allRenderers[0].bounds;
                    for (int i = 1; i < allRenderers.Length; i++)
                    {
                        b.Encapsulate(allRenderers[i].bounds);
                    }

                    float rawHeight = b.size.y;
                    if (rawHeight > 0.0001f)
                    {
                        float scaleFactor = modelHeight / rawHeight;
                        visual.transform.localScale = Vector3.one * scaleFactor;
                        Debug.Log($"[BES Baby Installer] Scaled Visual mesh by {scaleFactor}x (Raw height: {rawHeight}m)");
                    }

                    // Re-calculate bounds after scaling
                    b = allRenderers[0].bounds;
                    for (int i = 1; i < allRenderers.Length; i++)
                    {
                        b.Encapsulate(allRenderers[i].bounds);
                    }

                    // Shift visual so lowest point is at local Y = 0
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
                agent.speed = 3.5f; // slightly slower than parent's 4.5
                agent.angularSpeed = 720f;
                agent.acceleration = 12f;
                agent.stoppingDistance = 1.2f;

                // Game systems
                var health = root.AddComponent<EnemyHealth>();
                var serializedHealth = new SerializedObject(health);
                serializedHealth.FindProperty("maxHealth").floatValue = 30f;
                serializedHealth.FindProperty("defense").floatValue = 1f;
                serializedHealth.FindProperty("experienceReward").intValue = 5;
                serializedHealth.ApplyModifiedPropertiesWithoutUndo();

                root.AddComponent<EnemyHealthBar>();
                root.AddComponent<EnemyDamageFeedback>();

                var ai = root.AddComponent<EnemyAI>();
                ai.SetAnimatorController(controller);
                ai.Configure(12f, 1.8f, 6f, 1.2f, 3.5f);

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

            // Spawn near parent spawns at island grass plane Y = 7.0f
            // Parent forest spawn is (0, 7, 40)
            CreateRegion(scene, root.transform, "SpawnRegion_BabyForest", "region_baby_forest", "sub_baby_north", 
                new Vector3(12f, 7.0f, 38f), 12f, 2, 4, prefab);
            // Parent sakura spawn is (-40, 7, -10)
            CreateRegion(scene, root.transform, "SpawnRegion_BabySakura", "region_baby_sakura", "sub_baby_west", 
                new Vector3(-32f, 7.0f, -15f), 12f, 2, 4, prefab);
            // Parent camp spawn is (35, 7, -30)
            CreateRegion(scene, root.transform, "SpawnRegion_BabyCamp", "region_baby_camp", "sub_baby_camp", 
                new Vector3(40f, 7.0f, -28f), 12f, 2, 4, prefab);
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
            serialized.FindProperty("respawnDelay").floatValue = 40f; // slightly quicker respawn
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
