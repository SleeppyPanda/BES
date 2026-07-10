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
        [SerializeField] float detectRange = 12f;
        [SerializeField] float attackRange = 2f;
        [SerializeField] float attackDamage = 8f;
        [SerializeField] float attackCooldown = 1.5f;
        [SerializeField] float moveSpeed = 3.5f;

        NavMeshAgent agent;
        EnemyHealth health;
        Transform target;
        EnemyState state = EnemyState.Idle;
        float attackTimer;

        void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            health = GetComponent<EnemyHealth>();
        }

        void Start()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                target = player.transform;
        }

        void Update()
        {
            if (target == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                    target = player.transform;
            }

            if (!health.IsAlive || target == null)
                return;

            var distance = Vector3.Distance(transform.position, target.position);

            switch (state)
            {
                case EnemyState.Idle:
                    if (distance <= detectRange)
                        state = EnemyState.Chase;
                    break;

                case EnemyState.Chase:
                    MoveTowards(target.position);
                    if (distance <= attackRange)
                        state = EnemyState.Attack;
                    else if (distance > detectRange * 1.2f)
                        state = EnemyState.Idle;
                    break;

                case EnemyState.Attack:
                    if (agent != null && agent.isOnNavMesh)
                        agent.ResetPath();
                    transform.LookAt(new Vector3(target.position.x, transform.position.y, target.position.z));
                    if (attackTimer <= 0f)
                    {
                        TryAttackTarget();
                        attackTimer = attackCooldown;
                    }
                    if (distance > attackRange * 1.2f)
                        state = EnemyState.Chase;
                    break;
            }

            if (attackTimer > 0f)
                attackTimer -= Time.deltaTime;
        }

        void MoveTowards(Vector3 destination)
        {
            if (agent != null && agent.isOnNavMesh)
            {
                agent.SetDestination(destination);
                return;
            }

            var dir = (destination - transform.position);
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f)
                return;

            transform.position += dir.normalized * (moveSpeed * Time.deltaTime);
            transform.rotation = Quaternion.LookRotation(dir.normalized);
        }

        void TryAttackTarget()
        {
            if (target.TryGetComponent<DodgeController>(out var dodge) && dodge.IsInvincible)
                return;

            if (target.TryGetComponent<PlayerStats>(out var stats))
                stats.TakeDamage(attackDamage);
        }
    }
}
