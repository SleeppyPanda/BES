#if UNITY_EDITOR
using BES.UI.Menu;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace BES.EditorTools
{
    // Auto-run disabled: manual UI edits must not be overwritten on editor refresh.
    public static class StoryPartySlotEmptyImageMigration
    {
        const string PrefabPath = "Assets/_Project/UI/Prefabs/Screens/MenuHub.prefab";
        const string SessionKey = "BES.StoryPartySlotEmptyImages.v1";

        static StoryPartySlotEmptyImageMigration() => EditorApplication.delayCall += RunOnce;

        static void RunOnce()
        {
            if (SessionState.GetBool(SessionKey, false) || EditorApplication.isPlayingOrWillChangePlaymode) return;
            SessionState.SetBool(SessionKey, true);
            Apply();
        }

        [MenuItem("BES/UI/Convert Story Empty Slots To Images")]
        public static void Apply()
        {
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var controller = root.GetComponentInChildren<StoryModePanelController>(true);
                if (controller == null) return;

                var serialized = new SerializedObject(controller);
                var slots = serialized.FindProperty("beforeSlots");
                var changed = 0;
                for (var i = 0; i < slots.arraySize; i++)
                {
                    var emptyState = slots.GetArrayElementAtIndex(i)
                        .FindPropertyRelative("emptyState").objectReferenceValue as GameObject;
                    if (emptyState == null) continue;

                    var image = emptyState.GetComponent<Image>();
                    if (image == null)
                    {
                        var oldText = emptyState.GetComponent<TMP_Text>();
                        if (oldText != null) Object.DestroyImmediate(oldText);
                        image = emptyState.AddComponent<Image>();
                        image.color = Color.white;
                        image.raycastTarget = false;
                        changed++;
                    }
                    emptyState.name = "EmptyStateImage";
                }

                if (changed > 0)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                    Debug.Log($"[BES] Converted {changed} Story party empty states to assignable Images.");
                }
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
    }
}
#endif
