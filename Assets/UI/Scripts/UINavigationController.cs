using BES.Gameplay;
using UnityEngine;

namespace BES.UI
{
    public class UINavigationController : MonoBehaviour
    {
        [Header("Layer 0 — HUD (always visible in gameplay)")]
        [SerializeField] HUDController hud;
        [SerializeField] MiniMapUI miniMap;
        [SerializeField] InteractPromptUI interactPrompt;
        [SerializeField] HudNavBarUI hudNavBar;

        [Header("Layer 1 — Overlay menus")]
        [SerializeField] InventoryUI inventoryUI;
        [SerializeField] CharacterProfileUI characterProfileUI;
        [SerializeField] GameMapUI gameMapUI;

        [Header("Layer 2 — Full-screen meta")]
        [SerializeField] EventUI eventUI;
        [SerializeField] WishUI wishUI;
        [SerializeField] BattlePassUI battlePassUI;
        [SerializeField] SettingsUI settingsUI;

        [Header("Layer 3 — Modals")]
        [SerializeField] DialogueUI dialogueUI;
        [SerializeField] LoadingScreenUI loadingScreenUI;

        [Header("Mission")]
        [SerializeField] QuestLogUI questLogUI;

        PlayerInputReader input;

        public bool IsMenuOpen =>
            (inventoryUI != null && inventoryUI.IsOpen) ||
            (characterProfileUI != null && characterProfileUI.IsOpen) ||
            (gameMapUI != null && gameMapUI.IsOpen) ||
            (eventUI != null && eventUI.IsOpen) ||
            (wishUI != null && wishUI.IsOpen) ||
            (battlePassUI != null && battlePassUI.IsOpen) ||
            (settingsUI != null && settingsUI.IsOpen) ||
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
            interactPrompt ??= GetComponentInChildren<InteractPromptUI>(true);
            hudNavBar ??= GetComponentInChildren<HudNavBarUI>(true);
            inventoryUI ??= GetComponentInChildren<InventoryUI>(true);
            characterProfileUI ??= GetComponentInChildren<CharacterProfileUI>(true);
            gameMapUI ??= GetComponentInChildren<GameMapUI>(true);
            eventUI ??= GetComponentInChildren<EventUI>(true);
            wishUI ??= GetComponentInChildren<WishUI>(true);
            battlePassUI ??= GetComponentInChildren<BattlePassUI>(true);
            settingsUI ??= GetComponentInChildren<SettingsUI>(true);
            dialogueUI ??= GetComponentInChildren<DialogueUI>(true);
            loadingScreenUI ??= GetComponentInChildren<LoadingScreenUI>(true);
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
                (UnityEngine.InputSystem.Keyboard.current.jKey.wasPressedThisFrame ||
                 UnityEngine.InputSystem.Keyboard.current.vKey.wasPressedThisFrame))
                ToggleQuestLog();

            if (input.CloseMenuPressed)
            {
                CloseTopLayer();
                return;
            }

            if (input.InventoryPressed) ToggleInventory();
            if (input.CharacterMenuPressed) ToggleCharacter();
            if (input.MapTogglePressed) ToggleWorldMap();
            if (input.WishMenuPressed) ToggleWish();
            if (input.EventMenuPressed) ToggleEvent();
        }

        public void CloseTopLayer()
        {
            if (wishUI != null && wishUI.IsOpen) { wishUI.Hide(); return; }
            if (battlePassUI != null && battlePassUI.IsOpen) { battlePassUI.Hide(); return; }
            if (settingsUI != null && settingsUI.IsOpen) { settingsUI.Hide(); return; }
            if (eventUI != null && eventUI.IsOpen) { eventUI.Hide(); return; }
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
        }

        void CloseMetaScreens()
        {
            eventUI?.Hide();
            wishUI?.Hide();
            battlePassUI?.Hide();
            settingsUI?.Hide();
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

        public void ToggleBattlePass()
        {
            if (battlePassUI == null) return;
            var opening = !battlePassUI.IsOpen;
            if (opening) { CloseOverlayMenus(); CloseMetaScreens(); battlePassUI.Show(); }
            else battlePassUI.Hide();
        }

        public void ToggleSettings()
        {
            if (settingsUI == null) return;
            var opening = !settingsUI.IsOpen;
            if (opening) { CloseOverlayMenus(); CloseMetaScreens(); settingsUI.Show(); }
            else settingsUI.Hide();
        }

        public void ToggleQuestLog()
        {
            questLogUI ??= GetComponentInChildren<QuestLogUI>(true);
            questLogUI?.Toggle();
        }
    }
}
