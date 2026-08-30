using System.Collections.Generic;
using UnityEngine;

namespace BES.Gameplay
{
    /// <summary>
    /// Singleton Object Pool for Enemy GameObjects.
    /// Eliminates GC Spikes caused by Instantiate/Destroy on enemy spawn/death.
    /// Technique: Genshin Impact / Elden Ring-style entity recycling.
    /// </summary>
    public class EnemyObjectPool : MonoBehaviour
    {
        static EnemyObjectPool _instance;
        public static EnemyObjectPool Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("[EnemyObjectPool]");
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<EnemyObjectPool>();
                }
                return _instance;
            }
        }

        // Separate pool per prefab to avoid type mismatch
        readonly Dictionary<GameObject, Queue<GameObject>> pools
            = new Dictionary<GameObject, Queue<GameObject>>();

        // Track which prefab each pooled object came from
        readonly Dictionary<GameObject, GameObject> prefabMap
            = new Dictionary<GameObject, GameObject>();

        [Tooltip("Default number of instances pre-warmed per prefab")]
        [SerializeField] int defaultWarmupCount = 4;

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }

        /// <summary>
        /// Pre-warm pool with a set number of inactive instances.
        /// Call from EnemySpawnRegion.Start() for zero-stutter first spawn.
        /// </summary>
        public void Warmup(GameObject prefab, int count = -1)
        {
            if (prefab == null) return;
            int warmCount = count < 0 ? defaultWarmupCount : count;

            if (!pools.ContainsKey(prefab))
                pools[prefab] = new Queue<GameObject>();

            var parent = transform;
            for (int i = 0; i < warmCount; i++)
            {
                var go = Instantiate(prefab, parent);
                go.SetActive(false);
                prefabMap[go] = prefab;
                pools[prefab].Enqueue(go);
            }
        }

        /// <summary>
        /// Get an enemy from pool (or instantiate if pool empty).
        /// Returns active GameObject at the requested position/rotation.
        /// </summary>
        public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null) return null;

            if (!pools.ContainsKey(prefab))
                pools[prefab] = new Queue<GameObject>();

            GameObject go;
            if (pools[prefab].Count > 0)
            {
                go = pools[prefab].Dequeue();

                // Handle destroyed entries (scene reload etc.)
                while (go == null && pools[prefab].Count > 0)
                    go = pools[prefab].Dequeue();

                if (go == null)
                    go = Instantiate(prefab);
            }
            else
            {
                go = Instantiate(prefab);
            }

            prefabMap[go] = prefab;
            go.transform.SetPositionAndRotation(position, rotation);
            go.SetActive(true);
            return go;
        }

        /// <summary>
        /// Return an enemy to pool instead of Destroy.
        /// Call this from EnemyHealth.OnDeath() or EnemySpawnRegion when clearing.
        /// </summary>
        public void Return(GameObject go)
        {
            if (go == null) return;

            go.SetActive(false);
            go.transform.SetParent(transform);

            // Reset NavMeshAgent velocity if present
            var agent = go.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.ResetPath();
                agent.velocity = Vector3.zero;
            }

            // Reset AI state
            var ai = go.GetComponent<EnemyAI>();
            if (ai != null)
                ai.ResetForPool();

            // Reset health state
            var health = go.GetComponent<EnemyHealth>();
            if (health != null)
                health.ResetHealth();

            // Reset damage feedback visuals
            var feedback = go.GetComponent<EnemyDamageFeedback>();
            if (feedback != null)
                feedback.ResetForPool();


            if (prefabMap.TryGetValue(go, out var prefab) && prefab != null)
            {
                if (!pools.ContainsKey(prefab))
                    pools[prefab] = new Queue<GameObject>();
                pools[prefab].Enqueue(go);
            }
            else
            {
                // Unknown origin — just destroy safely
                Destroy(go);
            }
        }

        /// <summary>
        /// Return all enemies in a list to the pool.
        /// </summary>
        public void ReturnAll(List<GameObject> enemies)
        {
            for (int i = enemies.Count - 1; i >= 0; i--)
                Return(enemies[i]);
            enemies.Clear();
        }

        void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }
    }
}
