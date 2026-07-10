using BES.Gameplay;
using UnityEngine;

namespace BES.UI
{
    public class UINavigationController : MonoBehaviour
    {
        [Header("Layer 0 — HUD (always visible in gameplay)")]
        [SerializeField] HUDController hud;
        [SerializeField] MiniMapUI miniMap;
        [SerializeField] QuestTrackerUI questTracker;
        [SerializeField] InteractPromptUI interactPrompt;
        [SerializeField] HudNavBarUI hudNavBar;

        [Header("Layer 1 — Overlay menus")]
        [SerializeField] InventoryUI inventoryUI;
        [SerializeField] CharacterProfileUI characterProfileUI;
        [SerializeField] GameMapUI gameMapUI;
        [SerializeField] WeaponScreenUI weaponScreenUI;
        [SerializeField] ArtifactsUI artifactsUI;

        [Header("Layer 2 — Full-screen meta")]
        [SerializeField] TeamSetupUI teamSetupUI;
        [SerializeField] EventUI eventUI;
        [SerializeField] WishUI wishUI;

        [Header("Layer 3 — Modals")]
        [SerializeField] DialogueUI dialogueUI;
        [SerializeField] LoadingScreenUI loadingScreenUI;
        [SerializeField] PlayerProfileUI playerProfileUI;

        [Header("Weapon flow")]
        [SerializeField] WeaponEnhanceUI weaponEnhanceUI;
        [SerializeField] WeaponRankUpUI weaponRankUpUI;
        [SerializeField] WeaponRefineUI weaponRefineUI;
        [SerializeField] QuestLogUI questLogUI;

        PlayerInputReader input;

        public bool IsMenuOpen =>
            (inventoryUI != null && inventoryUI.IsOpen) ||
            (characterProfileUI != null && characterProfileUI.IsOpen) ||
            (gameMapUI != null && gameMapUI.IsOpen) ||
            (weaponScreenUI != null && weaponScreenUI.IsOpen) ||
            (artifactsUI != null && artifactsUI.IsOpen) ||
            (teamSetupUI != null && teamSetupUI.IsOpen) ||
            (eventUI != null && eventUI.IsOpen) ||
            (wishUI != null && wishUI.IsOpen) ||
            (playerProfileUI != null && playerProfileUI.IsOpen) ||
            (weaponEnhanceUI != null && weaponEnhanceUI.IsOpen) ||
            (weaponRankUpUI != null && weaponRankUpUI.IsOpen) ||
            (weaponRefineUI != null && weaponRefineUI.IsOpen) ||
            (questLogUI != null && questLogUI.IsOpen) ||
            (dialogueUI != null && dialogueUI.IsStoryOpen);

        public bool IsBlockingGameplay => IsMenuOpen;

        void Awake()
        {
            if (GetComponent<UIScreenBackgroundBootstrap>() == null)
                gameObject.AddComponent<UIScreenBackgroundBootstrap>();

            input = FindAnyObjectByType<PlayerInputReader>();
            AutoFindReferences();
        }

        void AutoFindReferences()
        {
            hud ??= GetComponentInChildren<HUDController>(true);
            miniMap ??= GetComponentInChildren<MiniMapUI>(true);
            questTracker ??= GetComponentInChildren<QuestTrackerUI>(true);
            interactPrompt ??= GetComponentInChildren<InteractPromptUI>(true);
            hudNavBar ??= GetComponentInChildren<HudNavBarUI>(true);
            inventoryUI ??= GetComponentInChildren<InventoryUI>(true);
            characterProfileUI ??= GetComponentInChildren<CharacterProfileUI>(true);
            gameMapUI ??= GetComponentInChildren<GameMapUI>(true);
            weaponScreenUI ??= GetComponentInChildren<WeaponScreenUI>(true);
            artifactsUI ??= GetComponentInChildren<ArtifactsUI>(true);
            teamSetupUI ??= GetComponentInChildren<TeamSetupUI>(true);
            eventUI ??= GetComponentInChildren<EventUI>(true);
            wishUI ??= GetComponentInChildren<WishUI>(true);
            dialogueUI ??= GetComponentInChildren<DialogueUI>(true);
            loadingScreenUI ??= GetComponentInChildren<LoadingScreenUI>(true);
            playerProfileUI ??= GetComponentInChildren<PlayerProfileUI>(true);
            weaponEnhanceUI ??= GetComponentInChildren<WeaponEnhanceUI>(true);
            weaponRankUpUI ??= GetComponentInChildren<WeaponRankUpUI>(true);
            weaponRefineUI ??= GetComponentInChildren<WeaponRefineUI>(true);
            questLogUI ??= GetComponentInChildren<QuestLogUI>(true);
        }

        void Update()
        {
            if (input == null)
            {
                input = FindAnyObjectByType<PlayerInputReader>();
                if (input == null)
                    return;
            }

            if (UnityEngine.InputSystem.Keyboard.current != null &&
                UnityEngine.InputSystem.Keyboard.current.jKey.wasPressedThisFrame)
                ToggleQuestLog();

            if (input.CloseMenuPressed)
            {
                CloseTopLayer();
                return;
            }

            if (input.InventoryPressed) ToggleInventory();
            if (input.CharacterMenuPressed) ToggleCharacter();
            if (input.MapTogglePressed) ToggleWorldMap();
            if (input.WeaponMenuPressed) ToggleWeapon();
            if (input.WishMenuPressed) ToggleWish();
            if (input.TeamMenuPressed) ToggleTeam();
            if (input.EventMenuPressed) ToggleEvent();
            if (input.ArtifactsMenuPressed) ToggleArtifacts();
        }

        public void CloseTopLayer()
        {
            if (weaponRefineUI != null && weaponRefineUI.IsOpen) { weaponRefineUI.Hide(); return; }
            if (weaponRankUpUI != null && weaponRankUpUI.IsOpen) { weaponRankUpUI.Hide(); return; }
            if (weaponEnhanceUI != null && weaponEnhanceUI.IsOpen) { weaponEnhanceUI.Hide(); return; }
            if (weaponScreenUI != null && weaponScreenUI.IsOpen) { weaponScreenUI.Hide(); return; }
            if (artifactsUI != null && artifactsUI.IsOpen) { artifactsUI.Hide(); return; }
            if (wishUI != null && wishUI.IsOpen) { wishUI.Hide(); return; }
            if (teamSetupUI != null && teamSetupUI.IsOpen) { teamSetupUI.Hide(); return; }
            if (eventUI != null && eventUI.IsOpen) { eventUI.Hide(); return; }
            if (playerProfileUI != null && playerProfileUI.IsOpen) { playerProfileUI.Hide(); return; }
            if (inventoryUI != null && inventoryUI.IsOpen) { inventoryUI.Close(); return; }
            if (characterProfileUI != null && characterProfileUI.IsOpen) { characterProfileUI.Close(); return; }
            if (gameMapUI != null && gameMapUI.IsOpen) { gameMapUI.Close(); return; }
            if (questLogUI != null && questLogUI.IsOpen) { questLogUI.Close(); return; }
        }

        void CloseOverlayMenus()
        {
            inventoryUI?.Close();
            characterProfileUI?.Close();
            gameMapUI?.Close();
            weaponScreenUI?.Hide();
            artifactsUI?.Hide();
        }

        void CloseMetaScreens()
        {
            teamSetupUI?.Hide();
            eventUI?.Hide();
            wishUI?.Hide();
        }

        public void ToggleInventory()
        {
            if (inventoryUI == null) return;
            var opening = !inventoryUI.IsOpen;
            if (opening) { CloseOverlayMenus(); CloseMetaScreens(); }
            inventoryUI.Toggle();
        }

        public void ToggleCharacter()
        {
            if (characterProfileUI == null) return;
            var opening = !characterProfileUI.IsOpen;
            if (opening) { CloseOverlayMenus(); CloseMetaScreens(); }
            characterProfileUI.Toggle();
        }

        public void ToggleWorldMap()
        {
            if (gameMapUI == null) return;
            var opening = !gameMapUI.IsOpen;
            if (opening) { CloseOverlayMenus(); CloseMetaScreens(); }
            gameMapUI.Toggle();
        }

        public void ToggleWeapon()
        {
            if (weaponScreenUI == null) return;
            var opening = !weaponScreenUI.IsOpen;
            if (opening) { CloseOverlayMenus(); CloseMetaScreens(); weaponScreenUI.Show(); }
            else weaponScreenUI.Hide();
        }

        public void ToggleArtifacts()
        {
            if (artifactsUI == null) return;
            var opening = !artifactsUI.IsOpen;
            if (opening) { CloseOverlayMenus(); CloseMetaScreens(); artifactsUI.Show(); }
            else artifactsUI.Hide();
        }

        public void ToggleTeam()
        {
            if (teamSetupUI == null) return;
            var opening = !teamSetupUI.IsOpen;
            if (opening) { CloseOverlayMenus(); CloseMetaScreens(); teamSetupUI.Show(); }
            else teamSetupUI.Hide();
        }

        public void ToggleEvent()
        {
            if (eventUI == null) return;
            var opening = !eventUI.IsOpen;
            if (opening) { CloseOverlayMenus(); CloseMetaScreens(); eventUI.Show(); }
            else eventUI.Hide();
        }

        public void ToggleWish()
        {
            if (wishUI == null) return;
            var opening = !wishUI.IsOpen;
            if (opening) { CloseOverlayMenus(); CloseMetaScreens(); wishUI.Show(); }
            else wishUI.Hide();
        }

        public void ShowPlayerProfile()
        {
            CloseMetaScreens();
            playerProfileUI?.Show();
        }

        public void ToggleQuestLog()
        {
            questLogUI ??= GetComponentInChildren<QuestLogUI>(true);
            questLogUI?.Toggle();
        }
    }
}
