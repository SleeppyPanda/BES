using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BES.UI
{
    public static class ChatEnterWidgets
    {
        public static ChatEnterWidget Build(Transform parent, HUDSpriteManifest manifest)
        {
            if (parent == null)
                return null;

            RemoveLegacyChatHint(parent);

            var existing = parent.Find("ChatEnter");
            GameObject go;
            if (existing != null)
                go = EnsureUiRoot(existing.gameObject, parent);
            else
                go = CreateUiObject("ChatEnter", parent);

            var rect = go.GetComponent<RectTransform>();
            UIAnchorPresets.ApplyChatRegion(rect);

            var bubbleGo = GetOrCreate(go.transform, "BubbleIcon");
            var bubbleRect = bubbleGo.GetComponent<RectTransform>();
            bubbleRect.anchorMin = bubbleRect.anchorMax = new Vector2(0f, 0.5f);
            bubbleRect.pivot = new Vector2(0f, 0.5f);
            bubbleRect.sizeDelta = new Vector2(36f, 36f);
            bubbleRect.anchoredPosition = Vector2.zero;
            var bubble = bubbleGo.GetComponent<Image>() ?? bubbleGo.gameObject.AddComponent<Image>();

            var pillGo = GetOrCreate(go.transform, "EnterPill");
            var pillRect = pillGo.GetComponent<RectTransform>();
            pillRect.anchorMin = pillRect.anchorMax = new Vector2(0f, 0.5f);
            pillRect.pivot = new Vector2(0f, 0.5f);
            pillRect.sizeDelta = new Vector2(104f, 34f);
            pillRect.anchoredPosition = new Vector2(40f, 0f);
            var pill = pillGo.GetComponent<Image>() ?? pillGo.gameObject.AddComponent<Image>();

            var labelGo = GetOrCreate(pillGo.transform, "Label");
            var labelRect = labelGo.GetComponent<RectTransform>();
            UIAnchorPresets.StretchFull(labelRect);
            var label = labelGo.GetComponent<TMP_Text>() ?? labelGo.gameObject.AddComponent<TextMeshProUGUI>();
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 15f;
            label.fontStyle = FontStyles.Bold;
            label.color = new Color(0.1f, 0.12f, 0.16f, 1f);

            var widget = go.GetComponent<ChatEnterWidget>() ?? go.AddComponent<ChatEnterWidget>();
            SetPrivate(widget, "bubbleIcon", bubble);
            SetPrivate(widget, "enterPill", pill);
            SetPrivate(widget, "enterLabel", label);
            widget.Apply(manifest);
            return widget;
        }

        static void RemoveLegacyChatHint(Transform parent)
        {
            var legacyHint = parent.Find("ChatHint");
            if (legacyHint == null)
                return;

            DestroyObject(legacyHint.gameObject);
        }

        static GameObject EnsureUiRoot(GameObject go, Transform parent)
        {
            if (go.GetComponent<RectTransform>() != null)
                return go;

            var name = go.name;
            var sibling = go.transform.GetSiblingIndex();
            DestroyObject(go);
            var replacement = CreateUiObject(name, parent);
            replacement.transform.SetSiblingIndex(sibling);
            return replacement;
        }

        static GameObject CreateUiObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        static Transform GetOrCreate(Transform parent, string name)
        {
            var child = parent.Find(name);
            if (child != null)
            {
                if (child.GetComponent<RectTransform>() == null)
                {
                    var sibling = child.GetSiblingIndex();
                    DestroyObject(child.gameObject);
                    child = CreateUiObject(name, parent).transform;
                    child.SetSiblingIndex(sibling);
                }

                return child;
            }

            return CreateUiObject(name, parent).transform;
        }

        static void DestroyObject(GameObject go)
        {
            if (go == null)
                return;

            if (Application.isPlaying)
                Object.Destroy(go);
            else
                Object.DestroyImmediate(go);
        }

        static void SetPrivate(object target, string field, object value)
        {
            target.GetType().GetField(field,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.SetValue(target, value);
        }
    }
}
