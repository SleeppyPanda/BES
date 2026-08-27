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
        [Header("Test Enemy")]
        [SerializeField] bool spawnTestEnemyOnStart = true;
        [SerializeField] GameObject testEnemyPrefab;
        [SerializeField] Vector3 testEnemyOffset = new Vector3(0f, 0f, 6f);
        [SerializeField] float testEnemyDetectRange = 18f;
        [SerializeField] float testEnemyAttackRange = 2f;
        [SerializeField] float testEnemyAttackDamage = 12f;
        [SerializeField] float testEnemyAttackCooldown = 1.2f;
        [SerializeField] float testEnemyMoveSpeed = 3.5f;

        void Start()
        {
            EnsureGameplayHud();
            EnsureCombatManager();
            EnsureNarrativeSystems();
            EnsureDialogueUi();
            SpawnPlayerIfMissing();
            ApplySaveIfLoaded();
            EnsureTestEnemy();
            StartMainQuestIfNeeded();
            EnsureFallRecoveryZone();
            EnsureTreasureChests();
            EnsureWindCurrents();
        }

        void EnsureGameplayHud()
        {
            if (FindAnyObjectByType<HUDController>() != null)
                return;

            string hudPrefabPath = "Prefabs/GameplayHUD";
            GameObject hudPrefab = Resources.Load<GameObject>(hudPrefabPath);
            if (hudPrefab == null)
            {
#if UNITY_EDITOR
                hudPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Resources/Prefabs/GameplayHUD.prefab");
#endif
            }

            if (hudPrefab != null)
            {
                var hudInstance = Instantiate(hudPrefab);
                hudInstance.name = "GameplayHUD";
                Debug.Log("[BES Bootstrap] Đã tự động sinh Gameplay HUD Canvas cho màn chơi!");
            }
            else
            {
                Debug.LogWarning("[BES Bootstrap] Không tìm thấy GameplayHUD prefab để tải!");
            }
        }

        void EnsureTreasureChests()
        {
            string chestPrefabPath = "Prefabs/TreasureChest";
            GameObject chestPrefab = Resources.Load<GameObject>(chestPrefabPath);
            if (chestPrefab == null)
            {
#if UNITY_EDITOR
                chestPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Resources/Prefabs/TreasureChest.prefab");
#endif
            }

            if (chestPrefab == null) return;

            // Raw candidate positions on the sand
            Vector3[] candidates = new Vector3[]
            {
                new Vector3(4f, 50f, -6f),
                new Vector3(18f, 50f, 15f),
                new Vector3(-14f, 50f, 18f)
            };

            for (int i = 0; i < candidates.Length; i++)
            {
                string chestId = $"chest_desert_{i}";
                if (MetaProgressState.Instance != null && MetaProgressState.Instance.IsWorldObjectCollected(chestId))
                    continue;

                // Perform Raycast downward to sit the chest exactly on the sand surface
                Vector3 spawnPos = candidates[i];
                spawnPos.y = 0.5f; // default fallback
                Ray ray = new Ray(candidates[i], Vector3.down);
                if (Physics.Raycast(ray, out RaycastHit hit, 100f))
                {
                    // Snap to the exact surface of the sand terrain and offset Y by 0.05m
                    spawnPos = hit.point + Vector3.up * 0.05f;
                }

                // Check if already exists in the scene
                bool exists = false;
                var existingChests = FindObjectsByType<TreasureChest>(FindObjectsSortMode.None);
                foreach (var c in existingChests)
                {
                    var field = typeof(TreasureChest).GetField("instanceId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    string id = field != null ? (string)field.GetValue(c) : string.Empty;
                    if (id == chestId)
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                {
                    var chestInstance = Instantiate(chestPrefab, spawnPos, Quaternion.identity);
                    chestInstance.name = $"TreasureChest_{i}";
                    var field = typeof(TreasureChest).GetField("instanceId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (field != null)
                    {
                        field.SetValue(chestInstance.GetComponent<TreasureChest>(), chestId);
                    }
                }
            }
        }

        void EnsureWindCurrents()
        {
            string currentPrefabPath = "Prefabs/WindCurrent";
            GameObject currentPrefab = Resources.Load<GameObject>(currentPrefabPath);
            if (currentPrefab == null)
            {
#if UNITY_EDITOR
                currentPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Resources/Prefabs/WindCurrent.prefab");
#endif
            }

            if (currentPrefab == null) return;

            // Candidate search zones for cliffs
            Vector3[] zones = new Vector3[]
            {
                new Vector3(6f, 0f, -4f),     // Cliff near spawn
                new Vector3(14f, 0f, 11f),    // Cliff near ruins
                new Vector3(-9.5f, 0f, 21f)   // Cliff near outskirts
            };

            for (int i = 0; i < zones.Length; i++)
            {
                // Smart auto-find: search locally around the candidate zone for a place where
                // there is a high platform above, but NO stairs/direct path connecting them (requiring a wind lift!)
                Vector3 finalSpawnPos = Vector3.zero;
                bool foundSafePos = false;

                // Search in a grid
                for (float dx = -2f; dx <= 2f; dx += 1.5f)
                {
                    for (float dz = -2f; dz <= 2f; dz += 1.5f)
                    {
                        Vector3 testLow = zones[i] + new Vector3(dx, 0f, dz);
                        
                        // 1. Raycast to find exact ground surface
                        Ray rayLow = new Ray(testLow + Vector3.up * 10f, Vector3.down);
                        if (!Physics.Raycast(rayLow, out RaycastHit hitLow, 20f)) continue;
                        
                        Vector3 groundPt = hitLow.point;

                        // 2. Check if ground is walkable on NavMesh (avoids inside walls)
                        if (!UnityEngine.AI.NavMesh.SamplePosition(groundPt, out UnityEngine.AI.NavMeshHit navHitLow, 1.2f, UnityEngine.AI.NavMesh.AllAreas))
                            continue;

                        // 3. Look for a nearby high point (the platform above)
                        Vector3[] offsets = new Vector3[] { Vector3.forward * 2f, Vector3.back * 2f, Vector3.left * 2f, Vector3.right * 2f };
                        foreach (var offset in offsets)
                        {
                            Vector3 testHigh = groundPt + offset;
                            Ray rayHigh = new Ray(testHigh + Vector3.up * 10f, Vector3.down);
                            if (Physics.Raycast(rayHigh, out RaycastHit hitHigh, 20f))
                            {
                                float heightDiff = hitHigh.point.y - groundPt.y;
                                // If height difference is between 2.0m and 5.0m (a cliff!)
                                if (heightDiff >= 2.0f && heightDiff <= 5.0f)
                                {
                                    // Check if there is a NavMesh connection (stairs/slope)
                                    if (UnityEngine.AI.NavMesh.SamplePosition(hitHigh.point, out UnityEngine.AI.NavMeshHit navHitHigh, 1.5f, UnityEngine.AI.NavMesh.AllAreas))
                                    {
                                        UnityEngine.AI.NavMeshPath path = new UnityEngine.AI.NavMeshPath();
                                        UnityEngine.AI.NavMesh.CalculatePath(navHitLow.position, navHitHigh.position, UnityEngine.AI.NavMesh.AllAreas, path);
                                        
                                        // If no complete direct path (PathPartial/Invalid) or path is very long (no stairs, must walk far around)
                                        float pathDist = GetPathLength(path);
                                        if (path.status != UnityEngine.AI.NavMeshPathStatus.PathComplete || pathDist > 14f)
                                        {
                                            // Lift by 0.05m to make sure it floats cleanly above the terrain mesh
                                            finalSpawnPos = navHitLow.position + Vector3.up * 0.05f;
                                            foundSafePos = true;
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                        if (foundSafePos) break;
                    }
                    if (foundSafePos) break;
                }

                // If smart search failed, use fallback snapped position
                if (!foundSafePos)
                {
                    if (UnityEngine.AI.NavMesh.SamplePosition(zones[i], out UnityEngine.AI.NavMeshHit fallbackHit, 8f, UnityEngine.AI.NavMesh.AllAreas))
                    {
                        finalSpawnPos = fallbackHit.position + Vector3.up * 0.05f;
                        foundSafePos = true;
                    }
                }

                if (foundSafePos)
                {
                    // Check if already exists in the scene
                    bool exists = false;
                    var existingWinds = FindObjectsByType<WindCurrent>(FindObjectsSortMode.None);
                    foreach (var w in existingWinds)
                    {
                        if (Vector3.Distance(w.transform.position, finalSpawnPos) < 1.5f)
                        {
                            exists = true;
                            break;
                        }
                    }

                    if (!exists)
                    {
                        var instance = Instantiate(currentPrefab, finalSpawnPos, Quaternion.identity);
                        instance.name = $"WindCurrent_{i}";
                    }
                }
            }
        }

        float GetPathLength(UnityEngine.AI.NavMeshPath path)
        {
            if (path == null || path.corners.Length < 2) return 0f;
            float length = 0f;
            for (int i = 1; i < path.corners.Length; i++)
            {
                length += Vector3.Distance(path.corners[i - 1], path.corners[i]);
            }
            return length;
        }

        void EnsureFallRecoveryZone()
        {
            if (FindAnyObjectByType<FallRecoveryZone>() == null)
            {
                var go = new GameObject("FallRecoveryZone");
                go.AddComponent<FallRecoveryZone>();
            }
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

                // Warp existing player to the bootstrap's position (on safe road)
                var existingCc = existingPlayer.GetComponent<CharacterController>();
                if (existingCc != null) existingCc.enabled = false;
                if (UnityEngine.AI.NavMesh.SamplePosition(transform.position, out UnityEngine.AI.NavMeshHit existingHit, 30f, UnityEngine.AI.NavMesh.AllAreas))
                {
                    existingPlayer.transform.position = existingHit.position + Vector3.up * 0.1f;
                }
                else
                {
                    existingPlayer.transform.position = transform.position;
                }
                if (existingCc != null) existingCc.enabled = true;
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
            
            // Warp to nearest NavMesh position to prevent spawning inside walls/colliders
            var newCc = player.GetComponent<CharacterController>();
            if (newCc != null) newCc.enabled = false;

            if (UnityEngine.AI.NavMesh.SamplePosition(transform.position, out UnityEngine.AI.NavMeshHit newHit, 30f, UnityEngine.AI.NavMesh.AllAreas))
            {
                player.transform.position = newHit.position + Vector3.up * 0.1f;
            }
            else
            {
                player.transform.position = transform.position;
            }

            if (newCc != null) newCc.enabled = true;
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
            if (FindAnyObjectByType<PartySwapController>() == null) player.AddComponent<PartySwapController>();
            if (player.GetComponent<PartyCharacterVisualSwitcher>() == null) player.AddComponent<PartyCharacterVisualSwitcher>();
        }

        void EnsureTestEnemy()
        {
            if (!spawnTestEnemyOnStart || FindAnyObjectByType<EnemyAI>() != null)
                return;

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
                return;

            var spawnPosition = player.transform.position + testEnemyOffset;
            GameObject enemy;
            if (testEnemyPrefab != null)
                enemy = Instantiate(testEnemyPrefab, spawnPosition, Quaternion.identity);
            else
                enemy = GameObject.CreatePrimitive(PrimitiveType.Capsule);

            enemy.name = "Enemy_TestDamage";
            TrySetTag(enemy, "Enemy");
            enemy.transform.position = spawnPosition;
            var lookDirection = player.transform.position - spawnPosition;
            lookDirection.y = 0f;
            if (lookDirection.sqrMagnitude > 0.01f)
                enemy.transform.rotation = Quaternion.LookRotation(lookDirection.normalized);

            var enemyLayerIndex = LayerMask.NameToLayer("Enemy");
            if (enemyLayerIndex >= 0)
                enemy.layer = enemyLayerIndex;

            if (enemy.GetComponent<EnemyHealth>() == null)
                enemy.AddComponent<EnemyHealth>();
            if (enemy.GetComponent<EnemyHealthBar>() == null)
                enemy.AddComponent<EnemyHealthBar>();
            if (enemy.GetComponent<EnemyDamageFeedback>() == null)
                enemy.AddComponent<EnemyDamageFeedback>();
            var ai = enemy.GetComponent<EnemyAI>();
            if (ai == null)
                ai = enemy.AddComponent<EnemyAI>();
            ai.Configure(testEnemyDetectRange, testEnemyAttackRange, testEnemyAttackDamage, testEnemyAttackCooldown, testEnemyMoveSpeed);

            var renderer = enemy.GetComponentInChildren<Renderer>();
            if (renderer != null && testEnemyPrefab == null)
                renderer.material.color = new Color(0.75f, 0.12f, 0.12f, 1f);
        }

        static void TrySetTag(GameObject go, string tagName)
        {
            try
            {
                go.tag = tagName;
            }
            catch (UnityException)
            {
                Debug.LogWarning($"[BES] Tag '{tagName}' is missing. Test enemy was spawned without that tag.");
            }
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
            GameManager.Instance?.Quests.StartQuestPanelTestQuests();
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
