#if UNITY_EDITOR
using System.Collections.Generic;
using BES.UI.Menu;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace BES.EditorTools
{
    public static class CashShopItemTemplateSync
    {
        const string PrefabPath = "Assets/_Project/UI/Prefabs/Screens/MenuHub.prefab";

        [MenuItem("BES/UI/Sync Cash Shop Items From Item 0 0")]
        public static void Apply()
        {
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var controller = root.GetComponentInChildren<CashShopPanelController>(true);
                if (controller == null) return;

                var serialized = new SerializedObject(controller);
                var items = serialized.FindProperty("items");
                if (items == null || items.arraySize < 2) return;

                var sourceBinding = items.GetArrayElementAtIndex(0);
                var sourceRoot = sourceBinding.FindPropertyRelative("root").objectReferenceValue as GameObject;
                if (sourceRoot == null || sourceRoot.name != "Item_0_0") return;

                var synced = 0;
                for (var i = 1; i < items.arraySize; i++)
                {
                    var binding = items.GetArrayElementAtIndex(i);
                    var targetRoot = binding.FindPropertyRelative("root").objectReferenceValue as GameObject;
                    if (targetRoot == null) continue;

                    var targetRect = targetRoot.GetComponent<RectTransform>();
                    var targetParent = targetRoot.transform.parent;
                    var targetSibling = targetRoot.transform.GetSiblingIndex();
                    var targetLocalPosition = targetRect.localPosition;
                    var targetName = targetRoot.name;
                    var targetArtwork = binding.FindPropertyRelative("artwork").objectReferenceValue as Image;
                    var targetSprite = targetArtwork != null ? targetArtwork.sprite : null;

                    var clone = Object.Instantiate(sourceRoot, targetParent, false);
                    clone.name = targetName;
                    clone.transform.SetSiblingIndex(targetSibling);
                    var cloneRect = clone.GetComponent<RectTransform>();
                    cloneRect.localPosition = targetLocalPosition;

                    binding.FindPropertyRelative("root").objectReferenceValue = clone;
                    CopyReference<Image>(sourceBinding, binding, "artwork", sourceRoot.transform, clone.transform);
                    CopyReference<TMP_Text>(sourceBinding, binding, "nameText", sourceRoot.transform, clone.transform);
                    CopyReference<TMP_Text>(sourceBinding, binding, "priceText", sourceRoot.transform, clone.transform);
                    CopyReference<Button>(sourceBinding, binding, "purchaseButton", sourceRoot.transform, clone.transform);
                    CopyGameObjectReference(sourceBinding, binding, "soldOutState", sourceRoot.transform, clone.transform);

                    var clonedArtwork = binding.FindPropertyRelative("artwork").objectReferenceValue as Image;
                    if (clonedArtwork != null) clonedArtwork.sprite = targetSprite;

                    Object.DestroyImmediate(targetRoot);
                    synced++;
                }

                ArrangeFourColumns(items, sourceRoot.GetComponent<RectTransform>());
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(controller);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log($"[BES] Synchronized {synced} Cash Shop items from Item_0_0.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static void ArrangeFourColumns(SerializedProperty items, RectTransform template)
        {
            var groups = new Dictionary<Transform, List<RectTransform>>();
            for (var i = 0; i < items.arraySize; i++)
            {
                var root = items.GetArrayElementAtIndex(i)
                    .FindPropertyRelative("root").objectReferenceValue as GameObject;
                var rect = root != null ? root.GetComponent<RectTransform>() : null;
                if (rect == null || rect.parent == null) continue;
                if (!groups.TryGetValue(rect.parent, out var list))
                {
                    list = new List<RectTransform>();
                    groups.Add(rect.parent, list);
                }
                list.Add(rect);
            }

            var spacing = new Vector2(template.sizeDelta.x + 18f, template.sizeDelta.y + 24f);
            var first = template.anchoredPosition;
            foreach (var group in groups.Values)
            {
                for (var i = 0; i < group.Count; i++)
                {
                    var rect = group[i];
                    var column = i % 4;
                    var row = i / 4;
                    rect.anchorMin = new Vector2(.5f, .5f);
                    rect.anchorMax = new Vector2(.5f, .5f);
                    rect.pivot = template.pivot;
                    rect.sizeDelta = template.sizeDelta;
                    rect.anchoredPosition = new Vector2(
                        first.x + column * spacing.x,
                        first.y - row * spacing.y);
                }
            }
        }

        static void CopyReference<T>(
            SerializedProperty source,
            SerializedProperty target,
            string field,
            Transform sourceRoot,
            Transform cloneRoot) where T : Component
        {
            var sourceComponent = source.FindPropertyRelative(field).objectReferenceValue as T;
            var targetProperty = target.FindPropertyRelative(field);
            if (sourceComponent == null)
            {
                targetProperty.objectReferenceValue = null;
                return;
            }
            var path = AnimationUtility.CalculateTransformPath(sourceComponent.transform, sourceRoot);
            var transform = string.IsNullOrEmpty(path) ? cloneRoot : cloneRoot.Find(path);
            targetProperty.objectReferenceValue = transform != null ? transform.GetComponent<T>() : null;
        }

        static void CopyGameObjectReference(
            SerializedProperty source,
            SerializedProperty target,
            string field,
            Transform sourceRoot,
            Transform cloneRoot)
        {
            var sourceObject = source.FindPropertyRelative(field).objectReferenceValue as GameObject;
            var targetProperty = target.FindPropertyRelative(field);
            if (sourceObject == null)
            {
                targetProperty.objectReferenceValue = null;
                return;
            }
            var path = AnimationUtility.CalculateTransformPath(sourceObject.transform, sourceRoot);
            var transform = string.IsNullOrEmpty(path) ? cloneRoot : cloneRoot.Find(path);
            targetProperty.objectReferenceValue = transform != null ? transform.gameObject : null;
        }
    }
}
#endif
