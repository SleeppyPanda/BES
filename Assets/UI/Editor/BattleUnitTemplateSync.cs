#if UNITY_EDITOR
using BES.UI.Menu;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace BES.EditorTools
{
    public static class BattleUnitTemplateSync
    {
        const string PrefabPath = "Assets/_Project/UI/Prefabs/Screens/MenuHub.prefab";

        [MenuItem("BES/UI/Sync Battle Units From Slot Zero")]
        public static void Apply()
        {
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var panel = Find(root.transform, "BattlePanel");
                var battle = panel != null ? panel.GetComponent<TurnBattleUI>() : null;
                if (battle == null) return;

                var serialized = new SerializedObject(battle);
                SyncCards(serialized.FindProperty("allies"), "Ally");
                SyncCards(serialized.FindProperty("enemies"), "Enemy");
                SyncPlayerBattlefieldImages(serialized.FindProperty("allies"));
                serialized.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log("[BES] Ally, Enemy and PlayerBattlefieldImage objects synchronized from slot 0.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static void SyncCards(SerializedProperty team, string label)
        {
            if (team == null || team.arraySize < 2) return;
            var sourceUnit = team.GetArrayElementAtIndex(0);
            var sourceRoot = sourceUnit.FindPropertyRelative("root").objectReferenceValue as GameObject;
            if (sourceRoot == null) return;

            for (var i = 1; i < team.arraySize; i++)
            {
                var targetUnit = team.GetArrayElementAtIndex(i);
                var targetRoot = targetUnit.FindPropertyRelative("root").objectReferenceValue as GameObject;
                if (targetRoot == null) continue;

                var oldRootRect = targetRoot.GetComponent<RectTransform>();
                var oldBattlefield = targetUnit.FindPropertyRelative("battlefieldImage").objectReferenceValue as Image;
                var oldBattlefieldLayout = oldBattlefield != null
                    ? RectLayout.Capture(oldBattlefield.rectTransform)
                    : default;
                var battlefieldWasInsideCard =
                    oldBattlefield != null && oldBattlefield.transform.IsChildOf(targetRoot.transform);

                var parent = targetRoot.transform.parent;
                var siblingIndex = targetRoot.transform.GetSiblingIndex();
                var rootPosition = oldRootRect != null ? RectPosition.Capture(oldRootRect) : default;
                var clone = Object.Instantiate(sourceRoot, parent);
                clone.name = label + "_" + i;
                clone.transform.SetSiblingIndex(siblingIndex);
                if (clone.TryGetComponent<RectTransform>(out var cloneRect))
                    rootPosition.Apply(cloneRect);

                CopyViewReference(sourceUnit, targetUnit, "targetButton", sourceRoot.transform, clone.transform);
                CopyViewReference(sourceUnit, targetUnit, "portrait", sourceRoot.transform, clone.transform);
                CopyViewReference(sourceUnit, targetUnit, "healthBar", sourceRoot.transform, clone.transform);
                CopyViewReference(sourceUnit, targetUnit, "healthText", sourceRoot.transform, clone.transform);
                CopyViewReference(sourceUnit, targetUnit, "animator", sourceRoot.transform, clone.transform);

                targetUnit.FindPropertyRelative("root").objectReferenceValue = clone;
                if (battlefieldWasInsideCard)
                {
                    CopyViewReference(
                        sourceUnit,
                        targetUnit,
                        "battlefieldImage",
                        sourceRoot.transform,
                        clone.transform);
                    var newBattlefield =
                        targetUnit.FindPropertyRelative("battlefieldImage").objectReferenceValue as Image;
                    if (newBattlefield != null) oldBattlefieldLayout.Apply(newBattlefield.rectTransform);
                }

                Object.DestroyImmediate(targetRoot);
            }
        }

        static void SyncPlayerBattlefieldImages(SerializedProperty allies)
        {
            if (allies == null || allies.arraySize < 2) return;
            var source = allies.GetArrayElementAtIndex(0)
                .FindPropertyRelative("battlefieldImage").objectReferenceValue as Image;
            if (source == null) return;

            for (var i = 1; i < allies.arraySize; i++)
            {
                var target = allies.GetArrayElementAtIndex(i)
                    .FindPropertyRelative("battlefieldImage").objectReferenceValue as Image;
                if (target == null) continue;

                var placement = RectPlacement.Capture(target.rectTransform);
                EditorUtility.CopySerialized(source, target);
                CopyRectAppearance(source.rectTransform, target.rectTransform);
                placement.Apply(target.rectTransform);
                target.name = "PlayerBattlefieldImage_" + i;
            }
        }

        static void CopyViewReference(
            SerializedProperty sourceUnit,
            SerializedProperty targetUnit,
            string field,
            Transform sourceRoot,
            Transform cloneRoot)
        {
            var sourceObject = sourceUnit.FindPropertyRelative(field).objectReferenceValue;
            var targetProperty = targetUnit.FindPropertyRelative(field);
            if (sourceObject == null)
            {
                targetProperty.objectReferenceValue = null;
                return;
            }

            var sourceComponent = sourceObject as Component;
            if (sourceComponent == null) return;
            var path = AnimationUtility.CalculateTransformPath(sourceComponent.transform, sourceRoot);
            var cloneTransform = string.IsNullOrEmpty(path) ? cloneRoot : cloneRoot.Find(path);
            targetProperty.objectReferenceValue =
                cloneTransform != null ? cloneTransform.GetComponent(sourceComponent.GetType()) : null;
        }

        static void CopyRectAppearance(RectTransform source, RectTransform target)
        {
            target.pivot = source.pivot;
            target.sizeDelta = source.sizeDelta;
            target.localRotation = source.localRotation;
            target.localScale = source.localScale;
        }

        readonly struct RectPosition
        {
            readonly Vector2 anchorMin;
            readonly Vector2 anchorMax;
            readonly Vector2 anchoredPosition;

            RectPosition(RectTransform rect)
            {
                anchorMin = rect.anchorMin;
                anchorMax = rect.anchorMax;
                anchoredPosition = rect.anchoredPosition;
            }

            public static RectPosition Capture(RectTransform rect) => new(rect);
            public void Apply(RectTransform rect)
            {
                rect.anchorMin = anchorMin;
                rect.anchorMax = anchorMax;
                rect.anchoredPosition = anchoredPosition;
            }
        }

        readonly struct RectPlacement
        {
            readonly Vector2 anchorMin;
            readonly Vector2 anchorMax;
            readonly Vector2 anchoredPosition;

            RectPlacement(RectTransform rect)
            {
                anchorMin = rect.anchorMin;
                anchorMax = rect.anchorMax;
                anchoredPosition = rect.anchoredPosition;
            }

            public static RectPlacement Capture(RectTransform rect) => new(rect);
            public void Apply(RectTransform rect)
            {
                rect.anchorMin = anchorMin;
                rect.anchorMax = anchorMax;
                rect.anchoredPosition = anchoredPosition;
            }
        }

        readonly struct RectLayout
        {
            readonly Vector2 anchorMin;
            readonly Vector2 anchorMax;
            readonly Vector2 anchoredPosition;

            RectLayout(RectTransform rect)
            {
                anchorMin = rect.anchorMin;
                anchorMax = rect.anchorMax;
                anchoredPosition = rect.anchoredPosition;
            }

            public static RectLayout Capture(RectTransform rect) => new(rect);
            public void Apply(RectTransform rect)
            {
                rect.anchorMin = anchorMin;
                rect.anchorMax = anchorMax;
                rect.anchoredPosition = anchoredPosition;
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
