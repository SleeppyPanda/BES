using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace BES.Gameplay
{
    /// <summary>
    /// WoW-style Interest Management for enemy AI.
    ///
    /// Divides all active enemies into 3 activity tiers based on distance to player.
    /// Runs the evaluation loop every 0.5 seconds (NOT every frame) — much cheaper
    /// than having each enemy poll its own distance independently.
    ///
    /// ┌──────────────────────────────────────────────────────────┐
    /// │  Tier       │ Distance │ NavMesh │ Animator │ AI Logic  │
    /// ├─────────────┼──────────┼─────────┼──────────┼───────────┤
    /// │  Active     │ < 25m    │ Full    │ On       │ Every 5f  │
    /// │  Sleeping   │ 25–50m   │ Stopped │ Cull     │ Every 15f │
    /// │  Frozen     │ > 50m    │ Off     │ Off      │ Never     │
    /// └──────────────────────────────────────────────────────────┘
    ///
    /// Technique references:
    ///   World of Warcraft — Interest Management, visibility zones
    ///   Assassin's Creed  — NPC activity budget, crowd sim distance tiers
    ///   Horizon ZD        — Machine activity sleep/wake at LOD boundaries
    ///
    /// Auto-instantiated by GameplaySceneBootstrap.
    /// </summary>
    public class EnemyInterestManager : MonoBehaviour
    {
        public static EnemyInterestManager Instance { get; private set; }

        [Header("Interest Zone Radii")]
        [Tooltip("Enemies within this radius run at full AI speed")]
        [SerializeField] float activeRadius   = 25f;
        [Tooltip("Enemies between activeRadius and this radius are in sleep mode (throttled)")]
        [SerializeField] float sleepRadius    = 50f;
        // Enemies beyond sleepRadius = Frozen tier (no field needed, comment only)

        [Header("Evaluation")]
        [Tooltip("How often (seconds) the interest zones are re-evaluated. 0.5s is ideal.")]
        [SerializeField] float evaluationInterval = 0.5f;
        [Tooltip("AI frame interval for ACTIVE tier (same as EnemyAI.AI_FRAME_INTERVAL)")]
        [SerializeField] int activeAIInterval  = 5;
        [Tooltip("AI frame interval for SLEEPING tier — much more throttled")]
        [SerializeField] int sleepAIInterval   = 15;

        // Squared radii for fast sqrMagnitude comparisons (avoids sqrt)
        float sqrActive;
        float sqrSleep;

        // Registry of all enemies in the scene
        static readonly List<EnemyRegistration> registrations = new List<EnemyRegistration>(64);

        Transform playerTransform;
        float evalTimer;

        // ── Enemy Registration ────────────────────────────────────────────────
        struct EnemyRegistration
        {
            public EnemyAI        ai;
            public NavMeshAgent   agent;
            public Animator       animator;
            public Renderer[]     renderers;
            public EnemyTier      tier;
        }

        enum EnemyTier { Unknown, Active, Sleeping, Frozen }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;

            sqrActive = activeRadius * activeRadius;
            sqrSleep  = sleepRadius  * sleepRadius;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ── Enemy Self-Registration ───────────────────────────────────────────
        // Called by EnemyAI.Start() to register with the interest manager.
        public static void Register(EnemyAI ai)
        {
            if (ai == null) return;
            // Avoid duplicate registration
            for (int i = 0; i < registrations.Count; i++)
                if (registrations[i].ai == ai) return;

            registrations.Add(new EnemyRegistration
            {
                ai        = ai,
                agent     = ai.GetComponent<NavMeshAgent>(),
                animator  = ai.GetComponentInChildren<Animator>(),
                renderers = ai.GetComponentsInChildren<Renderer>(true),
                tier      = EnemyTier.Unknown
            });
        }

        // Called by EnemyAI when returned to pool or destroyed.
        public static void Unregister(EnemyAI ai)
        {
            if (ai == null) return;
            for (int i = registrations.Count - 1; i >= 0; i--)
                if (registrations[i].ai == ai) { registrations.RemoveAt(i); return; }
        }

        // ── Main Evaluation Loop ──────────────────────────────────────────────
        void Update()
        {
            // Find player lazily
            if (playerTransform == null)
            {
                var player = GameObject.FindWithTag("Player");
                if (player != null) playerTransform = player.transform;
                else return;
            }

            evalTimer += Time.deltaTime;
            if (evalTimer < evaluationInterval) return;
            evalTimer = 0f;

            Vector3 playerPos = playerTransform.position;

            for (int i = registrations.Count - 1; i >= 0; i--)
            {
                var reg = registrations[i];

                // Remove null entries (destroyed enemies not properly unregistered)
                if (reg.ai == null || !reg.ai.gameObject.activeSelf)
                {
                    registrations.RemoveAt(i);
                    continue;
                }

                float sqrDist = (reg.ai.transform.position - playerPos).sqrMagnitude;
                EnemyTier newTier;

                if      (sqrDist <= sqrActive) newTier = EnemyTier.Active;
                else if (sqrDist <= sqrSleep)  newTier = EnemyTier.Sleeping;
                else                           newTier = EnemyTier.Frozen;

                // Only apply changes on tier transition to avoid per-frame overhead
                if (newTier == reg.tier) continue;

                ApplyTier(ref reg, newTier);

                // Write back the modified struct (List<struct> requires explicit update)
                registrations[i] = reg;
            }
        }

        void ApplyTier(ref EnemyRegistration reg, EnemyTier newTier)
        {
            reg.tier = newTier;

            switch (newTier)
            {
                // ── ACTIVE: Full AI, full animation ──────────────────────────
                case EnemyTier.Active:
                    SetNavMeshEnabled(ref reg, true);
                    SetAnimatorCulling(ref reg, AnimatorCullingMode.CullCompletely);
                    SetRenderersEnabled(ref reg, true);
                    // Note: AI interval is controlled via Time.frameCount in EnemyAI itself
                    break;

                // ── SLEEPING: NavMesh stopped, animation culled, slow AI ──────
                case EnemyTier.Sleeping:
                    // Stop movement but keep agent active (cheaper to re-enable)
                    if (reg.agent != null && reg.agent.isOnNavMesh)
                    {
                        reg.agent.isStopped = true;
                        reg.agent.ResetPath();
                    }
                    SetAnimatorCulling(ref reg, AnimatorCullingMode.CullCompletely);
                    // Keep renderers enabled so enemy is visible in the distance
                    SetRenderersEnabled(ref reg, true);
                    break;

                // ── FROZEN: Disable NavMesh, renderers, all AI ────────────────
                case EnemyTier.Frozen:
                    if (reg.agent != null && reg.agent.enabled && reg.agent.isOnNavMesh)
                    {
                        reg.agent.isStopped = true;
                        reg.agent.ResetPath();
                        reg.agent.velocity = Vector3.zero;
                    }
                    SetAnimatorCulling(ref reg, AnimatorCullingMode.CullCompletely);
                    SetRenderersEnabled(ref reg, false);
                    break;
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        static void SetNavMeshEnabled(ref EnemyRegistration reg, bool enabled)
        {
            if (reg.agent == null) return;
            if (enabled)
            {
                // IMPORTANT: Do NOT re-enable agent while enemy is attacking.
                // The Attack state explicitly sets isStopped=true. If we override
                // that here, the NavMesh snaps the agent's position → launches enemy into air.
                if (reg.ai != null && reg.ai.IsAttacking) return;
                reg.agent.isStopped = false;
            }
            else
            {
                if (reg.agent.isOnNavMesh)
                {
                    reg.agent.isStopped = true;
                    reg.agent.velocity  = Vector3.zero;
                }
            }
        }

        static void SetAnimatorCulling(ref EnemyRegistration reg, AnimatorCullingMode mode)
        {
            if (reg.animator != null)
                reg.animator.cullingMode = mode;
        }

        static void SetRenderersEnabled(ref EnemyRegistration reg, bool enabled)
        {
            if (reg.renderers == null) return;
            foreach (var r in reg.renderers)
                if (r != null) r.enabled = enabled;
        }

        // ── Gizmo Visualization ───────────────────────────────────────────────
        void OnDrawGizmosSelected()
        {
            var pos = playerTransform != null ? playerTransform.position : transform.position;

            Gizmos.color = new Color(0f, 1f, 0f, 0.08f);
            Gizmos.DrawWireSphere(pos, activeRadius);

            Gizmos.color = new Color(1f, 1f, 0f, 0.05f);
            Gizmos.DrawWireSphere(pos, sleepRadius);
        }
    }
}
