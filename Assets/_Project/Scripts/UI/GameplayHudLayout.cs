using UnityEngine;
using UnityEngine.UI;

namespace BES.UI
{
    /// <summary>
    /// Applies HUD sprite manifest and anchors at runtime for older prefabs.
    /// </summary>
    public class GameplayHudLayout : MonoBehaviour
    {
        [SerializeField] HUDSpriteManifest manifest;
        [SerializeField] bool applyRuntimeLayout;

        public bool ApplyRuntimeLayout => applyRuntimeLayout;

        void Awake()
        {
            manifest ??= HUDSpriteManifestLoader.Load();
            if (manifest == null || !applyRuntimeLayout)
                return;

            Reapply();
        }

        public void Reapply()
        {
            if (manifest == null)
                return;

            RemoveLegacyMockupLayers();
            TopLeftHudWidgets.ApplyMiniMap(transform.Find("HUDLayer/MiniMap"), manifest);
            ApplyNavBar();
            ApplyBars();
            ApplyPartyStripFromComponent();
            ApplySkillBarFromComponent();
            ApplySkillIcons();
            ApplyQuestCompass();
            ApplyInteractPrompt();
        }

        void RemoveLegacyMockupLayers()
        {
            var missionBg = transform.Find("HUDLayer/QuestTracker/MissionBg");
            if (missionBg != null)
                Destroy(missionBg.gameObject);

            var navBg = transform.Find("HUDLayer/HudNavBar/NavBarBg");
            if (navBg != null)
                Destroy(navBg.gameObject);

            var backdrop = transform.Find("HudBackdrop");
            if (backdrop != null)
                Destroy(backdrop.gameObject);

            var legacyChat = transform.Find("HUDLayer/ChatHint");
            if (legacyChat != null)
                Destroy(legacyChat.gameObject);

            var tracker = transform.Find("HUDLayer/QuestTracker");
            if (tracker != null)
            {
                var legacyBg = tracker.GetComponent<UIScreenBackground>();
                if (legacyBg != null)
                    Destroy(legacyBg);
            }
        }

        void ApplySkillIcons()
        {
            var bar = transform.Find("HUDLayer/SkillBar")?.GetComponent<SkillBarUI>();
            if (bar == null)
                return;

            bar.SetSkillIcon(0, manifest.skillIconSkill1);
            bar.SetSkillIcon(1, manifest.skillIconSkill2);
        }

        void ApplyNavBar()
        {
            var navRoot = transform.Find("HUDLayer/HudNavBar");
            if (navRoot == null)
                return;

            ApplyNavIcon(navRoot, "BattlePassBtn", manifest.navBattlePass != null ? manifest.navBattlePass : manifest.navTeam);
            ApplyNavIcon(navRoot, "BagBtn", manifest.navBag != null ? manifest.navBag : manifest.navInventory);
            ApplyNavIcon(navRoot, "PersonalBtn", manifest.navPersonal != null ? manifest.navPersonal : manifest.navCharacter);
            ApplyNavIcon(navRoot, "WishBtn", manifest.navWish);
            ApplyNavIcon(navRoot, "EventBtn", manifest.navEvent);
        }

        void ApplyPartyStripFromComponent()
        {
            var strip = transform.Find("HUDLayer/PartyStrip")?.GetComponent<PartyStripUI>();
            if (strip != null)
                strip.SetFrameSprites(manifest.partySlotFrame);
        }

        void ApplySkillBarFromComponent()
        {
            var bar = transform.Find("HUDLayer/SkillBar")?.GetComponent<SkillBarUI>();
            if (bar != null)
                bar.SetFrameSprites(manifest.skillSlotFrame);
        }

        void ApplyBars()
        {
            var hud = transform.Find("HUDLayer/HUD");
            if (hud == null)
                return;

            ApplyBarSprites(hud, "HealthBar", manifest.hpBarBackground, manifest.hpBarFill, HUDPrimitiveStyles.HpBarBackground, HUDPrimitiveStyles.HpBarFill);
            ApplyBarSprites(hud, "StaminaBar", manifest.staminaBarBackground, manifest.staminaBarFill, HUDPrimitiveStyles.HpBarBackground, HUDPrimitiveStyles.StaminaBarFill);
        }

        void ApplyQuestCompass()
        {
            var arrow = transform.Find("HUDLayer/QuestTracker/CompassArrow");
            if (arrow == null)
                return;

            var img = arrow.GetComponent<Image>();
            if (img != null && manifest.compassArrow != null && HUDPrimitiveStyles.IsWhitelistedIconSprite(manifest.compassArrow))
            {
                img.sprite = manifest.compassArrow;
                img.color = Color.white;
            }
        }

        void ApplyInteractPrompt()
        {
            var panel = transform.Find("HUDLayer/InteractPrompt/PromptPanel");
            if (panel == null)
                return;

            var bg = panel.GetComponent<Image>();
            if (bg == null)
                bg = panel.gameObject.AddComponent<Image>();

            if (manifest.interactPromptFrame != null)
            {
                bg.sprite = manifest.interactPromptFrame;
                bg.color = Color.white;
                bg.type = Image.Type.Sliced;
            }
            else
                HUDPrimitiveStyles.ApplySolidPanel(bg, HUDPrimitiveStyles.QuestPanelBackground);
        }

        static void ApplyNavIcon(Transform root, string name, Sprite sprite)
        {
            var t = root.Find(name);
            if (t == null)
                return;

            var raw = t.GetComponent<RawImage>();
            if (raw != null)
            {
                if (sprite != null && HUDPrimitiveStyles.IsWhitelistedIconSprite(sprite))
                {
                    raw.texture = sprite.texture;
                    raw.color = Color.white;
                }
                else
                    raw.color = HUDPrimitiveStyles.NavIconFallback;
            }

            var img = t.GetComponent<Image>();
            if (img == null && raw == null)
                return;

            if (img != null && sprite != null && HUDPrimitiveStyles.IsWhitelistedIconSprite(sprite))
            {
                img.sprite = sprite;
                img.preserveAspect = true;
                img.color = Color.white;
            }
            else if (img != null)
                HUDPrimitiveStyles.ApplySolidPanel(img, HUDPrimitiveStyles.NavIconFallback);

            var label = t.Find("Label");
            if (label != null)
                label.gameObject.SetActive(false);
        }

        static void ApplyDotSprite(Transform root, string name, Sprite sprite)
        {
            var t = root.Find(name);
            if (t == null || sprite == null)
                return;

            var img = t.GetComponent<Image>();
            if (img != null && HUDPrimitiveStyles.IsWhitelistedIconSprite(sprite))
            {
                img.sprite = sprite;
                img.color = Color.white;
            }
        }

        static void ApplyBarSprites(Transform root, string barName, Sprite bg, Sprite fill, Color fallbackBg, Color fallbackFill)
        {
            var bar = root.Find(barName);
            if (bar == null)
                return;

            var bgT = bar.Find("Background");
            if (bgT != null)
            {
                var img = bgT.GetComponent<Image>();
                if (img != null)
                {
                    if (bg != null && !bg.name.StartsWith("Group 427"))
                    {
                        img.sprite = bg;
                        img.color = Color.white;
                    }
                    else
                        HUDPrimitiveStyles.ApplySolidPanel(img, fallbackBg);
                }
            }

            var fillT = bar.Find("Fill Area/Fill");
            if (fillT != null)
            {
                var img = fillT.GetComponent<Image>();
                if (img != null)
                {
                    if (fill != null && HUDPrimitiveStyles.IsWhitelistedFrameSprite(fill))
                    {
                        img.sprite = fill;
                        img.type = Image.Type.Filled;
                        img.fillMethod = Image.FillMethod.Horizontal;
                        img.color = Color.white;
                    }
                    else
                    {
                        img.sprite = null;
                        img.type = Image.Type.Filled;
                        img.fillMethod = Image.FillMethod.Horizontal;
                        img.color = fallbackFill;
                    }
                }
            }
        }
    }
}
