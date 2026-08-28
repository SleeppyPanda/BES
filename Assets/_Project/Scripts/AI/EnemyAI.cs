using UnityEngine;
using UnityEngine.AI;

namespace BES.Gameplay
{
    public enum EnemyState
    {
        Idle,
        Chase,
        Attack
    }

    // ──────────────────────────────────────────────────────────────────────────
    // EnemyAI — Optimized for 60-120 FPS open-world performance
    // Techniques borrowed from Elden Ring, Genshin Impact:
    //   • AI Throttling   — AI logic runs 1x per N frames (staggered per enemy)
    //   • Component Cache — Animator resolved once, not every frame
    //   • Distance Culling — AI paused > 40 m, Renderer off > 80 m
    //   • CullCompletely  — Animator does NOT animate off-screen enemies
    // ──────────────────────────────────────────────────────────────────────────

    [RequireComponent(typeof(EnemyHealth))]
    public class EnemyAI : MonoBehaviour
    {
        [Header("Detection & Combat")]
        [SerializeField] float detectRange = 14f;
        [SerializeField] float attackRange = 2.5f;
        [SerializeField] float attackDamage = 10f;
        [SerializeField] float attackCooldown = 1.5f;
        [SerializeField] float patrolSpeed = 2.0f;
        [SerializeField] float chaseSpeed = 4.5f;

        [Header("Patrol Settings")]
        [SerializeField] float patrolRadius = 10f;
        [SerializeField] float patrolWaitMin = 1.5f;
        [SerializeField] float patrolWaitMax = 4f;

        NavMeshAgent agent;
        EnemyHealth health;
        Animator animator;
        Renderer[] cachedRenderers;       // Cached once for distance-culling
        Transform target;
        EnemyState state = EnemyState.Idle;

        /// <summary>True while enemy is in Attack state.
        /// EnemyInterestManager checks this before re-enabling NavMeshAgent
        /// to prevent mid-attack snap that launches the enemy into the air.</summary>
        public bool IsAttacking => state == EnemyState.Attack;
        float attackTimer;

        Vector3 spawnPosition;
        Vector3 patrolTarget;
        float patrolWaitTimer;
        bool hasPatrolTarget;
        [SerializeField] RuntimeAnimatorController defaultAnimatorController;
        bool isMoving;
        float currentMoveSpeed;

        // ── AI Throttling ────────────────────────────────────────────────────
        // Enemies update AI logic only every AI_FRAME_INTERVAL frames.
        // Each enemy is staggered by a unique offset so they don't all
        // recalculate on the same frame (Elden Ring batch-update technique).
        //
        // IMPORTANT: We use Time.frameCount (engine-level counter, free)
        // instead of a static counter that each instance increments.
        // Bug in old code: 15 enemies would increment the counter 15 times per
        // frame, completely breaking the stagger distribution.
        const int AI_FRAME_INTERVAL = 5;   // 20% CPU — was 3 (33%)
        int myFrameOffset;                  // Unique offset per instance

        // ── Distance Culling ─────────────────────────────────────────────────
        const float AI_PAUSE_DISTANCE       = 40f;   // AI goes idle beyond this
        const float RENDERER_CULL_DISTANCE  = 80f;   // Renderer disabled beyond this
        // Squared constants — avoids sqrt in hot distance checks every frame
        const float AI_PAUSE_SQR            = 40f * 40f;
        const float RENDERER_CULL_SQR       = 80f * 80f;
        bool renderersEnabled = true;

        // ── Target-search cooldown ───────────────────────────────────────────
        float findTargetTimer;              // Retry Find only every 2s, not every frame

        void Awake()
        {
            agent  = GetComponent<NavMeshAgent>();
            health = GetComponent<EnemyHealth>();

            // Cache Animator ONCE — never search each frame
            animator = GetComponentInChildren<Animator>();

            // Cache Renderers ONCE for distance-culling toggle
            cachedRenderers = GetComponentsInChildren<Renderer>(true);

            // Assign a unique stagger offset so all enemies don't
            // evaluate AI on the same frame (staggered batch update).
            // Use absolute value of InstanceID to guarantee positive modulo.
            myFrameOffset = Mathf.Abs(GetInstanceID()) % AI_FRAME_INTERVAL;

            EnsureAnimatorController();
        }

        public void EnsureAnimatorController()
        {
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            if (animator != null)
            {
                if (animator.runtimeAnimatorController == null)
                {
                    if (defaultAnimatorController != null)
                    {
                        animator.runtimeAnimatorController = defaultAnimatorController;
                    }
                    else
                    {
                        bool isBaby = gameObject.name.ToLower().Contains("baby") || (animator.avatar != null && !animator.avatar.isHuman);
                        string ctrlName = isBaby ? "Enemy_BabyMonster" : "Enemy_MeshyMonster";
                        var loaded = Resources.Load<RuntimeAnimatorController>(ctrlName);
                        if (loaded != null)
                        {
                            animator.runtimeAnimatorController = loaded;
                        }
                    }
                }

                // CullCompletely: animator does NOT tick when off-screen
                // This alone can save 10-20% CPU on scenes with many enemies
                animator.cullingMode = AnimatorCullingMode.CullCompletely;
                animator.applyRootMotion = false;
            }
        }

        public void SetAnimatorController(RuntimeAnimatorController controller)
        {
            defaultAnimatorController = controller;
            if (animator != null)
            {
                animator.runtimeAnimatorController = controller;
            }
        }

        public float PatrolRadius
        {
            get => patrolRadius;
            set => patrolRadius = Mathf.Max(0.1f, value);
        }

        public void Configure(float newDetectRange, float newAttackRange, float newAttackDamage, float newAttackCooldown, float newMoveSpeed)
        {
            detectRange = Mathf.Max(0.1f, newDetectRange);
            attackRange = Mathf.Max(0.1f, newAttackRange);
            attackDamage = Mathf.Max(0f, newAttackDamage);
            attackCooldown = Mathf.Max(0.1f, newAttackCooldown);
            chaseSpeed = Mathf.Max(0.1f, newMoveSpeed);
            patrolSpeed = Mathf.Max(0.1f, newMoveSpeed * 0.5f);

            if (agent != null)
            {
                agent.speed = chaseSpeed;
                agent.stoppingDistance = attackRange * 0.8f;
            }

            EnsureAnimatorController();
        }

        void Start()
        {
            spawnPosition = transform.position;

            if (agent != null && !agent.isOnNavMesh)
            {
                if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 30f, NavMesh.AllAreas))
                {
                    agent.Warp(hit.position);
                }
                else
                {
                    agent.enabled = false;
                    Debug.LogWarning($"[EnemyAI] {gameObject.name} failed to find NavMesh at {transform.position} within 30m. NavMeshAgent disabled.");
                }
            }

            FindPlayerTarget();

            // Register with the Interest Manager (WoW-style tier system)
            EnemyInterestManager.Register(this);
        }

        void OnDisable()
        {
            EnemyInterestManager.Unregister(this);
        }

        void OnDestroy()
        {
            EnemyInterestManager.Unregister(this);
        }

        void FindPlayerTarget()
        {
            // Do NOT call Find every frame — this is called from Update only
            // when target is null AND findTargetTimer has cooled down (every 2s)
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
                return;
            }
            // Fallback: search by component (more expensive, but rare)
            var stats = FindFirstObjectByType<PlayerStats>();
            if (stats != null)
                target = stats.transform;
        }

        void Update()
        {
            // ── Target search throttle ────────────────────────────────────────
            if (target == null)
            {
                findTargetTimer -= Time.deltaTime;
                if (findTargetTimer <= 0f)
                {
                    FindPlayerTarget();
                    findTargetTimer = 2f; // retry every 2s max, not every frame
                }
            }

            if (health != null && !health.IsAlive)
            {
                if (agent != null && agent.enabled && agent.isOnNavMesh)
                    agent.isStopped = true;
                if (animator != null)
                    animator.SetFloat("Speed", 0f);
                return;
            }

            // ── Distance-based culling (Elden Ring technique) ─────────────────
            // Use sqrMagnitude instead of Distance() to avoid sqrt — much cheaper
            float sqrDist = target != null
                ? (transform.position - target.position).sqrMagnitude
                : float.MaxValue;

            // Toggle Renderers beyond 80m (saves GPU fill rate)
            bool shouldRender = sqrDist <= RENDERER_CULL_SQR;
            if (shouldRender != renderersEnabled)
            {
                renderersEnabled = shouldRender;
                if (cachedRenderers != null)
                    foreach (var r in cachedRenderers) r.enabled = shouldRender;
            }

            // Pause AI entirely beyond 40m — zero CPU cost for distant enemies
            if (sqrDist > AI_PAUSE_SQR)
            {
                if (agent != null && agent.enabled && agent.isOnNavMesh)
                    agent.isStopped = true;
                if (animator != null)
                    animator.SetFloat("Speed", 0f);
                return;
            }

            // ── AI Throttling — Elden Ring staggered batch update ─────────────
            // Time.frameCount is a free engine property — zero cost, no shared mutation.
            // Previously used a static counter incremented by EACH enemy → bug:
            // with 15 enemies, counter jumped +15 per frame, breaking stagger distribution.
            bool myAIFrame = (Time.frameCount % AI_FRAME_INTERVAL) == myFrameOffset;

            // Always update timers (cheap — needed every frame for accuracy)
            if (attackTimer > 0f)    attackTimer    -= Time.deltaTime;
            if (patrolWaitTimer > 0f) patrolWaitTimer -= Time.deltaTime;

            if (!myAIFrame)
            {
                // Non-AI frame: only update animator to keep movement smooth
                UpdateAnimatorSpeed();
                return;
            }

            isMoving = false;
            currentMoveSpeed = 0f;

            // Pre-compute squared thresholds for state machine (avoids sqrt per branch)
            float sqrDetect      = detectRange * detectRange;
            float sqrAttack      = attackRange * attackRange;
            float sqrLeash       = (detectRange * 1.4f) * (detectRange * 1.4f);
            float sqrTooClose    = 1.4f * 1.4f;
            float sqrLeashAttack = (attackRange * 1.3f) * (attackRange * 1.3f);

            switch (state)
            {
                case EnemyState.Idle:
                    if (sqrDist <= sqrDetect)
                    {
                        state = EnemyState.Chase;
                        hasPatrolTarget = false;
                        if (agent != null && agent.enabled && agent.isOnNavMesh)
                            agent.ResetPath();
                    }
                    else
                    {
                        HandlePatrol();
                    }
                    break;

                case EnemyState.Chase:
                    if (target == null)
                    {
                        state = EnemyState.Idle;
                        break;
                    }

                    if (sqrDist <= sqrAttack)
                    {
                        state = EnemyState.Attack;
                        if (agent != null && agent.enabled && agent.isOnNavMesh)
                            agent.ResetPath();
                    }
                    else if (sqrDist > sqrLeash)
                    {
                        state = EnemyState.Idle;
                        patrolWaitTimer = 1f;
                        hasPatrolTarget = false;
                        if (agent != null && agent.enabled && agent.isOnNavMesh)
                            agent.ResetPath();
                    }
                    else
                    {
                        isMoving = true;
                        currentMoveSpeed = chaseSpeed;
                        MoveToDestination(target.position, chaseSpeed);
                    }
                    break;

                case EnemyState.Attack:
                    if (target == null)
                    {
                        state = EnemyState.Idle;
                        break;
                    }

                    // Stop moving when in attack range — let NavMesh stoppingDistance handle separation.
                    // DO NOT use agent.Move() for push-away: it bypasses NavMesh vertical constraints
                    // and causes enemies to be launched into the air.
                    if (agent != null && agent.enabled && agent.isOnNavMesh)
                    {
                        agent.isStopped = true;
                        agent.velocity  = Vector3.zero;
                    }

                    // Face target horizontally (Y-zeroed to prevent tilting)
                    var lookPos = target.position - transform.position;
                    lookPos.y = 0f;
                    if (lookPos.sqrMagnitude > 0.001f)
                    {
                        transform.rotation = Quaternion.Slerp(
                            transform.rotation,
                            Quaternion.LookRotation(lookPos),
                            Time.deltaTime * 10f);
                    }

                    if (attackTimer <= 0f)
                    {
                        TryAttackTarget();
                        if (animator != null)
                        {
                            animator.SetTrigger("Attack");
                        }
                        attackTimer = attackCooldown;
                    }

                    if (sqrDist > sqrLeashAttack)
                    {
                        state = EnemyState.Chase;
                    }
                    break;
            }

            UpdateAnimatorSpeed();
        }

        void UpdateAnimatorSpeed()
        {
            if (animator == null || !animator.isActiveAndEnabled || animator.runtimeAnimatorController == null)
                return;

            float calculatedSpeed = 0f;
            if (isMoving)
            {
                float agentSpeed = (agent != null && agent.enabled && agent.isOnNavMesh)
                    ? agent.velocity.magnitude : 0f;
                calculatedSpeed = agentSpeed > 0.1f ? agentSpeed : currentMoveSpeed;
            }
            animator.SetFloat("Speed", calculatedSpeed);
        }

        void HandlePatrol()
        {
            if (!hasPatrolTarget)
            {
                if (patrolWaitTimer <= 0f)
                {
                    Vector2 randomCircle = Random.insideUnitCircle * patrolRadius;
                    patrolTarget = spawnPosition + new Vector3(randomCircle.x, 0f, randomCircle.y);
                    hasPatrolTarget = true;
                }
            }
            else
            {
                isMoving = true;
                currentMoveSpeed = patrolSpeed;
                MoveToDestination(patrolTarget, patrolSpeed);

                float distToTarget = Vector3.Distance(new Vector3(transform.position.x, 0f, transform.position.z), 
                                                      new Vector3(patrolTarget.x, 0f, patrolTarget.z));
                bool reachedByAgent = agent != null && agent.enabled && agent.isOnNavMesh && !agent.pathPending && agent.remainingDistance <= (agent.stoppingDistance + 0.3f);

                if (distToTarget < 1.2f || reachedByAgent)
                {
                    hasPatrolTarget = false;
                    patrolWaitTimer = Random.Range(patrolWaitMin, patrolWaitMax);
                    if (agent != null && agent.enabled && agent.isOnNavMesh)
                        agent.ResetPath();
                }
            }
        }

        void MoveToDestination(Vector3 destination, float speed)
        {
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.speed = speed;
                agent.SetDestination(destination);
                return;
            }

            // Move horizontally on ground plane without raycast height jitter
            var dir = destination - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f)
                return;

            Vector3 step = dir.normalized * (speed * Time.deltaTime);
            transform.position += step;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir.normalized), Time.deltaTime * 10f);
        }

        void TryAttackTarget()
        {
            if (target == null) return;

            if (target.TryGetComponent<DodgeController>(out var dodge) && dodge.IsInvincible)
                return;

            if (target.TryGetComponent<PlayerStats>(out var stats))
                stats.TakeDamage(attackDamage);
        }

        /// <summary>
        /// Reset state when returned to EnemyObjectPool.
        /// Called by EnemyObjectPool.Return() before re-queuing.
        /// </summary>
        public void ResetForPool()
        {
            state           = EnemyState.Idle;
            attackTimer     = 0f;
            patrolWaitTimer = 0f;
            hasPatrolTarget = false;
            isMoving        = false;
            currentMoveSpeed = 0f;
            findTargetTimer = 0f;

            // Re-enable renderers so enemy is visible on next spawn
            renderersEnabled = true;
            if (cachedRenderers != null)
                foreach (var r in cachedRenderers) r.enabled = true;

            if (animator != null && animator.runtimeAnimatorController != null)
                animator.SetFloat("Speed", 0f);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectRange);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(spawnPosition != Vector3.zero ? spawnPosition : transform.position, patrolRadius);

            Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, AI_PAUSE_DISTANCE);

            Gizmos.color = new Color(0.3f, 0.3f, 1f, 0.15f);
            Gizmos.DrawWireSphere(transform.position, RENDERER_CULL_DISTANCE);
        }
    }
}
