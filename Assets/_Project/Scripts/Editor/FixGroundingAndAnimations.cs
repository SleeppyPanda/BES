#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;
using BES.Gameplay;

namespace BES.EditorTools
{
    public static class FixGroundingAndAnimations
    {
        [MenuItem("BES/Gameplay/Fix Grounding and Animations")]
        public static void RunFix()
        {
            Debug.Log("[BES Fix] Starting full grounding & animation repair...");

            // 1. Reconfigure player character models and standing idle animations
            CharacterModelSetup.ConfigureCharacters();

            // 2. Reinstall monsters with zeroed visual transforms & active animations
            ImportedBabyMonsterInstaller.Install(true);
            ImportedMeshyMonsterInstaller.Install(true);

            // 3. Update Desert Map Spawn Regions to snug platform bounds
            DesertMapSetup.SetupScene(true);

            // 4. Fix all Player prefabs to ensure feet touch Y = 0
            string[] playerGuids = AssetDatabase.FindAssets("Player_ t:Prefab", new[] { "Assets/_Project/Prefabs" });
            foreach (var guid in playerGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;

                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                try
                {
                    var cc = instance.GetComponent<CharacterController>();
                    if (cc != null)
                    {
                        float height = 1.8f;
                        cc.height = height;
                        cc.radius = 0.35f;
                        cc.center = new Vector3(0f, height * 0.5f, 0f);
                        cc.skinWidth = 0.02f; // Reduced from 0.08m to prevent floating
                        cc.stepOffset = 0.3f;
                        cc.minMoveDistance = 0f;
                    }

                    // Zero all top-level visual children positions
                    for (int i = 0; i < instance.transform.childCount; i++)
                    {
                        var child = instance.transform.GetChild(i);
                        if (child.name.ToLower().Contains("visual") || child.GetComponentInChildren<SkinnedMeshRenderer>() != null)
                        {
                            child.localPosition = Vector3.zero;
                            child.localRotation = Quaternion.identity;
                        }
                    }

                    PrefabUtility.SaveAsPrefabAsset(instance, path);
                    Debug.Log($"[BES Fix] Adjusted CharacterController & grounded visual on {prefab.name}");
                }
                finally
                {
                    Object.DestroyImmediate(instance);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[BES Fix] Grounding and animations repair complete!");
        }
    }
}
#endif
