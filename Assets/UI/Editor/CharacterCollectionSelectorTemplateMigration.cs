using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace BES.UI.Editor
{
    public static class CharacterCollectionSelectorTemplateMigration
    {
        const string PrefabPath = "Assets/_Project/UI/Prefabs/Screens/MenuHub.prefab";
        const string FontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/SVN-MoneyGame SDF.asset";

        [InitializeOnLoadMethod]
        static void CreateIfMissing()
        {
            EditorApplication.delayCall += () => CreateOrUpdate(false);
        }

        [MenuItem("BES/UI/Add Character Collection Selector Template")]
        public static void CreateFromMenu()
        {
            CreateOrUpdate(true);
        }

        static void CreateOrUpdate(bool forceLog)
        {
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            if (root == null) return;

            var dirty = false;
            try
            {
                var selector = FindDeep(root.transform, "OwnedCharacterSelector");
                if (selector == null)
                    selector = FindDeep(root.transform, "characterSelectorContent") ?? FindDeep(root.transform, "CharacterSelectorContent");

                if (selector == null)
                {
                    if (forceLog) Debug.LogWarning("[BES] Không tìm thấy OwnedCharacterSelector trong MenuHub.prefab.");
                    return;
                }

                if (FindDirectChild(selector, "CharacterSelectorCardTemplate") == null)
                {
                    CreateCardTemplate(selector);
                    dirty = true;
                }

                if (dirty)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                    Debug.Log("[BES] Đã thêm CharacterSelectorCardTemplate vào OwnedCharacterSelector trong MenuHub.prefab.");
                }
                else if (forceLog)
                {
                    Debug.Log("[BES] CharacterSelectorCardTemplate đã tồn tại trong MenuHub.prefab.");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static void CreateCardTemplate(Transform parent)
        {
            var card = CreateUiObject("CharacterSelectorCardTemplate", parent, new Vector2(120f, 160f));
            card.SetActive(false);
            var image = card.AddComponent<Image>();
            image.color = Color.white;
            var button = card.AddComponent<Button>();
            button.targetGraphic = image;

            var bg = CreateUiObject("BG", card.transform, Vector2.zero);
            Stretch(bg.GetComponent<RectTransform>());
            bg.AddComponent<Image>().color = Color.white;

            var level = CreateUiObject("CharacterLevel", card.transform, new Vector2(100f, 28f));
            var levelRect = level.GetComponent<RectTransform>();
            levelRect.anchorMin = new Vector2(.5f, 0f);
            levelRect.anchorMax = new Vector2(.5f, 0f);
            levelRect.pivot = new Vector2(.5f, 0f);
            levelRect.anchoredPosition = new Vector2(0f, 10f);
            var text = level.AddComponent<TextMeshProUGUI>();
            text.text = "Lv.1";
            text.fontSize = 20f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(.45f, .18f, .12f, 1f);
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (font != null) text.font = font;

            var selected = CreateUiObject("SelectedState", card.transform, Vector2.zero);
            Stretch(selected.GetComponent<RectTransform>());
            var selectedImage = selected.AddComponent<Image>();
            selectedImage.color = new Color(0f, 0f, 0f, .35f);
            selected.SetActive(false);
        }

        static GameObject CreateUiObject(string name, Transform parent, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            var uiLayer = LayerMask.NameToLayer("UI");
            go.layer = uiLayer >= 0 ? uiLayer : parent.gameObject.layer;
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(.5f, .5f);
            rect.anchorMax = new Vector2(.5f, .5f);
            rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;
            return go;
        }

        static void Stretch(RectTransform rect)
        {
            if (rect == null) return;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        static Transform FindDirectChild(Transform parent, string objectName)
        {
            if (parent == null) return null;
            for (var i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                if (child != null && child.name == objectName) return child;
            }
            return null;
        }

        static Transform FindDeep(Transform root, string objectName)
        {
            if (root == null || string.IsNullOrWhiteSpace(objectName)) return null;
            if (root.name == objectName) return root;
            for (var i = 0; i < root.childCount; i++)
            {
                var result = FindDeep(root.GetChild(i), objectName);
                if (result != null) return result;
            }
            return null;
        }
    }
}
