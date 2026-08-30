using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace BES.Gameplay
{
    public class EnemySpawnRegion : MonoBehaviour
    {
        [SerializeField] string regionId = "region_creation_city";
        [SerializeField] string subRegionId = "subregion_01";
        [SerializeField] GameObject[] enemyPrefabs;
        [SerializeField] Transform[] spawnPoints;
        [Tooltip("Optional box used to pick a random position inside the region. When assigned, it is preferred over Spawn Points.")]
        [SerializeField] BoxCollider spawnArea;
        [SerializeField] LayerMask groundMask = ~0;
        [SerializeField] float groundProbeHeight = 4f;
        [SerializeField] float groundProbeDistance = 15f;
        [SerializeField] int positionAttempts = 12;
        [SerializeField] Transform spawnedParent;
        [SerializeField] int minSpawnCount = 1;
        [SerializeField] int maxSpawnCount = 3;
        [SerializeField] bool spawnOnStart = true;
        [SerializeField] float patrolRadiusOverride = -1f;
        [SerializeField] bool respawnWhenCleared;
        [SerializeField] float respawnDelay = 30f;

        readonly List<GameObject> spawnedEnemies = new List<GameObject>();
        float respawnTimer;

        public string RegionId => regionId;
        public string SubRegionId => subRegionId;

        void Start()
        {
            // Pre-warm pool for zero-stutter first spawn
            if (enemyPrefabs != null)
                foreach (var p in enemyPrefabs)
                    if (p != null) EnemyObjectPool.Instance.Warmup(p, maxSpawnCount);

            if (spawnOnStart)
                SpawnRandomWave();

            // Replace per-frame Update() polling with a cheap periodic check (every 5s)
            if (respawnWhenCleared)
                InvokeRepeating(nameof(CheckRespawn), respawnDelay, 5f);
        }

        // Replaces Update() — runs once every 5s instead of every frame
        void CheckRespawn()
        {
            if (HasAliveEnemy()) return;

            respawnTimer += 5f;
            if (respawnTimer >= respawnDelay)
                SpawnRandomWave();
        }

        [ContextMenu("Spawn Random Wave")]
        public void SpawnRandomWave()
        {
            respawnTimer = 0f;
            RemoveNullEntries();

            var hasPoints = spawnPoints != null && spawnPoints.Length > 0;
            if (enemyPrefabs == null || enemyPrefabs.Length == 0 || (spawnArea == null && !hasPoints))
                return;

            var min = Mathf.Max(0, minSpawnCount);
            var max = Mathf.Max(min, maxSpawnCount);
            var count = Random.Range(min, max + 1);

            var availablePoints = hasPoints ? new List<Transform>(spawnPoints) : null;
            for (var i = 0; i < count; i++)
            {
                var prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
                if (prefab == null) continue;

                Vector3 position;
                Quaternion rotation;

                if (availablePoints != null && availablePoints.Count > 0)
                {
                    int ptIdx = Random.Range(0, availablePoints.Count);
                    var pt = availablePoints[ptIdx];
                    availablePoints.RemoveAt(ptIdx); // Each monster gets its own unique point
                    position = pt.position;
                    rotation = pt.rotation;

                    if (NavMesh.SamplePosition(position, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                    {
                        position = hit.position;
                    }
                }
                else if (!TryGetSpawnPose(out position, out rotation))
                {
                    continue;
                }

                // Use Object Pool instead of Instantiate to eliminate GC Spikes
                var parent = spawnedParent != null ? spawnedParent : transform;
                var enemy = EnemyObjectPool.Instance.Get(prefab, position, rotation);
                if (enemy != null)
                    enemy.transform.SetParent(parent);

                var ai = enemy != null ? enemy.GetComponent<EnemyAI>() : null;
                if (ai != null)
                {
                    if (patrolRadiusOverride > 0f)
                    {
                        ai.PatrolRadius = patrolRadiusOverride;
                    }

                    // Tách biệt vị trí tránh dẫm lên nhau
                    var agent = enemy.GetComponent<UnityEngine.AI.NavMeshAgent>();
                    if (agent != null)
                    {
                        agent.avoidancePriority = Random.Range(20, 80);
                    }
                }
                if (enemy != null)
                    spawnedEnemies.Add(enemy);
            }
        }

        bool TryGetSpawnPose(out Vector3 position, out Quaternion rotation)
        {
            if (spawnArea != null)
            {
                float regionY = spawnArea.transform.position.y;
                for (int attempt = 0; attempt < 25; attempt++)
                {
                    var half = spawnArea.size * 0.5f;
                    var local = spawnArea.center + new Vector3(
                        Random.Range(-half.x, half.x),
                        0f,
                        Random.Range(-half.z, half.z));
                    Vector3 candidatePos = spawnArea.transform.TransformPoint(local);
                    
                    // Bắn tia Raycast từ trên cao nhẹ (+2.5m) xuống dưới tối đa 5m để chỉ bắt đúng mặt sàn cùng tầng
                    if (Physics.Raycast(candidatePos + Vector3.up * 2.5f, Vector3.down, out RaycastHit rayHit, 5.5f, groundMask))
                    {
                        // Kiểm tra độ cao: Phải ở cùng tầng với vùng spawn (+/- 1.5m), tuyệt đối không nhận tầng đáy dưới map
                        if (Mathf.Abs(rayHit.point.y - regionY) <= 1.8f)
                        {
                            candidatePos.y = rayHit.point.y + 0.05f;
                            
                            // Sample NavMesh tại đúng độ cao của mặt sàn với bán kính hẹp (1.2m)
                            if (NavMesh.SamplePosition(candidatePos, out NavMeshHit hit, 1.2f, NavMesh.AllAreas))
                            {
                                // Đảm bảo điểm NavMesh cũng phải ở cùng tầng (không bị snap xuống tầng dưới map)
                                if (Mathf.Abs(hit.position.y - regionY) <= 1.8f)
                                {
                                    position = hit.position;
                                    rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                                    return true;
                                }
                            }
                        }
                    }
                }

                // Fallback: sinh tại tâm của vùng spawn (đã được GetGroundPoint định vị an toàn trên mặt đất)
                Vector3 fallbackPos = spawnArea.transform.position;
                if (Physics.Raycast(fallbackPos + Vector3.up * 2.5f, Vector3.down, out RaycastHit centerHit, 5.5f, groundMask))
                {
                    fallbackPos.y = centerHit.point.y + 0.05f;
                }

                if (NavMesh.SamplePosition(fallbackPos, out NavMeshHit fallbackNavHit, 1.5f, NavMesh.AllAreas))
                {
                    if (Mathf.Abs(fallbackNavHit.position.y - regionY) <= 1.8f)
                    {
                        position = fallbackNavHit.position;
                        rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                        return true;
                    }
                }

                position = fallbackPos;
                rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                return true;
            }

            if (spawnPoints != null && spawnPoints.Length > 0)
            {
                for (var i = 0; i < spawnPoints.Length; i++)
                {
                    var point = spawnPoints[Random.Range(0, spawnPoints.Length)];
                    if (point == null)
                        continue;

                    if (NavMesh.SamplePosition(point.position, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                    {
                        position = hit.position;
                        rotation = point.rotation;
                        return true;
                    }

                    position = point.position;
                    rotation = point.rotation;
                    return true;
                }
            }

            position = default;
            rotation = default;
            return false;
        }

        [ContextMenu("Clear Spawned Enemies")]
        public void ClearSpawnedEnemies()
        {
            // Return to pool instead of Destroy — no GC allocation
            EnemyObjectPool.Instance.ReturnAll(spawnedEnemies);
            respawnTimer = 0f;
        }

        bool HasAliveEnemy()
        {
            RemoveNullEntries();
            return spawnedEnemies.Count > 0;
        }

        void RemoveNullEntries()
        {
            for (var i = spawnedEnemies.Count - 1; i >= 0; i--)
            {
                if (spawnedEnemies[i] == null)
                    spawnedEnemies.RemoveAt(i);
            }
        }
    }
}
