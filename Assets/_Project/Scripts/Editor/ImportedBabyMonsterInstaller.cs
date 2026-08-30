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
        const string MeshyRootFolder = "Assets/MeshyImports/Model_Quai_Con";

        [MenuItem("BES/Gameplay/Install Baby Monster Spawns")]
        public static void InstallFromMenu() => Install(true);

        public static void Install(bool logResult = true)
        {
            // Force AssetDatabase to detect extracted texture PNGs
            AssetDatabase.Refresh();

            // Resolve folder dynamically to bypass unicode/normalization issues
            string resolvedRoot = ResolveMeshyFolder();
            if (logResult) Debug.Log($"[BES Baby Installer] Resolved baby monster folder path to: {resolvedRoot}");

            // Force reimport enemy.glb to resolve DefaultImporter binary lock in Unity
            string glbPath = resolvedRoot + "/enemy.glb";
            if (File.Exists(glbPath))
            {
                if (logResult) Debug.Log($"[BES Baby Installer] Forcing reimport of GLB model: {glbPath}");
                AssetDatabase.ImportAsset(glbPath, ImportAssetOptions.ForceUpdate);
            }

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
                if (folderName.Contains("model_quai_con") || folderName.Contains("model_quái_con") || (folderName.Contains("quái") && folderName.Contains("con")))
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
                        !clipAnims[i].lockRootRotation ||
                        clipAnims[i].maskType != ClipAnimationMaskType.CreateFromThisModel)
                    {
                        clipAnims[i].loopTime = shouldLoop;
                        clipAnims[i].loopPose = shouldLoop;
                        clipAnims[i].lockRootPositionXZ = true;
                        clipAnims[i].lockRootHeightY = true;
                        clipAnims[i].lockRootRotation = true;
                        clipAnims[i].keepOriginalPositionXZ = true;
                        clipAnims[i].keepOriginalPositionY = true;
                        clipAnims[i].keepOriginalOrientation = true;
                        clipAnims[i].maskType = ClipAnimationMaskType.CreateFromThisModel;
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

        static AnimationClip CreateBiteAttackClip(string resolvedRoot)
        {
            string animFolder = "Assets/_Project/Animations";
            EnsureFolder(animFolder);
            string clipPath = animFolder + "/Enemy_BabyMonster_Bite.anim";

            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if (clip == null)
            {
                clip = new AnimationClip();
                AssetDatabase.CreateAsset(clip, clipPath);
            }
            clip.ClearCurves();
            clip.name = "Enemy_BabyMonster_Bite";
            clip.wrapMode = WrapMode.Once;

            // Load the FBX model to discover relative bone paths
            string fbxPath = resolvedRoot + "/Meshy_AI_Animation_Walking_frame_rate_60.fbx";
            var fbxModel = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (fbxModel == null) return null;

            string headPath = GetRelativeBonePath(fbxModel.transform, "head");
            string jawPath = GetRelativeBonePath(fbxModel.transform, "jaw") ?? GetRelativeBonePath(fbxModel.transform, "dJaw");
            string rootPath = GetRelativeBonePath(fbxModel.transform, "root") ?? GetRelativeBonePath(fbxModel.transform, "Hips");
            string legLPath = GetRelativeBonePath(fbxModel.transform, "frontleg") ?? GetRelativeBonePath(fbxModel.transform, "frontleg0");
            string legRPath = GetRelativeBonePath(fbxModel.transform, "R_frontleg") ?? GetRelativeBonePath(fbxModel.transform, "R_frontleg0");
            string tailPath = GetRelativeBonePath(fbxModel.transform, "tail") ?? GetRelativeBonePath(fbxModel.transform, "tailstart");

            // 1. Root: Pull back during windup, lunge forward during bite impact
            if (!string.IsNullOrEmpty(rootPath))
            {
                var curveZ = new AnimationCurve(
                    new Keyframe(0f, 0f, 0f, 0f),
                    new Keyframe(0.2f, -0.08f, -0.6f, -0.6f),
                    new Keyframe(0.45f, 0.35f, 2.2f, 0f),
                    new Keyframe(0.7f, 0.15f, -0.6f, -0.6f),
                    new Keyframe(1.0f, 0f, 0f, 0f)
                );
                var curveY = new AnimationCurve(
                    new Keyframe(0f, 0f, 0f, 0f),
                    new Keyframe(0.2f, 0.05f, 0.3f, 0.3f),
                    new Keyframe(0.45f, -0.12f, -1.2f, 0f),
                    new Keyframe(1.0f, 0f, 0f, 0f)
                );
                clip.SetCurve(rootPath, typeof(Transform), "m_LocalPosition.z", curveZ);
                clip.SetCurve(rootPath, typeof(Transform), "m_LocalPosition.y", curveY);
            }

            // 2. Head: Pitch up during windup, snap forward-down for bite
            if (!string.IsNullOrEmpty(headPath))
            {
                var curveRotX = new AnimationCurve(
                    new Keyframe(0f, 0f, 0f, 0f),
                    new Keyframe(0.2f, -22f, -80f, -80f),
                    new Keyframe(0.45f, 30f, 180f, 0f),
                    new Keyframe(0.65f, 15f, -40f, -40f),
                    new Keyframe(1.0f, 0f, 0f, 0f)
                );
                var curveRotY = new AnimationCurve(
                    new Keyframe(0f, 0f, 0f, 0f),
                    new Keyframe(0.45f, 10f, 40f, 40f),
                    new Keyframe(0.6f, -10f, -60f, -60f),
                    new Keyframe(1.0f, 0f, 0f, 0f)
                );
                clip.SetCurve(headPath, typeof(Transform), "localEulerAnglesRaw.x", curveRotX);
                clip.SetCurve(headPath, typeof(Transform), "localEulerAnglesRaw.y", curveRotY);
            }

            // 3. Jaw: Open wide, snap shut on impact, secondary chomp
            if (!string.IsNullOrEmpty(jawPath))
            {
                var curveJawX = new AnimationCurve(
                    new Keyframe(0f, 0f, 0f, 0f),
                    new Keyframe(0.2f, 42f, 150f, 0f),
                    new Keyframe(0.45f, -8f, -240f, 0f),
                    new Keyframe(0.55f, 18f, 70f, 0f),
                    new Keyframe(0.7f, 0f, -80f, 0f),
                    new Keyframe(1.0f, 0f, 0f, 0f)
                );
                clip.SetCurve(jawPath, typeof(Transform), "localEulerAnglesRaw.x", curveJawX);
            }

            // 4. Front Legs: Plant forward to absorb lunge impact
            if (!string.IsNullOrEmpty(legLPath))
            {
                var legLCurve = new AnimationCurve(
                    new Keyframe(0f, 0f, 0f, 0f),
                    new Keyframe(0.2f, -15f, -50f, -50f),
                    new Keyframe(0.45f, 22f, 90f, 0f),
                    new Keyframe(1.0f, 0f, 0f, 0f)
                );
                clip.SetCurve(legLPath, typeof(Transform), "localEulerAnglesRaw.x", legLCurve);
            }
            if (!string.IsNullOrEmpty(legRPath))
            {
                var legRCurve = new AnimationCurve(
                    new Keyframe(0f, 0f, 0f, 0f),
                    new Keyframe(0.2f, 12f, 40f, 40f),
                    new Keyframe(0.45f, -18f, -80f, 0f),
                    new Keyframe(1.0f, 0f, 0f, 0f)
                );
                clip.SetCurve(legRPath, typeof(Transform), "localEulerAnglesRaw.x", legRCurve);
            }

            // 5. Tail: Whip up for balance
            if (!string.IsNullOrEmpty(tailPath))
            {
                var tailCurve = new AnimationCurve(
                    new Keyframe(0f, 0f, 0f, 0f),
                    new Keyframe(0.2f, -12f, -40f, -40f),
                    new Keyframe(0.45f, 28f, 130f, 0f),
                    new Keyframe(0.7f, 10f, -50f, -50f),
                    new Keyframe(1.0f, 0f, 0f, 0f)
                );
                clip.SetCurve(tailPath, typeof(Transform), "localEulerAnglesRaw.x", tailCurve);
            }

            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();

            Debug.Log($"[BES Baby Installer] Generated custom Bite attack clip mapped to FBX skeleton at: {clipPath}");
            return clip;
        }

        static string GetRelativeBonePath(Transform root, string boneNameKey)
        {
            var found = FindDeepChild(root, boneNameKey);
            if (found == null || found == root) return null;

            string path = found.name;
            Transform current = found.parent;
            while (current != null && current != root)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }
            return path;
        }

        static Transform FindDeepChild(Transform parent, string name)
        {
            if (parent.name.ToLower().Contains(name.ToLower()))
                return parent;
            foreach (Transform child in parent)
            {
                var result = FindDeepChild(child, name);
                if (result != null) return result;
            }
            return null;
        }

        static AnimatorController CreateAnimatorController(string resolvedRoot)
        {
            string folderPath = "Assets/_Project/AnimatorControllers";
            EnsureFolder(folderPath);
            string controllerPath = folderPath + "/Enemy_BabyMonster.controller";

            AnimationClip walkClip = FindClipInFile(resolvedRoot, "Meshy_AI_Animation_Walking_frame_rate_60.fbx");
            AnimationClip attackClip = CreateBiteAttackClip(resolvedRoot) ?? walkClip;
            AnimationClip idleClip = walkClip; // fallback to walk clip with low speed

            var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);

            var rootStateMachine = controller.layers[0].stateMachine;

            var idleState = rootStateMachine.AddState("Idle");
            idleState.motion = idleClip;
            idleState.speed = 1.0f; // natural ready posture speed (not frozen!)

            var walkState = rootStateMachine.AddState("Walk");
            walkState.motion = walkClip;
            walkState.speed = 1.0f;

            var runState = rootStateMachine.AddState("Run");
            runState.motion = walkClip; // reuse walk clip with higher play speed
            runState.speed = 1.5f;

            var attackState = rootStateMachine.AddState("Attack");
            attackState.motion = attackClip;
            attackState.speed = 1.0f;

            // Transitions
            // Idle <-> Walk
            var idleToWalk = idleState.AddTransition(walkState);
            idleToWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
            idleToWalk.hasExitTime = false;
            idleToWalk.duration = 0.15f;

            var walkToIdle = walkState.AddTransition(idleState);
            walkToIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
            walkToIdle.hasExitTime = false;
            walkToIdle.duration = 0.15f;

            // Walk <-> Run
            var walkToRun = walkState.AddTransition(runState);
            walkToRun.AddCondition(AnimatorConditionMode.Greater, 2.2f, "Speed");
            walkToRun.hasExitTime = false;
            walkToRun.duration = 0.15f;

            var runToWalk = runState.AddTransition(walkState);
            runToWalk.AddCondition(AnimatorConditionMode.Less, 2.2f, "Speed");
            runToWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
            runToWalk.hasExitTime = false;
            runToWalk.duration = 0.15f;

            var runToIdle = runState.AddTransition(idleState);
            runToIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
            runToIdle.hasExitTime = false;
            runToIdle.duration = 0.15f;

            // AnyState -> Attack
            var anyToAttack = rootStateMachine.AddAnyStateTransition(attackState);
            anyToAttack.AddCondition(AnimatorConditionMode.If, 0f, "Attack");
            anyToAttack.hasExitTime = false;
            anyToAttack.duration = 0.1f;

            var attackToIdle = attackState.AddTransition(idleState);
            attackToIdle.hasExitTime = true;
            attackToIdle.exitTime = 1f;
            attackToIdle.duration = 0.2f;

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

            // Use the quadruped FBX model that matches the walking & bite animations natively
            string modelPath = resolvedRoot + "/Meshy_AI_Animation_Walking_frame_rate_60.fbx";
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);

            if (model == null)
            {
                var fbxs = Directory.GetFiles(resolvedRoot, "*.fbx");
                if (fbxs.Length > 0)
                {
                    modelPath = fbxs[0].Replace('\\', '/');
                    model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
                }
            }

            if (model == null)
            {
                Debug.LogError("[BES Baby Installer] Could not load any model FBX from " + resolvedRoot);
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
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.identity;
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
                }

                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.identity;

                var collider = root.AddComponent<CapsuleCollider>();
                collider.height = 2.0f; // Tall cylindrical collider prevents CharacterController from climbing on top
                collider.center = new Vector3(0f, 1.0f, 0f);
                collider.radius = 0.45f;
                collider.isTrigger = true;


                var agent = root.AddComponent<NavMeshAgent>();
                agent.radius = 0.45f;
                agent.height = 2.0f;
                agent.baseOffset = 0f;
                agent.speed = 3.5f;
                agent.angularSpeed = 720f;
                agent.acceleration = 12f;
                agent.stoppingDistance = 1.5f;

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
                var serializedAI = new SerializedObject(ai);
                serializedAI.FindProperty("defaultAnimatorController").objectReferenceValue = controller;
                serializedAI.FindProperty("detectRange").floatValue = 14f;
                serializedAI.FindProperty("attackRange").floatValue = 1.8f;
                serializedAI.FindProperty("attackDamage").floatValue = 6f;
                serializedAI.FindProperty("attackCooldown").floatValue = 1.2f;
                serializedAI.FindProperty("chaseSpeed").floatValue = 3.5f;
                serializedAI.FindProperty("patrolSpeed").floatValue = 1.8f;
                serializedAI.ApplyModifiedPropertiesWithoutUndo();

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
