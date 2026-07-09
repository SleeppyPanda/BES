using BES.Core;
using BES.Gameplay;
using BES.Narrative;
using BES.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BES.Gameplay
{
    public class GameplaySceneBootstrap : MonoBehaviour
    {
        [SerializeField] InputActionAsset inputActions;
        [SerializeField] LayerMask enemyLayer;

        void Start()
        {
            EnsureCombatManager();
            EnsureNarrativeSystems();
            EnsureDialogueUi();
            SpawnPlayerIfMissing();
            ApplySaveIfLoaded();
            StartMainQuestIfNeeded();
        }

        void EnsureNarrativeSystems()
        {
            if (FindAnyObjectByType<DialogueSystem>() != null)
                return;

            var go = new GameObject("NarrativeSystems");
            go.AddComponent<DialogueSystem>();
            go.AddComponent<AIDialogueService>();
        }

        void EnsureDialogueUi()
        {
            if (FindAnyObjectByType<DialogueUI>() != null)
                return;

            var go = new GameObject("DialogueUI");
            go.AddComponent<DialogueUI>();
        }

        void EnsureCombatManager()
        {
            if (FindAnyObjectByType<CombatManager>() == null)
            {
                var go = new GameObject("CombatManager");
                go.AddComponent<CombatManager>();
            }
        }

        void SpawnPlayerIfMissing()
        {
            var existingPlayer = GameObject.FindGameObjectWithTag("Player");
            if (existingPlayer != null)
            {
                EnsurePlayerComponents(existingPlayer);
                SetupFollowCamera(existingPlayer.transform);
                return;
            }

            var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Player";
            player.tag = "Player";

            foreach (var col in player.GetComponents<Collider>())
                Destroy(col);

            var controller = player.AddComponent<CharacterController>();
            controller.height = 2f;
            controller.radius = 0.4f;
            controller.center = new Vector3(0f, 1f, 0f);

            EnsurePlayerComponents(player);
            SetupFollowCamera(player.transform);
            player.transform.position = new Vector3(0f, 1f, 0f);
        }

        void EnsurePlayerComponents(GameObject player)
        {
            if (player == null)
                return;

            var controller = player.GetComponent<CharacterController>();
            if (controller == null)
            {
                controller = player.AddComponent<CharacterController>();
                controller.height = 2f;
                controller.radius = 0.4f;
                controller.center = new Vector3(0f, 1f, 0f);
            }

            var inputReader = player.GetComponent<PlayerInputReader>();
            if (inputReader == null)
                inputReader = player.AddComponent<PlayerInputReader>();
            inputReader.SetInputActions(inputActions);

            if (player.GetComponent<PlayerMotor>() == null) player.AddComponent<PlayerMotor>();
            if (player.GetComponent<StaminaSystem>() == null) player.AddComponent<StaminaSystem>();
            if (player.GetComponent<PlayerStats>() == null) player.AddComponent<PlayerStats>();
            if (player.GetComponent<DodgeController>() == null) player.AddComponent<DodgeController>();
            if (player.GetComponent<BasicAttackController>() == null) player.AddComponent<BasicAttackController>();
            if (player.GetComponent<SkillController>() == null) player.AddComponent<SkillController>();
            if (player.GetComponent<PlayerBuildStats>() == null) player.AddComponent<PlayerBuildStats>();
            if (player.GetComponent<PartySwapController>() == null) player.AddComponent<PartySwapController>();
            if (player.GetComponent<PartyCharacterVisualSwitcher>() == null) player.AddComponent<PartyCharacterVisualSwitcher>();
        }

        void ApplySaveIfLoaded()
        {
            var save = GameManager.Instance?.Save;
            var player = GameObject.FindGameObjectWithTag("Player");
            if (save != null && player != null && save.LoadedFromContinue)
            {
                save.ApplyPlayerState(player);
                RestoreRegionFromSave(save);
            }
        }

        static void RestoreRegionFromSave(SaveSystem save)
        {
            var regionId = save.Current.currentRegionId;
            if (string.IsNullOrEmpty(regionId))
                return;

            var points = FindObjectsByType<TeleportPoint>(FindObjectsSortMode.None);
            foreach (var point in points)
            {
                if (point.RegionId != regionId || point.Destination == null)
                    continue;

                var player = GameObject.FindGameObjectWithTag("Player");
                if (player == null)
                    return;

                TeleportService.TeleportPlayer(
                    player.transform,
                    point.Destination.position,
                    point.Destination.rotation,
                    point.PointId,
                    point.RegionId);
                return;
            }
        }

        void StartMainQuestIfNeeded()
        {
            if (GameManager.Instance?.Save?.LoadedFromContinue == true)
                return;

            GameManager.Instance?.Quests.StartQuest("main_awakening");
        }

        void SetupFollowCamera(Transform playerTransform)
        {
            var cam = Camera.main;
            GameObject camGo;

            if (cam != null)
            {
                camGo = cam.gameObject;
            }
            else
            {
                camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                camGo.AddComponent<Camera>();
                camGo.AddComponent<AudioListener>();
            }

            var tpc = camGo.GetComponent<ThirdPersonCamera>();
            if (tpc == null)
                tpc = camGo.AddComponent<ThirdPersonCamera>();

            tpc.SetTarget(playerTransform);
        }
    }
}
