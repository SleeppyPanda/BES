#if UNITY_EDITOR
using BES.UI.Menu;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace BES.EditorTools
{
    public static class BattleArtAssignmentMigration
    {
        const string PrefabPath = "Assets/_Project/UI/Prefabs/Screens/MenuHub.prefab";
        const string ArtPath = "Assets/Art Ui/Mới/Battle/";

        [MenuItem("BES/UI/Assign Available Battle Artwork")]
        public static void Apply()
        {
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var panel = Find(root.transform, "BattlePanel");
                var battle = panel != null ? panel.GetComponent<TurnBattleUI>() : null;
                if (battle == null) return;

                var panelImage = panel.GetComponent<Image>();
                if (panelImage != null)
                {
                    panelImage.sprite = Sprite("Group 427323082.png");
                    panelImage.color = Color.white;
                }

                AssignUnitArtwork(battle);
                AssignButtonArtwork(panel, "SpeedButton", "Group 427322801.png");
                AssignButtonArtwork(panel, "AutoButton", "Group 427322802.png");
                AssignButtonArtwork(panel, "PauseButton", "Group 427322804.png");
                DisableLabel(panel, "SpeedButton");
                DisableLabel(panel, "AutoButton");
                DisableLabel(panel, "PauseButton");
                ClearControllerLabels(battle);

                var skillPanel = Find(panel, "SkillPanel");
                var skillPanelImage = skillPanel != null
                    ? skillPanel.GetComponent<Image>() ?? skillPanel.gameObject.AddComponent<Image>()
                    : null;
                if (skillPanelImage != null)
                {
                    skillPanelImage.sprite = Sprite("Group 427323081.png");
                    skillPanelImage.color = Color.white;
                    skillPanelImage.raycastTarget = false;
                    skillPanelImage.transform.SetAsFirstSibling();
                }

                var skillFrames = new[]
                {
                    "Rectangle 40053.png",
                    "Rectangle 40058.png",
                    "Rectangle 40059.png",
                    "Rectangle 40048.png"
                };
                for (var i = 0; i < 4; i++)
                {
                    AssignButtonArtwork(panel, "SkillButton_" + i, skillFrames[i]);
                    var icon = Find(Find(panel, "SkillButton_" + i), "SkillIcon");
                    if (icon != null)
                    {
                        var image = icon.GetComponent<Image>();
                        image.sprite = Sprite("Star 68.png");
                        image.color = Color.white;
                    }
                }

                var rail = Find(panel, "TurnOrderRail");
                if (rail != null)
                {
                    var image = rail.GetComponent<Image>() ?? rail.gameObject.AddComponent<Image>();
                    image.sprite = Sprite("Group 427323078.png");
                    image.color = Color.white;
                    image.raycastTarget = false;
                }

                var header = Find(panel, "BattleHeader");
                var roundArt = Find(header, "RoundArtwork");
                if (roundArt == null)
                {
                    var go = new GameObject(
                        "RoundArtwork",
                        typeof(RectTransform),
                        typeof(CanvasRenderer),
                        typeof(Image));
                    go.layer = LayerMask.NameToLayer("UI");
                    roundArt = go.transform;
                    roundArt.SetParent(header, false);
                    var rect = roundArt as RectTransform;
                    rect.anchorMin = new Vector2(0f, .15f);
                    rect.anchorMax = new Vector2(.18f, .85f);
                    rect.offsetMin = Vector2.zero;
                    rect.offsetMax = Vector2.zero;
                    var image = roundArt.GetComponent<Image>();
                    image.sprite = Sprite("Group 427322800.png");
                    image.color = Color.white;
                    image.preserveAspect = true;
                    image.raycastTarget = false;
                }

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log("[BES] All available Battle artwork assigned to background, units, controls, skills and turn rail.");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        static void AssignUnitArtwork(TurnBattleUI battle)
        {
            var allyBody = Sprite("Object.png");
            var allyPortrait = Sprite("image 340.png");
            var enemyBody = Sprite("image 336.png");
            var skillIcon = Sprite("Star 68.png");
            var serialized = new SerializedObject(battle);
            AssignTeam(serialized.FindProperty("allies"), allyBody, allyPortrait, skillIcon);
            AssignTeam(serialized.FindProperty("enemies"), enemyBody, enemyBody, skillIcon);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void AssignTeam(
            SerializedProperty team,
            Sprite body,
            Sprite portrait,
            Sprite skillIcon)
        {
            for (var i = 0; i < team.arraySize; i++)
            {
                var definition = team.GetArrayElementAtIndex(i).FindPropertyRelative("definition");
                definition.FindPropertyRelative("battlefieldSprite").objectReferenceValue = body;
                definition.FindPropertyRelative("portrait").objectReferenceValue = portrait;
                var skills = definition.FindPropertyRelative("skills");
                for (var skillIndex = 0; skillIndex < skills.arraySize; skillIndex++)
                    skills.GetArrayElementAtIndex(skillIndex)
                        .FindPropertyRelative("icon").objectReferenceValue = skillIcon;
            }
        }

        static void AssignButtonArtwork(Transform root, string buttonName, string spriteName)
        {
            var button = Find(root, buttonName);
            var image = button != null ? button.GetComponent<Image>() : null;
            if (image == null) return;
            image.sprite = Sprite(spriteName);
            image.color = Color.white;
            image.type = Image.Type.Simple;
        }

        static void DisableLabel(Transform root, string buttonName)
        {
            var button = Find(root, buttonName);
            var label = Find(button, "Label");
            if (label != null) label.gameObject.SetActive(false);
        }

        static void ClearControllerLabels(TurnBattleUI battle)
        {
            var serialized = new SerializedObject(battle);
            serialized.FindProperty("speedText").objectReferenceValue = null;
            serialized.FindProperty("autoText").objectReferenceValue = null;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static Sprite Sprite(string name) =>
            AssetDatabase.LoadAssetAtPath<Sprite>(ArtPath + name);

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
