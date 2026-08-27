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
        Transform target;
        EnemyState state = EnemyState.Idle;
        float attackTimer;

        Vector3 spawnPosition;
        Vector3 patrolTarget;
        float patrolWaitTimer;
        bool hasPatrolTarget;
        [SerializeField] RuntimeAnimatorController defaultAnimatorController;
        bool isMoving;
        float currentMoveSpeed;

        void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            health = GetComponent<EnemyHealth>();
            animator = GetComponentInChildren<Animator>();

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
                        var loaded = Resources.Load<RuntimeAnimatorController>("Enemy_MeshyMonster");
                        if (loaded != null)
                        {
                            animator.runtimeAnimatorController = loaded;
                        }
                    }
                }

                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.applyRootMotion = false;
            }
        }

        public void SetAnimatorController(RuntimeAnimatorController controller)
        {
            defaultAnimatorController = controller;
            if (animator != null && animator.runtimeAnimatorController == null)
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
        }

        void FindPlayerTarget()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
            }
            else
            {
                var stats = FindFirstObjectByType<PlayerStats>();
                if (stats != null)
                    target = stats.transform;
            }
        }

        void Update()
        {
            if (target == null)
            {
                FindPlayerTarget();
            }

            if (health != null && !health.IsAlive)
            {
                if (agent != null && agent.enabled && agent.isOnNavMesh)
                    agent.isStopped = true;
                if (animator != null)
                    animator.SetFloat("Speed", 0f);
                return;
            }

            // Update timers
            if (attackTimer > 0f)
                attackTimer -= Time.deltaTime;

            if (patrolWaitTimer > 0f)
                patrolWaitTimer -= Time.deltaTime;

            isMoving = false;
            currentMoveSpeed = 0f;
            float distanceToPlayer = target != null ? Vector3.Distance(transform.position, target.position) : 9999f;

            switch (state)
            {
                case EnemyState.Idle:
                    if (distanceToPlayer <= detectRange)
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

                    if (distanceToPlayer <= attackRange)
                    {
                        state = EnemyState.Attack;
                        if (agent != null && agent.enabled && agent.isOnNavMesh)
                            agent.ResetPath();
                    }
                    else if (distanceToPlayer > detectRange * 1.4f)
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

                    // Face target horizontally
                    var lookPos = target.position - transform.position;
                    lookPos.y = 0f;
                    if (lookPos.sqrMagnitude > 0.001f)
                    {
                        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookPos), Time.deltaTime * 10f);
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

                    if (distanceToPlayer > attackRange * 1.3f)
                    {
                        state = EnemyState.Chase;
                    }
                    break;
            }

            // Update Animator Speed parameter
            if (animator != null && animator.isActiveAndEnabled && animator.runtimeAnimatorController != null)
            {
                float calculatedSpeed = 0f;
                if (agent != null && agent.enabled && agent.isOnNavMesh)
                {
                    calculatedSpeed = agent.velocity.magnitude;
                }
                else if (isMoving)
                {
                    calculatedSpeed = currentMoveSpeed;
                }

                animator.SetFloat("Speed", calculatedSpeed);
            }
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

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectRange);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(spawnPosition != Vector3.zero ? spawnPosition : transform.position, patrolRadius);
        }
    }
}
