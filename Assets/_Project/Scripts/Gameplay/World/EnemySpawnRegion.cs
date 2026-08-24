using System.Collections.Generic;
using UnityEngine;

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
        [SerializeField] float groundProbeHeight = 80f;
        [SerializeField] float groundProbeDistance = 200f;
        [SerializeField] int positionAttempts = 12;
        [SerializeField] Transform spawnedParent;
        [SerializeField] int minSpawnCount = 1;
        [SerializeField] int maxSpawnCount = 3;
        [SerializeField] bool spawnOnStart = true;
        [SerializeField] bool respawnWhenCleared;
        [SerializeField] float respawnDelay = 30f;

        readonly List<GameObject> spawnedEnemies = new List<GameObject>();
        float respawnTimer;

        public string RegionId => regionId;
        public string SubRegionId => subRegionId;

        void Start()
        {
            if (spawnOnStart)
                SpawnRandomWave();
        }

        void Update()
        {
            if (!respawnWhenCleared || HasAliveEnemy())
                return;

            respawnTimer += Time.deltaTime;
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
            for (var i = 0; i < count; i++)
            {
                var prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
                if (prefab == null || !TryGetSpawnPose(out var position, out var rotation))
                    continue;

                var enemy = Instantiate(prefab, position, rotation, spawnedParent != null ? spawnedParent : transform);
                spawnedEnemies.Add(enemy);
            }
        }

        bool TryGetSpawnPose(out Vector3 position, out Quaternion rotation)
        {
            if (spawnArea != null)
            {
                var attempts = Mathf.Max(1, positionAttempts);
                for (var i = 0; i < attempts; i++)
                {
                    var half = spawnArea.size * 0.5f;
                    var local = spawnArea.center + new Vector3(
                        Random.Range(-half.x, half.x),
                        half.y,
                        Random.Range(-half.z, half.z));
                    var origin = spawnArea.transform.TransformPoint(local) + Vector3.up * groundProbeHeight;
                    if (!Physics.Raycast(origin, Vector3.down, out var hit, groundProbeDistance, groundMask, QueryTriggerInteraction.Ignore))
                        continue;

                    position = hit.point;
                    rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                    return true;
                }
            }

            if (spawnPoints != null && spawnPoints.Length > 0)
            {
                for (var i = 0; i < spawnPoints.Length; i++)
                {
                    var point = spawnPoints[Random.Range(0, spawnPoints.Length)];
                    if (point == null)
                        continue;

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
            for (var i = spawnedEnemies.Count - 1; i >= 0; i--)
            {
                var enemy = spawnedEnemies[i];
                if (enemy != null)
                    Destroy(enemy);
            }

            spawnedEnemies.Clear();
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
