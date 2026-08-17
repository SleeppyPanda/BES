#if UNITY_EDITOR
using BES.UI.Menu;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace BES.UI.Editor
{
    public static class CharacterCollectionMigration
    {
        const string PrefabPath = "Assets/_Project/UI/Prefabs/Screens/MenuHub.prefab";
        const string SpriteRoot = "Assets/Art Ui/Game Việt hóa mới/Character information 2";

        [MenuItem("BES/UI/Build Character Collection")]
        public static void Build()
        {
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var gallery = Find(root.transform, "GalleryPanel");
                var wish = Find(root.transform, "WishPanel");
                var home = root.GetComponentInChildren<MenuHomeController>(true);
                if (gallery == null || wish == null || home == null)
                {
                    Debug.LogError("[BES] Character collection migration could not find GalleryPanel, WishPanel or MenuHomeController.");
                    return;
                }

                var controller = gallery.GetComponent<CharacterCollectionPanel>();
                if (controller == null) controller = gallery.gameObject.AddComponent<CharacterCollectionPanel>();
                var controllerSo = new SerializedObject(controller);
                controllerSo.FindProperty("database").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<MenuContentDatabase>("Assets/Scenes/MenuContentDatabase.asset");
                controllerSo.FindProperty("homeController").objectReferenceValue = home;
                controllerSo.FindProperty("modal").objectReferenceValue = gallery.GetComponent<SimpleModalPanel>();
                controllerSo.FindProperty("wishModal").objectReferenceValue = wish.GetComponent<SimpleModalPanel>();
                controllerSo.FindProperty("galleryReference").objectReferenceValue = LoadSprite(
                    "Assets/Art Ui/Game Việt hóa mới/Character information/Group 427323110.png");
                controllerSo.FindProperty("detailReference").objectReferenceValue = LoadSprite(
                    "Assets/Art Ui/Game Việt hóa mới/Màn hình hoàn chỉnh/Character/Character information-1.png");
                controllerSo.FindProperty("levelReference").objectReferenceValue = LoadSprite(
                    "Assets/Art Ui/Game Việt hóa mới/Màn hình hoàn chỉnh/Character/Nâng level.png");
                controllerSo.FindProperty("affinityReference").objectReferenceValue = LoadSprite(
                    "Assets/Art Ui/Game Việt hóa mới/Màn hình hoàn chỉnh/Character/Thiện cảm.png");
                controllerSo.FindProperty("constellationReference").objectReferenceValue = LoadSprite(
                    "Assets/Art Ui/Game Việt hóa mới/Màn hình hoàn chỉnh/Character/Tinh mệnh.png");
                controllerSo.FindProperty("artifactReference").objectReferenceValue = LoadSprite(
                    "Assets/Art Ui/Game Việt hóa mới/Màn hình hoàn chỉnh/Character/Trang bị di vật.png");
                controllerSo.FindProperty("weaponReference").objectReferenceValue = LoadSprite(
                    "Assets/Art Ui/Game Việt hóa mới/Màn hình hoàn chỉnh/Character/Trang bị vũ khí.png");
                AssignAllCharacterSprites(controllerSo);
                controllerSo.FindProperty("galleryPanelSprite").objectReferenceValue = LoadSprite(
                    "Assets/Art Ui/Game Việt hóa mới/Character information/Rectangle 40291.png");
                controllerSo.FindProperty("combatPowerButtonSprite").objectReferenceValue = LoadSprite(
                    "Assets/Art Ui/Game Việt hóa mới/Character information/Group 427323124.png");
                controllerSo.FindProperty("constellationButtonSprite").objectReferenceValue = LoadSprite(
                    "Assets/Art Ui/Game Việt hóa mới/Character information/Group 427323125.png");
                controllerSo.FindProperty("qualityButtonSprite").objectReferenceValue = LoadSprite(
                    "Assets/Art Ui/Game Việt hóa mới/Character information/Group 427323126.png");
                controllerSo.FindProperty("affinityButtonSprite").objectReferenceValue = LoadSprite(
                    "Assets/Art Ui/Game Việt hóa mới/Character information/Group 427323127.png");
                controllerSo.FindProperty("galleryStarSprite").objectReferenceValue = LoadSprite(
                    "Assets/Art Ui/Game Việt hóa mới/Character information 2/Star 87.png");
                controllerSo.FindProperty("emptyStarSprite").objectReferenceValue = LoadSprite(
                    "Assets/Art Ui/Game Việt hóa mới/Character information/Star 54.png");
                controllerSo.FindProperty("informationTabSprite").objectReferenceValue = LoadSprite(
                    "Assets/Art Ui/Game Việt hóa mới/Character information 2/Group 427323169.png");
                controllerSo.FindProperty("levelTabSprite").objectReferenceValue = LoadSprite(
                    "Assets/Art Ui/Game Việt hóa mới/Character information 2/Group 427323170.png");
                controllerSo.FindProperty("artifactTabSprite").objectReferenceValue = LoadSprite(
                    "Assets/Art Ui/Game Việt hóa mới/Character information 2/Group 427323168.png");
                controllerSo.FindProperty("weaponTabSprite").objectReferenceValue = LoadSprite(
                    "Assets/Art Ui/Game Việt hóa mới/Character information 2/Group 427323167.png");
                controllerSo.FindProperty("constellationTabSprite").objectReferenceValue = LoadSprite(
                    "Assets/Art Ui/Game Việt hóa mới/Character information 2/Group 427323166.png");
                controllerSo.FindProperty("affinityTabSprite").objectReferenceValue = LoadSprite(
                    "Assets/Art Ui/Game Việt hóa mới/Character information 2/Group 427323165.png");
                InitializeRarityMappings(controllerSo);
                controllerSo.ApplyModifiedPropertiesWithoutUndo();
                EnsurePreservingHierarchy(root.transform);

                var homeSo = new SerializedObject(home);
                homeSo.FindProperty("characterCollection").objectReferenceValue = controller;
                homeSo.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                AssetDatabase.SaveAssets();
                CleanupBrokenTmpSubMeshes();
                Debug.Log("[BES] Character Gallery, detail and level flow connected to Wish-owned roster.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static Sprite LoadSprite(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);

        static void AssignAllCharacterSprites(SerializedObject controllerSo)
        {
            var guids = AssetDatabase.FindAssets("t:Sprite", new[] { SpriteRoot });
            System.Array.Sort(guids, (a, b) => string.CompareOrdinal(
                AssetDatabase.GUIDToAssetPath(a), AssetDatabase.GUIDToAssetPath(b)));
            var sprites = controllerSo.FindProperty("characterUiSprites");
            sprites.arraySize = guids.Length;
            for (var i = 0; i < guids.Length; i++)
                sprites.GetArrayElementAtIndex(i).objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(guids[i]));
        }

        static void InitializeRarityMappings(SerializedObject controllerSo)
        {
            var mappings = controllerSo.FindProperty("rarityBackgrounds");
            if (mappings.arraySize > 0) return;
            mappings.arraySize = 4;
            for (var i = 0; i < mappings.arraySize; i++)
                mappings.GetArrayElementAtIndex(i).FindPropertyRelative("rarity").intValue = i + 3;
        }

        static void CleanupBrokenTmpSubMeshes()
        {
            foreach (var subMesh in Resources.FindObjectsOfTypeAll<TMP_SubMeshUI>())
            {
                if (subMesh == null || EditorUtility.IsPersistent(subMesh)) continue;
                var parentText = subMesh.transform.parent != null
                    ? subMesh.transform.parent.GetComponent<TMP_Text>()
                    : null;
                if (parentText != null) continue;
                var scene = subMesh.gameObject.scene;
                Object.DestroyImmediate(subMesh.gameObject);
                if (scene.IsValid()) EditorSceneManager.MarkSceneDirty(scene);
            }
        }

        static void EnsurePreservingHierarchy(Transform root)
        {
            var characterPage = Find(root, "CharacterPage");
            var artifact = Find(root, "ArtifactContent");
            var level = Find(root, "LevelContent");
            var navigation = Find(root, "TabNavigation");
            var information = Find(root, "InformationContent");
            if (characterPage == null || artifact == null || level == null || navigation == null || information == null)
                return;

            EnsureImage("ArtifactListContent", artifact,
                "Assets/Art Ui/Game Việt hóa mới/Màn hình hoàn chỉnh/Character/Trang bị di vật-1.png", Vector2.zero, Vector2.one, false);
            EnsureImage("ArtifactDetailContent", artifact,
                "Assets/Art Ui/Game Việt hóa mới/Màn hình hoàn chỉnh/Character/Trang bị di vật-2.png", Vector2.zero, Vector2.one, false);
            EnsureImage("BreakthroughContent", level,
                "Assets/Art Ui/Game Việt hóa mới/Màn hình hoàn chỉnh/Character/Đột phá khi đủ lv.png", Vector2.zero, Vector2.one, false);

            var indicator = EnsureImage("TabIndicator", navigation,
                "Assets/Art Ui/Game Việt hóa mới/Character information 2/Star 87.png",
                new Vector2(.972f, .724f), new Vector2(.992f, .748f), true);
            indicator.raycastTarget = false;

            var element = EnsureImage("SelectedElementIcon", characterPage, null,
                new Vector2(.202f, .12f), new Vector2(.238f, .18f), true);
            element.preserveAspect = true;
            element.raycastTarget = false;

            var informationPanel = EnsureImage("InformationPanelArtwork", information,
                "Assets/Art Ui/Game Việt hóa mới/Character information 2/Group 427323331.png",
                new Vector2(.525f, .075f), new Vector2(.80f, .88f), true);
            informationPanel.transform.SetAsFirstSibling();
            informationPanel.raycastTarget = false;
            EnsureImage("AttackAttribute", information,
                "Assets/Art Ui/Game Việt hóa mới/Character information 2/Group 427323130.png",
                new Vector2(.545f, .59f), new Vector2(.61f, .72f), true).raycastTarget = false;
            EnsureImage("DefenseAttribute", information,
                "Assets/Art Ui/Game Việt hóa mới/Character information 2/Group 427323131.png",
                new Vector2(.635f, .59f), new Vector2(.70f, .72f), true).raycastTarget = false;
            EnsureImage("HealthAttribute", information,
                "Assets/Art Ui/Game Việt hóa mới/Character information 2/Group 427323132.png",
                new Vector2(.725f, .59f), new Vector2(.79f, .72f), true).raycastTarget = false;

            string[] emptySprites =
            {
                "Assets/Art Ui/Game Việt hóa mới/Character information 2/Rectangle 40322.png",
                "Assets/Art Ui/Game Việt hóa mới/Character information 2/Rectangle 40323.png",
                "Assets/Art Ui/Game Việt hóa mới/Character information 2/Rectangle 40324.png",
                "Assets/Art Ui/Game Việt hóa mới/Character information 2/Rectangle 40315.png"
            };
            for (var i = 0; i < 4; i++)
            {
                var x = .535f + i * .052f;
                var slot = EnsureImage($"InformationEquipmentSlot_{i}", information, emptySprites[i],
                    new Vector2(x, .30f), new Vector2(x + .043f, .385f), true);
                if (slot.GetComponent<Button>() == null) slot.gameObject.AddComponent<Button>();
            }

            string[] skillSprites =
            {
                "Assets/Art Ui/Game Việt hóa mới/Character information 2/Rectangle 40319.png",
                "Assets/Art Ui/Game Việt hóa mới/Character information 2/Rectangle 40320.png",
                "Assets/Art Ui/Game Việt hóa mới/Character information 2/Rectangle 40321.png"
            };
            for (var i = 0; i < 3; i++)
            {
                var x = .615f + i * .055f;
                EnsureImage($"InformationSkillSlot_{i}", information, skillSprites[i],
                    new Vector2(x, .12f), new Vector2(x + .045f, .20f), true).raycastTarget = false;
            }
        }

        static Image EnsureImage(string name, Transform parent, string spritePath,
            Vector2 anchorMin, Vector2 anchorMax, bool active)
        {
            var existing = Find(parent, name);
            if (existing != null)
            {
                var existingImage = existing.GetComponent<Image>();
                return existingImage != null ? existingImage : existing.gameObject.AddComponent<Image>();
            }
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var image = go.GetComponent<Image>();
            image.sprite = string.IsNullOrEmpty(spritePath) ? null : LoadSprite(spritePath);
            image.color = Color.white;
            image.preserveAspect = true;
            go.SetActive(active);
            return image;
        }

        static void DeleteLegacyPanel(Transform root, string panelName)
        {
            var panel = Find(root, panelName);
            if (panel != null) Object.DestroyImmediate(panel.gameObject);
        }

        static Transform Find(Transform root, string name)
        {
            if (root.name == name) return root;
            foreach (Transform child in root)
            {
                var result = Find(child, name);
                if (result != null) return result;
            }
            return null;
        }
    }
}
#endif
