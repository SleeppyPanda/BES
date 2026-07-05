using UnityEngine;

namespace BES.UI
{
    /// <summary>
    /// Gắn mockup cho overlay/meta/modal panels. Không dùng full-screen Main play trên gameplay HUD.
    /// </summary>
    public class UIScreenBackgroundBootstrap : MonoBehaviour
    {
        void Awake()
        {
            RemoveGameplayBackdrop();
            Bind("OverlayLayer/InventoryUI", UIScreenBackgroundId.Inventory);
            Bind("OverlayLayer/CharacterProfileUI", UIScreenBackgroundId.CharacterProfile);
            Bind("OverlayLayer/CharacterProfileUI/CharacterPreview", UIScreenBackgroundId.CharacterPreview, false);
            Bind("OverlayLayer/GameMapUI", UIScreenBackgroundId.WorldMap);
            Bind("OverlayLayer/WeaponScreenUI", UIScreenBackgroundId.Weapon);
            Bind("OverlayLayer/ArtifactsUI", UIScreenBackgroundId.Artifacts);
            Bind("MetaLayer/TeamSetupUI", UIScreenBackgroundId.TeamSetup);
            Bind("MetaLayer/EventUI", UIScreenBackgroundId.EventCheckIn);
            Bind("MetaLayer/WishUI", UIScreenBackgroundId.Wish);
            Bind("ModalLayer/DialogueUI/DialoguePanel", UIScreenBackgroundId.Dialogue);
            Bind("ModalLayer/LoadingScreenUI", UIScreenBackgroundId.Loading);
            Bind("ModalLayer/WeaponEnhanceUI", UIScreenBackgroundId.WeaponEnhance);
            Bind("ModalLayer/WeaponRankUpUI", UIScreenBackgroundId.WeaponRankUp);
            Bind("ModalLayer/WeaponRefineUI", UIScreenBackgroundId.WeaponRefine);
        }

        void RemoveGameplayBackdrop()
        {
            var backdrop = transform.Find("HudBackdrop");
            if (backdrop != null)
                Destroy(backdrop.gameObject);
        }

        void Bind(string path, UIScreenBackgroundId screenId, bool raycastTarget = true)
        {
            var node = transform.Find(path);
            if (node == null)
                return;

            var binder = node.GetComponent<UIScreenBackground>();
            if (binder == null)
                binder = node.gameObject.AddComponent<UIScreenBackground>();

            binder.Configure(screenId, raycastTarget);
            binder.Apply();
        }
    }
}
