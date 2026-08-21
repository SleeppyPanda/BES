#if UNITY_EDITOR
using BES.UI.Menu;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace BES.EditorTools
{
    // Auto-run disabled: manual UI edits must not be overwritten on editor refresh.
    public static class HomeModeSwitcherViewportMigration
    {
        const string PrefabPath = "Assets/_Project/UI/Prefabs/Screens/MenuHub.prefab";
        const string SessionKey = "BES.HomeModeViewportMigration.v2";

        static HomeModeSwitcherViewportMigration() => EditorApplication.delayCall += RunOnce;

        static void RunOnce()
        {
            if (SessionState.GetBool(SessionKey, false) || EditorApplication.isPlayingOrWillChangePlaymode) return;
            SessionState.SetBool(SessionKey, true);
            PatchPrefab();
        }

        [MenuItem("BES/UI/Fix Home Mode Viewport Only")]
        public static void PatchPrefab()
        {
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var switcher = root.GetComponentInChildren<HomeModeSwitcher>(true);
                if (switcher == null)
                {
                    Debug.LogWarning("[BES] HomeModeSwitcher was not found; MenuHub was not changed.");
                    return;
                }

                var viewport = switcher.transform as RectTransform;
                if (viewport.GetComponent<RectMask2D>() == null) viewport.gameObject.AddComponent<RectMask2D>();
                var raycastGraphic = viewport.GetComponent<Graphic>();
                if (raycastGraphic == null)
                {
                    var image = viewport.gameObject.AddComponent<Image>();
                    image.color = new Color(1f, 1f, 1f, 0.001f);
                    raycastGraphic = image;
                }
                raycastGraphic.raycastTarget = true;

                var serialized = new SerializedObject(switcher);
                serialized.FindProperty("viewport").objectReferenceValue = viewport;
                serialized.FindProperty("enforceViewportMask").boolValue = true;

                var storyObject = serialized.FindProperty("storyModeContent").objectReferenceValue as GameObject;
                var playObject = serialized.FindProperty("playModeContent").objectReferenceValue as GameObject;
                var storyGroup = EnsureCanvasGroup(storyObject);
                var playGroup = EnsureCanvasGroup(playObject);
                serialized.FindProperty("storyCanvasGroup").objectReferenceValue = storyGroup;
                serialized.FindProperty("playCanvasGroup").objectReferenceValue = playGroup;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log("[BES] Home mode viewport patched without rebuilding MenuHub or changing its layout values.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static CanvasGroup EnsureCanvasGroup(GameObject target)
        {
            if (target == null) return null;
            return target.TryGetComponent<CanvasGroup>(out var group) ? group : target.AddComponent<CanvasGroup>();
        }
    }
}
#endif
