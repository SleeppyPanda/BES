using BES.Gameplay;
using UnityEngine;

namespace BES.UI
{
    public class UIRootController : MonoBehaviour
    {
        [SerializeField] InventoryUI inventoryUI;
        [SerializeField] CharacterProfileUI characterProfileUI;
        [SerializeField] GameMapUI gameMapUI;

        PlayerInputReader input;

        public bool IsMenuOpen =>
            (inventoryUI != null && inventoryUI.IsOpen) ||
            (characterProfileUI != null && characterProfileUI.IsOpen) ||
            (gameMapUI != null && gameMapUI.IsOpen);

        void Awake()
        {
            input = FindAnyObjectByType<PlayerInputReader>();
            inventoryUI ??= GetComponentInChildren<InventoryUI>(true);
            characterProfileUI ??= GetComponentInChildren<CharacterProfileUI>(true);
            gameMapUI ??= GetComponentInChildren<GameMapUI>(true);
        }

        void Update()
        {
            if (input == null)
            {
                input = FindAnyObjectByType<PlayerInputReader>();
                if (input == null)
                    return;
            }

            if (input.InventoryPressed)
                ToggleInventory();

            if (input.CharacterMenuPressed)
                ToggleCharacterMenu();

            if (input.MapTogglePressed)
                ToggleWorldMap();
        }

        void ToggleInventory()
        {
            if (inventoryUI == null)
                return;

            var opening = !inventoryUI.IsOpen;
            if (opening)
            {
                characterProfileUI?.Close();
                gameMapUI?.Close();
            }

            inventoryUI.Toggle();
        }

        void ToggleCharacterMenu()
        {
            if (characterProfileUI == null)
                return;

            var opening = !characterProfileUI.IsOpen;
            if (opening)
            {
                inventoryUI?.Close();
                gameMapUI?.Close();
            }

            characterProfileUI.Toggle();
        }

        void ToggleWorldMap()
        {
            if (gameMapUI == null)
                return;

            var opening = !gameMapUI.IsOpen;
            if (opening)
            {
                inventoryUI?.Close();
                characterProfileUI?.Close();
            }

            gameMapUI.Toggle();
        }
    }
}
