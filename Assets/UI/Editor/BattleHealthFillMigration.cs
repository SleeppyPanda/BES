#if UNITY_EDITOR
using BES.UI.Menu;
using BES.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace BES.EditorTools
{
    public static class BattleHealthFillMigration
    {
        const string PrefabPath = "Assets/_Project/UI/Prefabs/Screens/MenuHub.prefab";

        [MenuItem("BES/UI/Release Battle Health Fill RectTransforms")]
        public static void Apply()
        {
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var battle = Find(root.transform, "BattlePanel")?.GetComponent<TurnBattleUI>();
                if (battle == null) return;

                var released = ReleaseAllBattleSliders(battle.transform);
                var serialized = new SerializedObject(battle);
                AssignHealthFills(serialized.FindProperty("allies"));
                AssignHealthFills(serialized.FindProperty("enemies"));
                serialized.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log($"[BES] Released {released} Battle HP Fill RectTransforms from Slider control.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static int ReleaseAllBattleSliders(Transform battlePanel)
        {
            var released = 0;
            foreach (var slider in battlePanel.GetComponentsInChildren<Slider>(true))
            {
                var fillImage = slider.fillRect != null ? slider.fillRect.GetComponent<Image>() : null;
                if (fillImage == null) fillImage = Find(slider.transform, "Fill")?.GetComponent<Image>();
                if (fillImage == null) continue;

                slider.fillRect = null;
                fillImage.type = Image.Type.Filled;
                fillImage.fillMethod = Image.FillMethod.Horizontal;
                fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
                fillImage.fillClockwise = true;

                var driver = slider.GetComponent<FreeLayoutSliderFill>();
                if (driver == null) driver = slider.gameObject.AddComponent<FreeLayoutSliderFill>();
                driver.Configure(slider, fillImage);
                released++;
            }
            return released;
        }

        static void AssignHealthFills(SerializedProperty team)
        {
            if (team == null) return;
            for (var i = 0; i < team.arraySize; i++)
            {
                var unit = team.GetArrayElementAtIndex(i);
                var slider = unit.FindPropertyRelative("healthBar").objectReferenceValue as Slider;
                if (slider == null) continue;
                var fillImage = Find(slider.transform, "Fill")?.GetComponent<Image>();
                if (fillImage == null) continue;
                unit.FindPropertyRelative("healthFill").objectReferenceValue = fillImage;
            }
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
