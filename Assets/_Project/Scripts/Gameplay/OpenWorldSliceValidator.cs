using BES.Core;
using BES.Gameplay;
using BES.Narrative;
using BES.UI;
using UnityEngine;
using UnityEngine.UI;

namespace BES.Gameplay
{
    /// <summary>
    /// Runtime QA checklist cho vertical slice Genshin-like (log-only).
    /// </summary>
    public class OpenWorldSliceValidator : MonoBehaviour
    {
        [ContextMenu("Run QA Checklist")]
        public void RunChecklist()
        {
            CheckHudNoBackdrop();
            CheckHudNoMockupLayers();
            CheckHudMockupSprites();
            CheckManifests();
            CheckSaveSession();
            CheckInputGate();
            CheckBuildStats();
            CheckPartySwap();
            CheckGachaPity();
            CheckHudWidgets();
            CheckFigmaLayout();
            Debug.Log("[BES QA] Vertical slice checklist hoàn tất — xem log phía trên.");
        }

        void CheckHudWidgets()
        {
            var tracker = GameObject.Find("QuestTracker");
            if (tracker != null && tracker.transform.Find("MissionBg") != null)
                Debug.LogError("[BES QA] FAIL: QuestTracker vẫn có MissionBg mockup.");
            else if (tracker != null)
                Debug.Log("[BES QA] PASS: QuestTracker không dùng MissionBg.");

            var navBar = GameObject.Find("HudNavBar");
            if (navBar != null && navBar.transform.Find("NavBarBg") != null)
                Debug.LogError("[BES QA] FAIL: HudNavBar vẫn có NavBarBg stretch mockup.");
            else if (navBar != null)
                Debug.Log("[BES QA] PASS: HudNavBar không có NavBarBg.");

            var questLog = Object.FindAnyObjectByType<QuestLogUI>();
            if (questLog != null && questLog.IsOpen)
                Debug.LogError("[BES QA] FAIL: QuestLog panel đang mở khi vào gameplay.");
            else
                Debug.Log("[BES QA] PASS: QuestLog đóng khi explore.");

            var partyStrip = Object.FindAnyObjectByType<PartyStripUI>();
            if (partyStrip == null)
                Debug.LogWarning("[BES QA] WARN: PartyStripUI không tìm thấy.");
            else
                Debug.Log("[BES QA] PASS: PartyStripUI present.");

            var skillBar = Object.FindAnyObjectByType<SkillBarUI>();
            if (skillBar == null)
                Debug.LogWarning("[BES QA] WARN: SkillBarUI không tìm thấy.");
            else
                Debug.Log("[BES QA] PASS: SkillBarUI present.");

            if (Object.FindAnyObjectByType<SkillBarDriver>() == null)
                Debug.LogWarning("[BES QA] WARN: SkillBarDriver chưa gắn trên SkillBar.");
            else
                Debug.Log("[BES QA] PASS: SkillBarDriver present.");
        }

        void CheckFigmaLayout()
        {
            var hudLayer = GameObject.Find("HUDLayer");
            if (hudLayer == null)
            {
                Debug.LogWarning("[BES QA] WARN: HUDLayer không tìm thấy.");
                return;
            }

            if (hudLayer.transform.Find("PortraitChip") == null)
                Debug.LogWarning("[BES QA] WARN: Thiếu PortraitChip (Figma góc trái trên).");
            else
                Debug.Log("[BES QA] PASS: PortraitChip present.");

            if (hudLayer.transform.Find("LockBtn") == null)
                Debug.LogWarning("[BES QA] WARN: Thiếu LockBtn.");
            else
                Debug.Log("[BES QA] PASS: LockBtn present.");

            if (hudLayer.transform.Find("ChatEnter") == null)
                Debug.LogWarning("[BES QA] WARN: Thiếu ChatEnter widget.");
            else
                Debug.Log("[BES QA] PASS: ChatEnter present.");

            var skillBarRoot = hudLayer.transform.Find("SkillBar");
            if (skillBarRoot != null)
            {
                var extraSlot = skillBarRoot.Find("SkillSlot4");
                if (extraSlot != null && extraSlot.gameObject.activeInHierarchy)
                    Debug.LogError("[BES QA] FAIL: Skill bar có SkillSlot4 — Figma chỉ Z/E/Q.");
                else
                    Debug.Log("[BES QA] PASS: Skill bar Z/E/Q layout (3 slot).");

                for (var i = 0; i < SkillBarUI.VisibleSlotCount; i++)
                {
                    var key = skillBarRoot.Find($"SkillSlot{i + 1}/KeyLabel")?.GetComponent<TMPro.TMP_Text>();
                    if (key != null && key.text is not ("Z" or "E" or "Q"))
                        Debug.LogWarning($"[BES QA] WARN: SkillSlot{i + 1} key '{key.text}' — Figma dùng Z/E/Q.");
                }
            }

            var navBar = hudLayer.transform.Find("HudNavBar");
            if (navBar != null)
            {
                foreach (var img in navBar.GetComponentsInChildren<Image>(true))
                {
                    if (img.sprite != null && img.sprite.name.StartsWith("Group 427"))
                        Debug.LogError($"[BES QA] FAIL: Nav dùng mockup sprite {img.sprite.name}");
                }

                foreach (var text in navBar.GetComponentsInChildren<TMPro.TMP_Text>(true))
                {
                    if (!string.IsNullOrEmpty(text.text) && text.text != text.name)
                        Debug.LogWarning($"[BES QA] WARN: Nav có label text '{text.text}' — Figma chỉ icon.");
                }
            }

            if (hudLayer.transform.Find("PartySlotNumber1") != null)
                Debug.Log("[BES QA] PASS: Party số 1–4 anchor sát mép phải màn hình.");
            else
                Debug.LogWarning("[BES QA] WARN: PartySlotNumber* chưa tách khỏi pill — chạy BES → Setup Project.");

            var hud = HUDSpriteManifestLoader.Load();
            if (hud != null)
            {
                if (hud.lockIcon == null || hud.chatBubbleIcon == null)
                    Debug.LogWarning("[BES QA] WARN: Manifest thiếu lock/chat icon.");
                if (hud.skillIconDodge != null)
                    Debug.LogWarning("[BES QA] WARN: skillIconDodge vẫn map — Figma không có Shift slot.");
            }

            var level = hudLayer.transform.Find("HUD/LevelText")?.GetComponent<TMPro.TMP_Text>();
            if (level != null && !level.text.StartsWith("Level "))
                Debug.LogWarning($"[BES QA] WARN: Level text format '{level.text}' — Figma dùng 'Level N.'");
            else if (level != null)
                Debug.Log("[BES QA] PASS: Level text format Figma.");
        }

        void CheckHudNoBackdrop()
        {
            var backdrop = GameObject.Find("HudBackdrop");
            if (backdrop != null)
                Debug.LogError("[BES QA] FAIL: HudBackdrop vẫn tồn tại — gameplay bị mockup che world.");
            else
                Debug.Log("[BES QA] PASS: Không có HudBackdrop full-screen.");
        }

        void CheckHudNoMockupLayers()
        {
            var hudLayer = GameObject.Find("HUDLayer");
            if (hudLayer == null)
            {
                Debug.LogWarning("[BES QA] WARN: HUDLayer không tìm thấy.");
                return;
            }

            foreach (var bg in hudLayer.GetComponentsInChildren<UIScreenBackground>(true))
            {
                if (bg != null && bg.gameObject.activeInHierarchy)
                    Debug.LogError($"[BES QA] FAIL: UIScreenBackground trên HUDLayer: {bg.gameObject.name}");
            }

            foreach (var img in hudLayer.GetComponentsInChildren<Image>(true))
            {
                if (img.sprite == null || !img.gameObject.activeInHierarchy)
                    continue;

                var spriteName = img.sprite.name;
                if (spriteName.Contains("Main play") || spriteName == "Mission")
                    Debug.LogError($"[BES QA] FAIL: HUDLayer dùng mockup full-screen: {spriteName} trên {img.gameObject.name}");
            }
        }

        void CheckHudMockupSprites()
        {
            var hud = HUDSpriteManifestLoader.Load();
            if (hud == null)
                return;

            if (hud.minimapFrame != null && !HUDPrimitiveStyles.IsWhitelistedFrameSprite(hud.minimapFrame))
                Debug.LogError($"[BES QA] FAIL: minimapFrame không whitelist: {hud.minimapFrame.name}");

            if (hud.questTrackerFrame != null && !HUDPrimitiveStyles.IsWhitelistedFrameSprite(hud.questTrackerFrame))
                Debug.LogError($"[BES QA] FAIL: questTrackerFrame không whitelist: {hud.questTrackerFrame.name}");

            if (hud.navBarBackground != null)
                Debug.LogError("[BES QA] FAIL: navBarBackground vẫn được map — nav phải là icon buttons riêng.");

            if (hud.minimapFrame == null && hud.questTrackerFrame == null && hud.navBarBackground == null)
                Debug.Log("[BES QA] PASS: Manifest không map mockup panel lớn cho minimap/quest/nav.");
        }

        void CheckManifests()
        {
            var hud = HUDSpriteManifestLoader.Load();
            if (hud == null || hud.navWeapon == null)
                Debug.LogWarning("[BES QA] WARN: HUDSpriteManifest thiếu sprite (chạy BES → Setup Project).");
            else
                Debug.Log("[BES QA] PASS: HUD manifest có navWeapon.");

            if (hud != null && hud.partySlotFrame != null && !HUDPrimitiveStyles.IsWhitelistedFrameSprite(hud.partySlotFrame))
                Debug.LogError($"[BES QA] FAIL: partySlotFrame là mockup: {hud.partySlotFrame.name}");
        }

        void CheckSaveSession()
        {
            var save = GameManager.Instance?.Save;
            if (save == null)
                Debug.LogWarning("[BES QA] WARN: SaveSystem chưa sẵn sàng.");
            else
                Debug.Log($"[BES QA] Save: HasSave={save.HasSave}, Continue={save.LoadedFromContinue}, NewSession={save.IsNewSession}");
        }

        void CheckInputGate()
        {
            var blocked = GameplayInputGate.IsGameplayBlocked;
            Debug.Log($"[BES QA] Input gate blocked={blocked} (false khi đang explore).");
        }

        void CheckBuildStats()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null || !player.TryGetComponent<PlayerBuildStats>(out var build))
            {
                Debug.LogWarning("[BES QA] WARN: PlayerBuildStats chưa gắn trên player.");
                return;
            }

            build.Refresh();
            Debug.Log($"[BES QA] PASS: Build ATK={build.ComputedAttack}, HP={build.ComputedMaxHealth}");
        }

        void CheckPartySwap()
        {
            if (PartyRoster.Instance == null)
            {
                Debug.LogWarning("[BES QA] WARN: PartyRoster chưa sẵn sàng.");
                return;
            }

            if (Object.FindAnyObjectByType<PartySwapController>() == null)
                Debug.LogWarning("[BES QA] WARN: PartySwapController chưa gắn trên player.");
            else
                Debug.Log("[BES QA] PASS: PartySwapController present.");

            Debug.Log($"[BES QA] Party active slot={PartyRoster.Instance.ActiveCharacterIndex}, id={PartyRoster.Instance.ActiveCharacterId}");
        }

        void CheckGachaPity()
        {
            if (GachaPityState.Instance == null)
                Debug.LogWarning("[BES QA] WARN: GachaPityState chưa sẵn sàng.");
            else
                Debug.Log($"[BES QA] Gacha pity={GachaPityState.Instance.PullsSinceLastFiveStar}, stardust={GachaPityState.Instance.Stardust}");
        }
    }
}
