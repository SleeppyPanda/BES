using UnityEngine;
using UnityEngine.UI;

namespace BES.UI
{
    public class HudNavBarUI : MonoBehaviour
    {
        [SerializeField] Button inventoryButton;
        [SerializeField] Button characterButton;
        [SerializeField] Button mapButton;
        [SerializeField] Button wishButton;
        [SerializeField] Button teamButton;
        [SerializeField] Button eventButton;
        [SerializeField] Button artifactsButton;
        [SerializeField] Button weaponButton;

        UINavigationController navigation;

        void Awake()
        {
            navigation = GetComponentInParent<UINavigationController>();
            if (inventoryButton != null) inventoryButton.onClick.AddListener(() => navigation?.ToggleInventory());
            if (characterButton != null) characterButton.onClick.AddListener(() => navigation?.ToggleCharacter());
            if (mapButton != null) mapButton.onClick.AddListener(() => navigation?.ToggleWorldMap());
            if (wishButton != null) wishButton.onClick.AddListener(() => navigation?.ToggleWish());
            if (teamButton != null) teamButton.onClick.AddListener(() => navigation?.ToggleTeam());
            if (eventButton != null) eventButton.onClick.AddListener(() => navigation?.ToggleEvent());
            if (artifactsButton != null) artifactsButton.onClick.AddListener(() => navigation?.ToggleArtifacts());
            if (weaponButton != null) weaponButton.onClick.AddListener(() => navigation?.ToggleWeapon());
        }
    }
}
