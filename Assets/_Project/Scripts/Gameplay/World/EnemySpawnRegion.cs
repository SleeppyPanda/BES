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

            if (enemyPrefabs == null || enemyPrefabs.Length == 0 || spawnPoints == null || spawnPoints.Length == 0)
                return;

            var min = Mathf.Max(0, minSpawnCount);
            var max = Mathf.Max(min, maxSpawnCount);
            var count = Random.Range(min, max + 1);
            for (var i = 0; i < count; i++)
            {
                var prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
                var point = spawnPoints[Random.Range(0, spawnPoints.Length)];
                if (prefab == null || point == null)
                    continue;

                var enemy = Instantiate(prefab, point.position, point.rotation, spawnedParent != null ? spawnedParent : transform);
                spawnedEnemies.Add(enemy);
            }
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
