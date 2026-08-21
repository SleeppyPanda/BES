#if UNITY_EDITOR
using BES.UI.Menu;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace BES.EditorTools
{
    // Auto-run disabled: manual UI edits must not be overwritten on editor refresh.
    public static class BattlePlayerFieldMigration
    {
        const string PrefabPath = "Assets/_Project/UI/Prefabs/Screens/MenuHub.prefab";
        const string SessionKey = "BES.BattlePlayerField.v1";

        static readonly Vector2[] Positions =
        {
            new(.23f, .37f),
            new(.33f, .43f),
            new(.43f, .37f),
            new(.53f, .46f)
        };

        static BattlePlayerFieldMigration() => EditorApplication.delayCall += RunOnce;

        static void RunOnce()
        {
            if (SessionState.GetBool(SessionKey, false) || EditorApplication.isPlayingOrWillChangePlaymode) return;
            SessionState.SetBool(SessionKey, true);
            Apply();
        }

        [MenuItem("BES/UI/Create Four Player Battlefield Views")]
        public static void Apply()
        {
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var panel = Find(root.transform, "BattlePanel");
                var battle = panel != null ? panel.GetComponent<TurnBattleUI>() : null;
                if (battle == null) return;

                var field = FindDirect(panel, "PlayerBattlefield");
                if (field == null)
                {
                    var go = new GameObject("PlayerBattlefield", typeof(RectTransform));
                    go.layer = LayerMask.NameToLayer("UI");
                    field = go.transform;
                    field.SetParent(panel, false);
                    var fieldRect = field as RectTransform;
                    fieldRect.anchorMin = Vector2.zero;
                    fieldRect.anchorMax = Vector2.one;
                    fieldRect.offsetMin = Vector2.zero;
                    fieldRect.offsetMax = Vector2.zero;
                    field.SetSiblingIndex(Mathf.Min(2, panel.childCount - 1));
                }

                var serialized = new SerializedObject(battle);
                var allies = serialized.FindProperty("allies");
                for (var i = 0; i < Mathf.Min(4, allies.arraySize); i++)
                {
                    var unit = allies.GetArrayElementAtIndex(i);
                    var image = unit.FindPropertyRelative("battlefieldImage").objectReferenceValue as Image;
                    if (image == null)
                    {
                        var go = new GameObject(
                            "PlayerBattlefieldImage_" + i,
                            typeof(RectTransform),
                            typeof(CanvasRenderer),
                            typeof(Image));
                        go.layer = LayerMask.NameToLayer("UI");
                        image = go.GetComponent<Image>();
                        image.color = Color.white;
                        image.preserveAspect = true;
                        image.raycastTarget = false;
                        unit.FindPropertyRelative("battlefieldImage").objectReferenceValue = image;
                    }

                    image.name = "PlayerBattlefieldImage_" + i;
                    image.transform.SetParent(field, false);
                    image.transform.SetSiblingIndex(i);
                    image.color = Color.white;
                    image.preserveAspect = true;
                    image.raycastTarget = false;

                    var rect = image.rectTransform;
                    rect.anchorMin = Positions[i];
                    rect.anchorMax = Positions[i];
                    rect.pivot = new Vector2(.5f, 0f);
                    rect.anchoredPosition = Vector2.zero;
                    rect.sizeDelta = new Vector2(170f, 250f);
                    rect.localScale = Vector3.one;
                }
                serialized.ApplyModifiedPropertiesWithoutUndo();

                // Keep cards and HP above the battlefield character art.
                for (var i = 0; i < 4; i++)
                {
                    var card = FindDirect(panel, "Ally_" + i);
                    if (card != null) card.SetAsLastSibling();
                }

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log("[BES] Four independent player battlefield images created and kept separate from ally cards.");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        static Transform FindDirect(Transform root, string name)
        {
            if (root == null) return null;
            foreach (Transform child in root) if (child.name == name) return child;
            return null;
        }

        static Transform Find(Transform root, string name)
        {
            if (root == null) return null;
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
                if (child.name == name) return child;
            return null;
        }
    }
}
#endif
