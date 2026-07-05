using BES.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BES.UI
{
    /// <summary>
    /// Minimap + quest + portrait chip theo Main play.png.
    /// </summary>
    public static class TopLeftHudWidgets
    {
        public static void ApplyTopLeftCluster(Transform hudLayer, HUDSpriteManifest manifest)
        {
            if (hudLayer == null)
                return;

            ApplyPortraitChip(hudLayer, manifest);
            ApplyLockBtn(hudLayer, manifest);
            ApplyMiniMap(hudLayer.Find("MiniMap"), manifest);
            ApplyQuestTracker(hudLayer.Find("QuestTracker"), manifest);
        }

        public static void ApplyPortraitChip(Transform hudLayer, HUDSpriteManifest manifest)
        {
            var chip = EnsureUiRoot(hudLayer, "PortraitChip");
            var chipRect = chip.GetComponent<RectTransform>();
            UIAnchorPresets.ApplyPortraitChipRegion(chipRect);
            RemoveImageIfPresent(chip);

            var ring = EnsureChildImage(chip, "Ring");
            var ringRect = ring.GetComponent<RectTransform>();
            UIAnchorPresets.StretchFull(ringRect);
            var ringImg = ring.GetComponent<Image>();
            if (manifest?.portraitChipRing != null && HUDPrimitiveStyles.IsWhitelistedFrameSprite(manifest.portraitChipRing))
            {
                ringImg.sprite = manifest.portraitChipRing;
                ringImg.type = Image.Type.Simple;
                ringImg.preserveAspect = true;
                ringImg.color = Color.white;
            }
            else if (manifest?.minimapRing != null && HUDPrimitiveStyles.IsWhitelistedMinimapRingSprite(manifest.minimapRing))
            {
                ringImg.sprite = manifest.minimapRing;
                ringImg.type = Image.Type.Simple;
                ringImg.preserveAspect = true;
                ringImg.color = Color.white;
            }
            else
            {
                ringImg.sprite = HUDPrimitiveStyles.GetMinimapRingSprite();
                ringImg.type = Image.Type.Simple;
                ringImg.preserveAspect = true;
                ringImg.color = Color.white;
            }

            var portraitMask = EnsureChildImage(chip, "PortraitMask");
            var maskRect = portraitMask.GetComponent<RectTransform>();
            UIAnchorPresets.StretchFull(maskRect);
            maskRect.offsetMin = new Vector2(5f, 5f);
            maskRect.offsetMax = new Vector2(-5f, -5f);
            var maskImg = portraitMask.GetComponent<Image>();
            maskImg.sprite = HUDPrimitiveStyles.GetMinimapFaceSprite();
            maskImg.color = Color.white;
            if (portraitMask.GetComponent<Mask>() == null)
            {
                var mask = portraitMask.gameObject.AddComponent<Mask>();
                mask.showMaskGraphic = false;
            }

            var portrait = EnsureChildImage(portraitMask, "Portrait");
            var pRect = portrait.GetComponent<RectTransform>();
            UIAnchorPresets.StretchFull(pRect);

            var img = portrait.GetComponent<Image>();
            img.type = Image.Type.Simple;
            img.preserveAspect = true;
            img.color = Color.white;

            var portraitManifest = CharacterPortraitManifestLoader.Load();
            var roster = PartyRoster.Instance ?? Object.FindAnyObjectByType<PartyRoster>();
            Sprite portraitSprite = null;
            if (roster != null && portraitManifest != null)
            {
                var member = roster.GetSlot(roster.ActiveCharacterIndex);
                if (member != null && !string.IsNullOrEmpty(member.characterId))
                    portraitSprite = portraitManifest.GetPortrait(member.characterId);
            }

            if (portraitSprite == null && manifest?.partyPortraitDefault != null &&
                HUDPrimitiveStyles.IsWhitelistedIconSprite(manifest.partyPortraitDefault))
                portraitSprite = manifest.partyPortraitDefault;

            if (portraitSprite != null)
            {
                img.sprite = portraitSprite;
                img.color = Color.white;
            }
            else
            {
                img.sprite = null;
                img.color = new Color(0.35f, 0.4f, 0.5f, 0.65f);
            }

            portrait.SetAsLastSibling();
        }

        public static void ApplyLockBtn(Transform hudLayer, HUDSpriteManifest manifest)
        {
            var lockBtn = EnsureUiRoot(hudLayer, "LockBtn");
            var lockRect = lockBtn.GetComponent<RectTransform>();
            UIAnchorPresets.ApplyLockBtnRegion(lockRect);
            RemoveImageIfPresent(lockBtn);

            var frame = EnsureChildImage(lockBtn, "Frame");
            var frameRect = frame.GetComponent<RectTransform>();
            UIAnchorPresets.StretchFull(frameRect);
            var frameImg = frame.GetComponent<Image>();
            if (manifest?.lockBtnFrame != null)
                HUDPrimitiveStyles.TryApplySmallFrame(frameImg, manifest.lockBtnFrame);
            else
                HUDPrimitiveStyles.ApplySolidPanel(frameImg, new Color(1f, 1f, 1f, 0.12f));
            frameImg.color = new Color(1f, 1f, 1f, 0.22f);

            var icon = EnsureChildImage(lockBtn, "Icon");
            var iconRect = icon.GetComponent<RectTransform>();
            iconRect.anchorMin = iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.sizeDelta = new Vector2(18f, 18f);
            iconRect.anchoredPosition = Vector2.zero;
            ApplyIcon(icon.GetComponent<Image>(), manifest?.lockIcon, new Color(1f, 1f, 1f, 0.9f));
        }

        public static void ApplyMiniMap(Transform miniMapRoot, HUDSpriteManifest manifest)
        {
            if (miniMapRoot == null)
                return;

            var rect = miniMapRoot.GetComponent<RectTransform>();
            if (rect != null)
                UIAnchorPresets.ApplyMiniMapRegion(rect);

            RemoveLegacyImage(miniMapRoot);

            var ring = EnsureChildImage(miniMapRoot, "Ring");
            var ringRect = ring.GetComponent<RectTransform>();
            UIAnchorPresets.StretchFull(ringRect);
            ApplyMinimapRing(ring.GetComponent<Image>(), manifest?.minimapRing);

            var face = EnsureChildRect(miniMapRoot, "Face");
            var faceRect = face.GetComponent<RectTransform>();
            UIAnchorPresets.StretchFull(faceRect);
            faceRect.offsetMin = new Vector2(8f, 8f);
            faceRect.offsetMax = new Vector2(-8f, -8f);
            var faceImg = face.GetComponent<Image>() ?? face.gameObject.AddComponent<Image>();
            faceImg.sprite = HUDPrimitiveStyles.GetMinimapFaceSprite();
            faceImg.type = Image.Type.Simple;
            faceImg.preserveAspect = true;
            faceImg.color = Color.white;

            EnsureMapDot(face, "PlayerIcon", manifest?.playerDot, new Color(0.45f, 0.95f, 0.55f));
            EnsureMapDot(face, "ObjectiveIcon", manifest?.objectiveDot, new Color(0.98f, 0.88f, 0.35f));

            var north = EnsureChildText(miniMapRoot, "NorthMark", "N", new Vector2(78f, -10f), 13f);
            north.fontStyle = FontStyles.Bold;
            north.color = new Color(1f, 1f, 1f, 0.85f);

            var expand = EnsureChildText(miniMapRoot, "ExpandHint", "+", new Vector2(132f, -132f), 18f);
            expand.color = new Color(1f, 1f, 1f, 0.75f);

            var miniMapUi = miniMapRoot.GetComponent<MiniMapUI>();
            if (miniMapUi != null)
            {
                SetPrivate(miniMapUi, "mapRect", faceRect);
                SetPrivate(miniMapUi, "playerIcon", face.Find("PlayerIcon") as RectTransform);
                SetPrivate(miniMapUi, "objectiveIcon", face.Find("ObjectiveIcon") as RectTransform);
            }
        }

        public static void ApplyQuestTracker(Transform questRoot, HUDSpriteManifest manifest)
        {
            if (questRoot == null)
                return;

            var rect = questRoot.GetComponent<RectTransform>();
            if (rect != null)
                UIAnchorPresets.ApplyQuestTrackerRegion(rect);

            var frame = EnsureChildImage(questRoot, "TrackerFrame");
            var frameRect = frame.GetComponent<RectTransform>();
            UIAnchorPresets.StretchFull(frameRect);
            var frameImg = frame.GetComponent<Image>();
            frameImg.color = new Color(0f, 0f, 0f, 0.18f);
            frameImg.sprite = null;
            frame.transform.SetAsFirstSibling();

            var book = EnsureChildImage(questRoot, "BookIcon");
            LayoutTopLeft(book, new Vector2(0f, -8f), new Vector2(24f, 24f));
            ApplyIcon(book.GetComponent<Image>(), manifest?.questBookIcon, new Color(1f, 1f, 1f, 0.9f));

            var star = EnsureChildImage(questRoot, "QuestStarIcon");
            LayoutTopLeft(star, new Vector2(28f, -10f), new Vector2(16f, 16f));
            ApplyIcon(star.GetComponent<Image>(), manifest?.questStarIcon ?? manifest?.playerDot, Color.white);

            var title = EnsureChildText(questRoot, "QuestTitle", "TITLE OF MISSIONS", new Vector2(50f, -8f), 16f);
            title.fontStyle = FontStyles.Bold;
            title.alignment = TextAlignmentOptions.TopLeft;

            var step = EnsureChildText(questRoot, "QuestStep", "Task instructions", new Vector2(28f, -34f), 13f);
            step.color = new Color(0.92f, 0.92f, 0.95f, 0.92f);
            step.alignment = TextAlignmentOptions.TopLeft;

            var compass = EnsureChildImage(questRoot, "CompassArrow");
            var compassRect = compass.GetComponent<RectTransform>();
            compassRect.anchorMin = compassRect.anchorMax = new Vector2(1f, 0.5f);
            compassRect.pivot = new Vector2(1f, 0.5f);
            compassRect.sizeDelta = new Vector2(20f, 20f);
            compassRect.anchoredPosition = new Vector2(-6f, -6f);
            ApplyIcon(compass.GetComponent<Image>(), manifest?.compassArrow, new Color(0.55f, 0.95f, 1f, 0.95f));

            var tracker = questRoot.GetComponent<QuestTrackerUI>();
            if (tracker != null)
            {
                SetPrivate(tracker, "questTitleText", title);
                SetPrivate(tracker, "questStepText", step);
                SetPrivate(tracker, "compassArrow", compassRect);
            }
        }

        static void LayoutTopLeft(Transform t, Vector2 pos, Vector2 size)
        {
            var r = t.GetComponent<RectTransform>();
            r.anchorMin = r.anchorMax = new Vector2(0f, 1f);
            r.pivot = new Vector2(0f, 1f);
            r.anchoredPosition = pos;
            r.sizeDelta = size;
        }

        static void RemoveLegacyImage(Transform root)
        {
            var legacyImage = root.GetComponent<Image>();
            if (legacyImage == null)
                return;

            if (Application.isPlaying)
                Object.Destroy(legacyImage);
            else
                Object.DestroyImmediate(legacyImage);
        }

        static void ApplyMinimapRing(Image ring, Sprite ringSprite)
        {
            if (ring == null)
                return;

            if (ringSprite != null && HUDPrimitiveStyles.IsWhitelistedMinimapRingSprite(ringSprite))
            {
                ring.sprite = ringSprite;
                ring.type = Image.Type.Simple;
                ring.preserveAspect = true;
                ring.color = Color.white;
                return;
            }

            ring.sprite = HUDPrimitiveStyles.GetMinimapRingSprite();
            ring.type = Image.Type.Simple;
            ring.preserveAspect = true;
            ring.color = Color.white;
        }

        static void EnsureMapDot(Transform parent, string name, Sprite sprite, Color fallback)
        {
            var dot = EnsureChildImage(parent, name);
            var rect = dot.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = sprite != null ? new Vector2(12f, 12f) : new Vector2(8f, 8f);
            rect.anchoredPosition = Vector2.zero;
            ApplyIcon(dot.GetComponent<Image>(), sprite, fallback);
        }

        static void ApplyIcon(Image img, Sprite sprite, Color fallback)
        {
            if (img == null)
                return;

            if (sprite != null && HUDPrimitiveStyles.IsWhitelistedIconSprite(sprite))
            {
                img.sprite = sprite;
                img.preserveAspect = true;
                img.color = Color.white;
            }
            else
            {
                img.sprite = null;
                img.color = new Color(fallback.r, fallback.g, fallback.b, 0f);
            }
        }

        static Transform EnsureUiRoot(Transform parent, string name)
        {
            var child = parent.Find(name);
            if (child == null)
            {
                var go = new GameObject(name, typeof(RectTransform));
                go.transform.SetParent(parent, false);
                return go.transform;
            }

            if (child.GetComponent<RectTransform>() == null)
                child.gameObject.AddComponent<RectTransform>();
            return child;
        }

        static void RemoveImageIfPresent(Transform target)
        {
            var image = target.GetComponent<Image>();
            if (image == null)
                return;

            if (Application.isPlaying)
                Object.Destroy(image);
            else
                Object.DestroyImmediate(image);
        }

        static Transform EnsureChildRect(Transform parent, string name)
        {
            var child = parent.Find(name);
            if (child != null)
                return child;

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            return go.transform;
        }

        static Transform EnsureChildImage(Transform parent, string name)
        {
            var child = parent.Find(name);
            if (child != null)
            {
                if (child.GetComponent<Image>() == null)
                    child.gameObject.AddComponent<Image>();
                return child;
            }

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            go.AddComponent<Image>();
            return go.transform;
        }

        static TMP_Text EnsureChildText(Transform parent, string name, string defaultText, Vector2 anchoredPos, float fontSize)
        {
            var child = parent.Find(name);
            TMP_Text text;
            if (child != null)
            {
                text = child.GetComponent<TMP_Text>();
                if (text == null)
                    text = child.gameObject.AddComponent<TextMeshProUGUI>();
            }
            else
            {
                var go = new GameObject(name);
                go.transform.SetParent(parent, false);
                go.AddComponent<RectTransform>();
                text = go.AddComponent<TextMeshProUGUI>();
            }

            var textRect = text.rectTransform;
            textRect.anchorMin = textRect.anchorMax = new Vector2(0f, 1f);
            textRect.pivot = new Vector2(0f, 1f);
            textRect.sizeDelta = new Vector2(360f, 28f);
            textRect.anchoredPosition = anchoredPos;
            text.fontSize = fontSize;
            text.text = defaultText;
            text.color = Color.white;
            return text;
        }

        static void SetPrivate(object target, string fieldName, object value)
        {
            target.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.SetValue(target, value);
        }
    }
}
