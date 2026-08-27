using TMPro;
using UnityEditor;
using UnityEngine;

namespace BES.UI.Editor
{
    public static class CharacterInformationStatsPrefabMigration
    {
        const string PrefabPath = "Assets/_Project/UI/Prefabs/Screens/MenuHub.prefab";
        const string FontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/zhcn SDF.asset";
        const string MaterialPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/zhcn SDF Material.mat";

        static readonly (string name, string text, Vector2 position)[] StatTexts =
        {
            ("CharacterEnergyStatText", "0 lượt", new Vector2(385f, -115f)),
            ("CharacterCritRateStatText", "0%", new Vector2(385f, -155f)),
            ("CharacterCritDamageStatText", "0%", new Vector2(385f, -195f)),
            ("CharacterElementText", "Hệ", new Vector2(385f, -235f)),
            ("CharacterRoleText", "Vai trò", new Vector2(385f, -275f)),
        };

        [InitializeOnLoadMethod]
        static void CreateIfMissing()
        {
            EditorApplication.delayCall += () => CreateOrUpdate(false);
        }

        [MenuItem("BES/UI/Add Character Information Stat Texts")]
        public static void CreateFromMenu()
        {
            CreateOrUpdate(true);
        }

        static void CreateOrUpdate(bool forceLog)
        {
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            if (root == null)
                return;

            var dirty = false;
            try
            {
                var info = FindDeep(root.transform, "InformationContent");
                if (info == null)
                {
                    if (forceLog) Debug.LogWarning("[BES] Không tìm thấy InformationContent trong MenuHub.prefab.");
                    return;
                }

                var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
                var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
                foreach (var stat in StatTexts)
                {
                    var existing = FindDeep(info, stat.name);
                    if (existing != null)
                        continue;

                    CreateText(info, stat.name, stat.text, stat.position, font, material);
                    dirty = true;
                }

                if (dirty)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                    Debug.Log("[BES] Đã thêm 5 text thông số mới vào CharacterPage/InformationContent trong MenuHub.prefab.");
                }
                else if (forceLog)
                {
                    Debug.Log("[BES] 5 text thông số mới đã tồn tại trong MenuHub.prefab.");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static void CreateText(Transform parent, string objectName, string text, Vector2 anchoredPosition, TMP_FontAsset font, Material material)
        {
            var go = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            var uiLayer = LayerMask.NameToLayer("UI");
            go.layer = uiLayer >= 0 ? uiLayer : parent.gameObject.layer;

            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(.5f, .5f);
            rect.anchorMax = new Vector2(.5f, .5f);
            rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(420f, 36f);

            var label = go.GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.font = font != null ? font : label.font;
            if (material != null) label.fontSharedMaterial = material;
            label.fontSize = 25f;
            label.color = new Color(.28f, .16f, .11f, 1f);
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
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
