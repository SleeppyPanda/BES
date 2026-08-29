using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace BES.UI.Menu
{
    [Serializable]
    public class MenuScreenBinding
    {
        public MenuScreenId id;
        public GameObject panel;
        public Button defaultFocus;
        public UnityEvent onOpened;
        public UnityEvent onClosed;
    }

    public class MenuNavigator : MonoBehaviour
    {
        [SerializeField] MenuScreenId initialScreen = MenuScreenId.Home;
        [SerializeField] List<MenuScreenBinding> screens = new();
        [SerializeField] bool allowEscapeBack = true;
        readonly Stack<MenuScreenId> history = new();
        MenuScreenId current;
        public MenuScreenId Current => current;
        public event Action<MenuScreenId> ScreenChanged;

        void Awake()
        {
            RepairMissingScreenPanels();
            foreach (var screen in screens) screen.panel?.SetActive(false);
            OpenInternal(initialScreen, false);
        }

        void Update()
        {
            if (allowEscapeBack && UnityEngine.InputSystem.Keyboard.current?.escapeKey.wasPressedThisFrame == true) Back();
        }

        public void Open(int screenId) => Open((MenuScreenId)screenId);
        public void Open(MenuScreenId id) => OpenInternal(id, true);
        public void OpenAsRoot(MenuScreenId id)
        {
            history.Clear();
            OpenInternal(id, false);
        }

        void OpenInternal(MenuScreenId id, bool rememberCurrent)
        {
            var next = screens.Find(x => x.id == id);
            if (next == null || next.panel == null) return;
            var previous = screens.Find(x => x.id == current);
            if (previous?.panel != null && previous.panel.activeSelf)
            {
                if (rememberCurrent && current != id) history.Push(current);
                previous.panel.SetActive(false);
                previous.onClosed?.Invoke();
            }
            current = id;
            next.panel.SetActive(true);
            next.onOpened?.Invoke();
            next.defaultFocus?.Select();
            ScreenChanged?.Invoke(id);
        }

        public void Back()
        {
            if (history.Count > 0) OpenInternal(history.Pop(), false);
            else if (current != initialScreen) OpenInternal(initialScreen, false);
        }

        void RepairMissingScreenPanels()
        {
            foreach (var screen in screens)
            {
                if (screen == null || screen.panel != null) continue;
                var found = FindPanelForScreen(screen.id);
                if (found != null) screen.panel = found;
            }
        }

        GameObject FindPanelForScreen(MenuScreenId id)
        {
            var names = CandidatePanelNames(id);
            if (names == null || names.Length == 0) return null;
            var transforms = GetComponentsInChildren<Transform>(true);
            foreach (var candidate in names)
            {
                foreach (var item in transforms)
                {
                    if (item == null) continue;
                    if (string.Equals(item.name, candidate, StringComparison.OrdinalIgnoreCase))
                        return item.gameObject;
                }
            }
            return null;
        }

        static string[] CandidatePanelNames(MenuScreenId id)
        {
            return id switch
            {
                MenuScreenId.Battle => new[] { "BattlePanel", "battlePanel" },
                MenuScreenId.Home => new[] { "HomePanel", "Home" },
                MenuScreenId.StoryParty => new[] { "StoryPartyPanel", "StoryModePanel", "StoryPanel" },
                MenuScreenId.PlayParty => new[] { "PlayPartyPanel", "PlayTeamSetupPanel", "PrebattlePanel" },
                MenuScreenId.ResourceStages => new[] { "ResourceStagesPanel", "ResourcePanel" },
                MenuScreenId.SanctumRelics => new[] { "SanctumRelicsPanel", "SanctumPanel" },
                MenuScreenId.WeaponBreakthrough => new[] { "WeaponBreakthroughPanel", "WeaponPanel" },
                MenuScreenId.Dialogue => new[] { "DialoguePanel" },
                MenuScreenId.Management => new[] { "ManagementPanel" },
                MenuScreenId.CashShop => new[] { "CashShopPanel" },
                MenuScreenId.BattlePass => new[] { "BattlePassPanel" },
                _ => Array.Empty<string>()
            };
        }
    }
}
