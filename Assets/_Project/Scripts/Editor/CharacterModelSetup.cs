#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;
using BES.UI;
using BES.Gameplay;
using AnimatorController = UnityEditor.Animations.AnimatorController;
using AnimatorControllerParameterType = UnityEngine.AnimatorControllerParameterType;
using AnimatorConditionMode = UnityEditor.Animations.AnimatorConditionMode;

namespace BES.EditorTools
{
    public static class CharacterModelSetup
    {
        const string CharacterFolder = "Assets/Model character";
        const string PrefabFolder = "Assets/_Project/Prefabs";
        const string ControllerPath = "Assets/_Project/AnimatorControllers/PlayerAnimatorController.controller";
        const string DatabasePath = "Assets/Resources/Data/CharacterDatabase.asset";

        [MenuItem("BES/Gameplay/Configure Character Models")]
        public static void ConfigureCharacters()
        {
            AssetDatabase.Refresh();

            // 1. Define paths for character models
            string alinaPath = "Assets/Model character/Alina.fbx";
            
            // New Meshy AI models
            string acolyteFolder = "Assets/MeshyImports/Meshy_AI_Gilded_Shadow_Acolyte_biped";
            string dancerFolder = "Assets/MeshyImports/Meshy_AI_Emerald_Palace_Dancer_biped";
            
            string acolytePath = $"{acolyteFolder}/Meshy_AI_Gilded_Shadow_Acolyte_biped_Animation_Walking_frame_rate_60.fbx";
            string dancerPath = $"{dancerFolder}/Meshy_AI_Emerald_Palace_Dancer_biped_Animation_Walking_frame_rate_60.fbx";

            // 2. Configure Humanoid rigs and extract textures for all files
            ConfigureHumanoidRig(alinaPath);
            
            if (Directory.Exists(acolyteFolder))
            {
                var files = Directory.GetFiles(acolyteFolder, "*.fbx", SearchOption.AllDirectories);
                foreach (var f in files)
                {
                    ConfigureHumanoidRig(f.Replace('\\', '/'));
                }
            }

            if (Directory.Exists(dancerFolder))
            {
                var files = Directory.GetFiles(dancerFolder, "*.fbx", SearchOption.AllDirectories);
                foreach (var f in files)
                {
                    ConfigureHumanoidRig(f.Replace('\\', '/'));
                }
            }

            // 3. Create or Update the Player Animator Controller (Base)
            var baseController = CreateOrUpdateAnimatorController();
            if (baseController == null)
            {
                Debug.LogError("[BES Character Setup] Failed to create Player Animator Controller.");
                return;
            }

            // Create override controllers for each character (leaving idleFile null to use the shared standing idle)
            var elioController = CreateOrUpdateOverrideController(
                baseController,
                "elio",
                acolyteFolder,
                null,
                "Meshy_AI_Gilded_Shadow_Acolyte_biped_Animation_Walking_frame_rate_60.fbx",
                "Meshy_AI_Gilded_Shadow_Acolyte_biped_Animation_Running_frame_rate_60.fbx",
                "Meshy_AI_Gilded_Shadow_Acolyte_biped_Animation_dying_backwards_frame_rate_60.fbx"
            );

            var aurelianController = CreateOrUpdateOverrideController(
                baseController,
                "aurelian",
                dancerFolder,
                null,
                "Meshy_AI_Emerald_Palace_Dancer_biped_Animation_Walking_frame_rate_60.fbx",
                "Meshy_AI_Emerald_Palace_Dancer_biped_Animation_Running_frame_rate_60.fbx",
                "Meshy_AI_Emerald_Palace_Dancer_biped_Animation_dying_backwards_frame_rate_60.fbx"
            );

            var rashadController = CreateOrUpdateOverrideController(
                baseController,
                "rashad",
                dancerFolder,
                null,
                "Meshy_AI_Emerald_Palace_Dancer_biped_Animation_Walking_frame_rate_60.fbx",
                "Meshy_AI_Emerald_Palace_Dancer_biped_Animation_Running_frame_rate_60.fbx",
                "Meshy_AI_Emerald_Palace_Dancer_biped_Animation_dying_backwards_frame_rate_60.fbx"
            );

            // sahure (Sahure / Alina) has no animations, so it falls back to the base controller (acolyte animations)
            var sahureController = baseController;

            // 4. Create Prefabs for each character
            var elioPrefab = CreateCharacterPrefab(acolytePath, elioController, "Player_elio", 1.8f);
            var aurelianPrefab = CreateCharacterPrefab(dancerPath, aurelianController, "Player_aurelian", 1.8f);
            var rashadPrefab = CreateCharacterPrefab(dancerPath, rashadController, "Player_rashad", 1.8f);
            var sahurePrefab = CreateCharacterPrefab(alinaPath, sahureController, "Player_sahure", 1.8f);

            // 5. Update the CharacterDatabase asset
            var db = AssetDatabase.LoadAssetAtPath<CharacterDatabase>(DatabasePath);
            if (db == null)
            {
                Debug.LogError($"[BES Character Setup] CharacterDatabase not found at: {DatabasePath}");
                return;
            }

            var elio = db.Get("elio");
            if (elio != null) elio.gameplayPrefab = elioPrefab;

            var aurelian = db.Get("aurelian");
            if (aurelian != null) aurelian.gameplayPrefab = aurelianPrefab;

            var sahure = db.Get("sahure");
            if (sahure != null) sahure.gameplayPrefab = sahurePrefab;

            var rashad = db.Get("rashad");
            if (rashad != null) rashad.gameplayPrefab = rashadPrefab;

            EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();

            Debug.Log("[BES Character Setup] Configuration and setup completed successfully! New character models configured and database linked.");
        }

        static void ConfigureHumanoidRig(string fbxPath)
        {
            if (string.IsNullOrEmpty(fbxPath) || !File.Exists(fbxPath)) return;

            var importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
            if (importer == null) return;

            // Extract embedded textures to the same folder so Unity can read and apply them
            string folder = Path.GetDirectoryName(fbxPath);
            importer.ExtractTextures(folder);

            bool changed = false;
            if (importer.animationType != ModelImporterAnimationType.Human)
            {
                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                changed = true;
            }

            // Loop and bake root motion into pose for looping walk/run animations
            var clipAnims = importer.clipAnimations;
            if (clipAnims == null || clipAnims.Length == 0)
            {
                clipAnims = importer.defaultClipAnimations;
            }

            if (clipAnims != null && clipAnims.Length > 0)
            {
                for (int i = 0; i < clipAnims.Length; i++)
                {
                    string name = clipAnims[i].name.ToLower();
                    bool shouldLoop = name.Contains("walk") || name.Contains("run") || name.Contains("idle") || name.Contains("take");

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
                        clipAnims[i].keepOriginalPositionXZ = false; // center of mass to prevent drift
                        clipAnims[i].keepOriginalPositionY = true;
                        clipAnims[i].keepOriginalOrientation = true;
                        changed = true;
                    }
                }
                importer.clipAnimations = clipAnims;
            }

            if (changed)
            {
                importer.SaveAndReimport();
            }
        }

        static AnimatorController CreateOrUpdateAnimatorController(string mainModelPath = null)
        {
            EnsureFolder(Path.GetDirectoryName(ControllerPath));

            if (File.Exists(ControllerPath))
            {
                AssetDatabase.DeleteAsset(ControllerPath);
            }

            var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);

            var rootStateMachine = controller.layers[0].stateMachine;

            // Load clips from acolyte FBX files
            string acolyteFolder = "Assets/MeshyImports/Meshy_AI_Gilded_Shadow_Acolyte_biped";
            
            // Load standing Idle clip from Alina.fbx as the shared idle animation
            var alinaAssets = AssetDatabase.LoadAllAssetsAtPath("Assets/Model character/Alina.fbx");
            AnimationClip idleClip = alinaAssets.OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__"));

            AnimationClip walkClip = LoadClipFromFBX($"{acolyteFolder}/Meshy_AI_Gilded_Shadow_Acolyte_biped_Animation_Walking_frame_rate_60.fbx");
            AnimationClip runClip = LoadClipFromFBX($"{acolyteFolder}/Meshy_AI_Gilded_Shadow_Acolyte_biped_Animation_Running_frame_rate_60.fbx");
            AnimationClip dyingClip = LoadClipFromFBX($"{acolyteFolder}/Meshy_AI_Gilded_Shadow_Acolyte_biped_Animation_dying_backwards_frame_rate_60.fbx");

            // Fallback
            if (idleClip == null)
            {
                idleClip = alinaAssets.OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__"));
            }
            if (walkClip == null) walkClip = idleClip;
            if (runClip == null) runClip = walkClip;
            if (dyingClip == null) dyingClip = idleClip;

            var idleState = rootStateMachine.AddState("Idle");
            idleState.motion = idleClip;

            var walkState = rootStateMachine.AddState("Walk");
            walkState.motion = walkClip;

            var runState = rootStateMachine.AddState("Run");
            runState.motion = runClip;

            var dyingState = rootStateMachine.AddState("Dying");
            dyingState.motion = dyingClip;

            controller.AddParameter("Die", AnimatorControllerParameterType.Trigger);
            var dyingTransition = rootStateMachine.AddAnyStateTransition(dyingState);
            dyingTransition.AddCondition(AnimatorConditionMode.If, 0f, "Die");
            dyingTransition.hasExitTime = false;
            dyingTransition.duration = 0.2f;

            // Transitions: Idle <-> Walk
            var idleToWalk = idleState.AddTransition(walkState);
            idleToWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
            idleToWalk.hasExitTime = false;
            idleToWalk.duration = 0.25f;

            var walkToIdle = walkState.AddTransition(idleState);
            walkToIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
            walkToIdle.hasExitTime = false;
            walkToIdle.duration = 0.25f;

            // Transitions: Walk <-> Run
            var walkToRun = walkState.AddTransition(runState);
            walkToRun.AddCondition(AnimatorConditionMode.Greater, 4.5f, "Speed");
            walkToRun.hasExitTime = false;
            walkToRun.duration = 0.25f;

            var runToWalk = runState.AddTransition(walkState);
            runToWalk.AddCondition(AnimatorConditionMode.Less, 4.5f, "Speed");
            runToWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
            runToWalk.hasExitTime = false;
            runToWalk.duration = 0.25f;

            var runToIdle = runState.AddTransition(idleState);
            runToIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
            runToIdle.hasExitTime = false;
            runToIdle.duration = 0.25f;

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        static AnimationClip LoadClipFromFBX(string fbxPath)
        {
            if (!File.Exists(fbxPath)) return null;
            var subAssets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            foreach (var a in subAssets)
            {
                if (a is AnimationClip clip && !clip.name.StartsWith("__"))
                {
                    return clip;
                }
            }
            return null;
        }

        static RuntimeAnimatorController CreateOrUpdateOverrideController(
            AnimatorController baseController,
            string characterName,
            string fbxFolder,
            string idleFile,
            string walkFile,
            string runFile,
            string dyingFile)
        {
            string overridePath = $"Assets/_Project/AnimatorControllers/PlayerAnimatorController_{characterName}.overrideController";
            EnsureFolder(Path.GetDirectoryName(overridePath));

            if (File.Exists(overridePath))
            {
                AssetDatabase.DeleteAsset(overridePath);
            }

            var overrideController = new AnimatorOverrideController(baseController);
            
            // Load custom clips from the respective FBX files
            AnimationClip customIdle = string.IsNullOrEmpty(idleFile) ? null : LoadClipFromFBX($"{fbxFolder}/{idleFile}");
            AnimationClip customWalk = LoadClipFromFBX($"{fbxFolder}/{walkFile}");
            AnimationClip customRun = LoadClipFromFBX($"{fbxFolder}/{runFile}");
            AnimationClip customDying = LoadClipFromFBX($"{fbxFolder}/{dyingFile}");

            // Load base clips from Gilded Shadow Acolyte files (which are used in the baseController)
            string acolyteFolder = "Assets/MeshyImports/Meshy_AI_Gilded_Shadow_Acolyte_biped";
            AnimationClip baseIdle = LoadClipFromFBX($"{acolyteFolder}/Meshy_AI_Gilded_Shadow_Acolyte_biped_Animation_Standard_Forward_Charge_inplace_frame_rate_60.fbx");
            AnimationClip baseWalk = LoadClipFromFBX($"{acolyteFolder}/Meshy_AI_Gilded_Shadow_Acolyte_biped_Animation_Walking_frame_rate_60.fbx");
            AnimationClip baseRun = LoadClipFromFBX($"{acolyteFolder}/Meshy_AI_Gilded_Shadow_Acolyte_biped_Animation_Running_frame_rate_60.fbx");
            AnimationClip baseDying = LoadClipFromFBX($"{acolyteFolder}/Meshy_AI_Gilded_Shadow_Acolyte_biped_Animation_dying_backwards_frame_rate_60.fbx");

            // Override them!
            var overrides = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<AnimationClip, AnimationClip>>();
            
            if (baseIdle != null && customIdle != null) overrides.Add(new(baseIdle, customIdle));
            if (baseWalk != null && customWalk != null) overrides.Add(new(baseWalk, customWalk));
            if (baseRun != null && customRun != null) overrides.Add(new(baseRun, customRun));
            if (baseDying != null && customDying != null) overrides.Add(new(baseDying, customDying));

            overrideController.ApplyOverrides(overrides);
            AssetDatabase.CreateAsset(overrideController, overridePath);
            AssetDatabase.SaveAssets();
            
            return overrideController;
        }

        static GameObject CreateCharacterPrefab(string modelPath, RuntimeAnimatorController controller, string prefabName, float height)
        {
            EnsureFolder(PrefabFolder);
            string finalPrefabPath = $"{PrefabFolder}/{prefabName}.prefab";

            var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (model == null) return null;

            var root = new GameObject(prefabName);
            var visual = (GameObject)PrefabUtility.InstantiatePrefab(model);
            visual.name = "Visual";
            visual.transform.SetParent(root.transform, false);
            PrefabUtility.UnpackPrefabInstance(visual, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

            // Clean up duplicate armatures/meshes (.001) and Blender scene default exports (Camera, Light, Empty, etc.)
            var allChildren = visual.GetComponentsInChildren<Transform>(true);
            foreach (var t in allChildren)
            {
                if (t == null || t.gameObject == visual) continue;
                
                string name = t.name;
                string lower = name.ToLower();
                if (name.EndsWith(".001") || 
                    lower == "metarig" || 
                    lower == "camera" || 
                    lower == "light" || 
                    lower == "empty" || 
                    lower.Contains("cube") || 
                    lower.Contains("sphere") ||
                    lower.Contains("beta_joints") ||
                    lower.Contains("beta_surface"))
                {
                    Object.DestroyImmediate(t.gameObject);
                }
            }

            // Add Animator to Visual child and configure
            var animator = visual.GetComponent<Animator>();
            if (animator == null) animator = visual.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            // Set Avatar
            var subAssets = AssetDatabase.LoadAllAssetsAtPath(modelPath);
            foreach (var a in subAssets)
            {
                if (a is Avatar av)
                {
                    animator.avatar = av;
                    break;
                }
            }

            // Ensure skinned meshes update offscreen
            var smrs = visual.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (var smr in smrs)
            {
                smr.updateWhenOffscreen = true;
            }

            // Upgrade materials to URP Lit if they use Standard built-in shaders
            var renderers = visual.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                var mats = r.sharedMaterials;
                bool matChanged = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] != null && (mats[i].shader.name == "Standard" || mats[i].shader.name.Contains("Default") || mats[i].shader.name == "Standard (Specular Setup)"))
                    {
                        var uShader = Shader.Find("Universal Render Pipeline/Lit");
                        if (uShader != null)
                        {
                            mats[i].shader = uShader;
                            matChanged = true;
                        }
                    }
                }
                if (matChanged)
                {
                    r.sharedMaterials = mats;
                }
            }

            // Automatically scale visual model to proper height using local sharedMesh bounds (stable in Editor scripts)
            float rawHeight = 0f;
            Bounds localBounds = new Bounds(Vector3.zero, Vector3.zero);
            bool hasBounds = false;

            var allSmrs = visual.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (var smr in allSmrs)
            {
                if (smr.sharedMesh != null)
                {
                    if (!hasBounds) { localBounds = smr.sharedMesh.bounds; hasBounds = true; }
                    else localBounds.Encapsulate(smr.sharedMesh.bounds);
                }
            }

            var allMfs = visual.GetComponentsInChildren<MeshFilter>(true);
            foreach (var mf in allMfs)
            {
                if (mf.sharedMesh != null)
                {
                    if (!hasBounds) { localBounds = mf.sharedMesh.bounds; hasBounds = true; }
                    else localBounds.Encapsulate(mf.sharedMesh.bounds);
                }
            }

            if (hasBounds)
            {
                rawHeight = localBounds.size.y;
                if (rawHeight > 0.01f)
                {
                    float scaleFactor = height / rawHeight;
                    visual.transform.localScale = Vector3.one * scaleFactor;

                    float lowestPoint = localBounds.min.y * scaleFactor;
                    float offsetX = localBounds.center.x * scaleFactor;
                    float offsetZ = localBounds.center.z * scaleFactor;
                    visual.transform.localPosition = new Vector3(-offsetX, -lowestPoint, -offsetZ);
                }
            }

            // Add RootMotionFixer to prevent hips translation drift during walk loops
            root.AddComponent<RootMotionFixer>();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, finalPrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
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
