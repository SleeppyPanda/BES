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
        public bool HeavyAttackPressed { get; private set; }
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
            ResetFrameInputs();

            if (!isBound)
            {
                ReadKeyboardFallback();
                return;
            }

            Move = moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;
            Look = lookAction != null ? lookAction.ReadValue<Vector2>() : Vector2.zero;
            JumpPressed = jumpAction != null && jumpAction.WasPressedThisFrame();
            SprintHeld = sprintAction != null && sprintAction.IsPressed();
            AttackPressed = attackAction != null && attackAction.WasPressedThisFrame();
            HeavyAttackPressed = Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;
            Skill1Pressed = skill1Action != null && skill1Action.WasPressedThisFrame();
            Skill2Pressed = skill2Action != null && skill2Action.WasPressedThisFrame();
            DodgePressed = dodgeAction != null && dodgeAction.WasPressedThisFrame();
            InteractPressed = interactAction != null && interactAction.WasPressedThisFrame();
            InventoryPressed = inventoryAction != null && inventoryAction.WasPressedThisFrame();
            CharacterMenuPressed = characterMenuAction != null && characterMenuAction.WasPressedThisFrame();
            MapTogglePressed = mapToggleAction != null && mapToggleAction.WasPressedThisFrame();
            CloseMenuPressed = closeMenuAction != null && closeMenuAction.WasPressedThisFrame();
            WeaponMenuPressed = weaponMenuAction != null && weaponMenuAction.WasPressedThisFrame();
            WishMenuPressed = wishMenuAction != null && wishMenuAction.WasPressedThisFrame();
            TeamMenuPressed = teamMenuAction != null && teamMenuAction.WasPressedThisFrame();
            EventMenuPressed = eventMenuAction != null && eventMenuAction.WasPressedThisFrame();
            ArtifactsMenuPressed = artifactsMenuAction != null && artifactsMenuAction.WasPressedThisFrame();

            if (Move.sqrMagnitude < 0.001f)
                ReadKeyboardMoveFallback();
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

            playerMap = inputActions.FindActionMap("Player", false);
            uiMap = inputActions.FindActionMap("UI", false);

            if (playerMap != null)
            {
                moveAction = playerMap.FindAction("Move", false);
                lookAction = playerMap.FindAction("Look", false);
                jumpAction = playerMap.FindAction("Jump", false);
                sprintAction = playerMap.FindAction("Sprint", false);
                attackAction = playerMap.FindAction("Attack", false);
                skill1Action = playerMap.FindAction("Skill1", false);
                skill2Action = playerMap.FindAction("Skill2", false);
                dodgeAction = playerMap.FindAction("Dodge", false);
                interactAction = playerMap.FindAction("Interact", false);
            }

            if (uiMap != null)
            {
                inventoryAction = uiMap.FindAction("Inventory", false);
                characterMenuAction = uiMap.FindAction("CharacterMenu", false);
                mapToggleAction = uiMap.FindAction("MapToggle", false);
                closeMenuAction = uiMap.FindAction("CloseMenu", false);
                weaponMenuAction = uiMap.FindAction("WeaponMenu", false);
                wishMenuAction = uiMap.FindAction("WishMenu", false);
                teamMenuAction = uiMap.FindAction("TeamMenu", false);
                eventMenuAction = uiMap.FindAction("EventMenu", false);
                artifactsMenuAction = uiMap.FindAction("ArtifactsMenu", false);
            }
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

        void ResetFrameInputs()
        {
            Move = Vector2.zero;
            Look = Vector2.zero;
            JumpPressed = false;
            SprintHeld = false;
            AttackPressed = false;
            HeavyAttackPressed = false;
            Skill1Pressed = false;
            Skill2Pressed = false;
            DodgePressed = false;
            InteractPressed = false;
            InventoryPressed = false;
            CharacterMenuPressed = false;
            MapTogglePressed = false;
            CloseMenuPressed = false;
            WeaponMenuPressed = false;
            WishMenuPressed = false;
            TeamMenuPressed = false;
            EventMenuPressed = false;
            ArtifactsMenuPressed = false;
        }

        void ReadKeyboardFallback()
        {
            ReadKeyboardMoveFallback();

            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (keyboard == null)
                return;

            JumpPressed = keyboard.spaceKey.wasPressedThisFrame;
            SprintHeld = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
            AttackPressed = mouse != null && mouse.leftButton.wasPressedThisFrame;
            HeavyAttackPressed = mouse != null && mouse.rightButton.wasPressedThisFrame;
            Skill1Pressed = keyboard.qKey.wasPressedThisFrame;
            Skill2Pressed = keyboard.eKey.wasPressedThisFrame;
            DodgePressed = keyboard.leftCtrlKey.wasPressedThisFrame || keyboard.cKey.wasPressedThisFrame;
            InteractPressed = keyboard.fKey.wasPressedThisFrame || keyboard.eKey.wasPressedThisFrame;
            InventoryPressed = keyboard.iKey.wasPressedThisFrame;
            CharacterMenuPressed = keyboard.cKey.wasPressedThisFrame;
            MapTogglePressed = keyboard.mKey.wasPressedThisFrame;
            CloseMenuPressed = keyboard.escapeKey.wasPressedThisFrame;
            WeaponMenuPressed = keyboard.vKey.wasPressedThisFrame;
            WishMenuPressed = keyboard.gKey.wasPressedThisFrame;
            TeamMenuPressed = keyboard.tKey.wasPressedThisFrame;
            EventMenuPressed = keyboard.oKey.wasPressedThisFrame;
            ArtifactsMenuPressed = keyboard.rKey.wasPressedThisFrame;
        }

        void ReadKeyboardMoveFallback()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            var x = 0f;
            var y = 0f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) x -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) x += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) y -= 1f;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) y += 1f;

            var move = new Vector2(x, y);
            Move = move.sqrMagnitude > 1f ? move.normalized : move;
        }
    }
}
