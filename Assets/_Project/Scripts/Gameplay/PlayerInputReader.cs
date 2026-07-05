using UnityEngine;
using UnityEngine.InputSystem;

namespace BES.Gameplay
{
    public class PlayerInputReader : MonoBehaviour
    {
        const string DefaultInputPath = "Assets/InputSystem_Actions.inputactions";

        [SerializeField] InputActionAsset inputActions;

        InputActionMap playerMap;
        InputActionMap uiMap;
        InputAction moveAction;
        InputAction lookAction;
        InputAction jumpAction;
        InputAction sprintAction;
        InputAction attackAction;
        InputAction skill1Action;
        InputAction skill2Action;
        InputAction dodgeAction;
        InputAction interactAction;
        InputAction inventoryAction;
        InputAction characterMenuAction;
        InputAction mapToggleAction;
        InputAction closeMenuAction;
        InputAction weaponMenuAction;
        InputAction wishMenuAction;
        InputAction teamMenuAction;
        InputAction eventMenuAction;
        InputAction artifactsMenuAction;
        bool isBound;

        public Vector2 Move { get; private set; }
        public Vector2 Look { get; private set; }
        public bool JumpPressed { get; private set; }
        public bool SprintHeld { get; private set; }
        public bool AttackPressed { get; private set; }
        public bool Skill1Pressed { get; private set; }
        public bool Skill2Pressed { get; private set; }
        public bool DodgePressed { get; private set; }
        public bool InteractPressed { get; private set; }
        public bool InventoryPressed { get; private set; }
        public bool CharacterMenuPressed { get; private set; }
        public bool MapTogglePressed { get; private set; }
        public bool CloseMenuPressed { get; private set; }
        public bool WeaponMenuPressed { get; private set; }
        public bool WishMenuPressed { get; private set; }
        public bool TeamMenuPressed { get; private set; }
        public bool EventMenuPressed { get; private set; }
        public bool ArtifactsMenuPressed { get; private set; }

        public void SetInputActions(InputActionAsset asset)
        {
            if (inputActions == asset && isBound)
                return;

            if (enabled)
                DisableActions();

            inputActions = asset;
            isBound = false;
            TryBindActions();

            if (enabled)
                EnableActions();
        }

        void Awake()
        {
            ResolveInputAsset();
            TryBindActions();
        }

        void OnEnable() => EnableActions();

        void OnDisable() => DisableActions();

        void Update()
        {
            if (!isBound)
                return;

            Move = moveAction.ReadValue<Vector2>();
            Look = lookAction.ReadValue<Vector2>();
            JumpPressed = jumpAction.WasPressedThisFrame();
            SprintHeld = sprintAction.IsPressed();
            AttackPressed = attackAction.WasPressedThisFrame();
            Skill1Pressed = skill1Action.WasPressedThisFrame();
            Skill2Pressed = skill2Action.WasPressedThisFrame();
            DodgePressed = dodgeAction.WasPressedThisFrame();
            InteractPressed = interactAction.WasPressedThisFrame();
            InventoryPressed = inventoryAction.WasPressedThisFrame();
            CharacterMenuPressed = characterMenuAction.WasPressedThisFrame();
            MapTogglePressed = mapToggleAction.WasPressedThisFrame();
            CloseMenuPressed = closeMenuAction.WasPressedThisFrame();
            WeaponMenuPressed = weaponMenuAction.WasPressedThisFrame();
            WishMenuPressed = wishMenuAction.WasPressedThisFrame();
            TeamMenuPressed = teamMenuAction.WasPressedThisFrame();
            EventMenuPressed = eventMenuAction.WasPressedThisFrame();
            ArtifactsMenuPressed = artifactsMenuAction.WasPressedThisFrame();
        }

        void ResolveInputAsset()
        {
            if (inputActions != null)
                return;

            inputActions = Resources.Load<InputActionAsset>("InputSystem_Actions");
#if UNITY_EDITOR
            if (inputActions == null)
                inputActions = UnityEditor.AssetDatabase.LoadAssetAtPath<InputActionAsset>(DefaultInputPath);
#endif
            if (inputActions == null)
                Debug.LogError("[BES] Không tìm thấy InputSystem_Actions.");
        }

        void TryBindActions()
        {
            if (isBound || inputActions == null)
                return;

            playerMap = inputActions.FindActionMap("Player", true);
            uiMap = inputActions.FindActionMap("UI", true);

            moveAction = playerMap.FindAction("Move", true);
            lookAction = playerMap.FindAction("Look", true);
            jumpAction = playerMap.FindAction("Jump", true);
            sprintAction = playerMap.FindAction("Sprint", true);
            attackAction = playerMap.FindAction("Attack", true);
            skill1Action = playerMap.FindAction("Skill1", true);
            skill2Action = playerMap.FindAction("Skill2", true);
            dodgeAction = playerMap.FindAction("Dodge", true);
            interactAction = playerMap.FindAction("Interact", true);
            inventoryAction = uiMap.FindAction("Inventory", true);
            characterMenuAction = uiMap.FindAction("CharacterMenu", true);
            mapToggleAction = uiMap.FindAction("MapToggle", true);
            closeMenuAction = uiMap.FindAction("CloseMenu", true);
            weaponMenuAction = uiMap.FindAction("WeaponMenu", true);
            wishMenuAction = uiMap.FindAction("WishMenu", true);
            teamMenuAction = uiMap.FindAction("TeamMenu", true);
            eventMenuAction = uiMap.FindAction("EventMenu", true);
            artifactsMenuAction = uiMap.FindAction("ArtifactsMenu", true);
            isBound = true;
        }

        void EnableActions()
        {
            if (!isBound || inputActions == null)
                return;

            inputActions.Enable();
        }

        void DisableActions()
        {
            if (!isBound || inputActions == null)
                return;

            inputActions.Disable();
        }
    }
}
