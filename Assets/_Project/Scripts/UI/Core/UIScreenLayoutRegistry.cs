using UnityEngine;

namespace BES.UI
{
    public enum UIScreenId
    {
        MainMenu,
        GameplayHud,
        Inventory,
        Character,
        WorldMap,
        Weapon,
        Artifacts,
        Team,
        Event,
        Wish,
        Dialogue,
        Loading,
        PlayerProfile,
        Settings,
        ServerPicker
    }

    /// <summary>
    /// Runtime layout fallback for older prefabs — mirrors editor anchor presets.
    /// </summary>
    public static class UIScreenLayoutRegistry
    {
        public static void Apply(Transform root, UIScreenId screenId)
        {
            if (root == null)
                return;

            switch (screenId)
            {
                case UIScreenId.MainMenu:
                    MainMenuLayout.Apply(root);
                    break;
                case UIScreenId.GameplayHud:
                    var layout = root.GetComponentInChildren<GameplayHudLayout>(true);
                    if (layout != null && layout.ApplyRuntimeLayout)
                        layout.Reapply();
                    break;
            }
        }
    }
}
