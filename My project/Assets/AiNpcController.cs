using UnityEngine;
using UnityEngine.AI;
using Fusion;

public enum NpcRole { Civilian, Police }

[RequireComponent(typeof(NavMeshAgent))]
public class AiNpcController : NetworkBehaviour
{
    [Header("NPC Settings")]
    public NpcRole npcRole = NpcRole.Civilian;
    public float walkSpeed = 3.5f;
    public float chaseSpeed = 5.5f;

    [Header("Patrol Settings")]
    public Transform[] patrolPoints = new Transform[3]; // ?????? 3 ????? Inspector
    private int currentPointIndex = 0;

    [Header("Vision Settings")]
    public float viewDistance = 15f;
    [Range(0, 360)] public float viewAngle = 90f;
    public LayerMask wallLayer; // ????? Layer ????????????/???????????
    public LayerMask playerLayer; // ????? Layer ??? Player ???????????????

    [Header("Combat Settings")]
    public int attackDamage = 15;
    public float attackRange = 2f;
    public float attackRate = 1.5f;
    private float nextAttackTime = 0f;

    private NavMeshAgent agent;
    private Transform targetPlayer;
    private PlayerStatus targetPlayerStatus;

    public override void Spawned()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = walkSpeed;

        // ??????????????? (????? State Authority ??????????)
        if (Object.HasStateAuthority && patrolPoints.Length >= 3)
        {
            GotoNextPoint();
        }
    }

    public override void FixedUpdateNetwork()
    {
        // ?? Photon Fusion ????????????? AI Logic ??????? State Authority (Server/Host)
        if (!Object.HasStateAuthority) return;

        FindVisiblePlayer();

        if (targetPlayer != null && targetPlayerStatus != null && !targetPlayerStatus.IsDead)
        {
            // ??????? Police ??????????????
            if (npcRole == NpcRole.Police)
            {
                AttackBehavior();
            }
        }
        else
        {
            // ???????????? ??????????????? ???????????????????????????
            PatrolBehavior();
        }
    }

    // ---??????????????? (Patrol)---
    private void PatrolBehavior()
    {
        agent.speed = walkSpeed;

        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            GotoNextPoint();
        }
    }

    private void GotoNextPoint()
    {
        if (patrolPoints.Length == 0) return;

        agent.destination = patrolPoints[currentPointIndex].position;
        currentPointIndex = (currentPointIndex + 1) % patrolPoints.Length;
    }

    // ---????????? (Vision)---
    private void FindVisiblePlayer()
    {
        // ????? Object ?????????????????
        Collider[] targetsInMinusRadius = Physics.OverlapSphere(transform.position, viewDistance, playerLayer);

        bool foundPlayer = false;

        foreach (var targetCollider in targetsInMinusRadius)
        {
            // ??????? Tag "Player" ??????????
            if (targetCollider.CompareTag("Player"))
            {
                Transform player = targetCollider.transform;
                Vector3 directionToPlayer = (player.position - transform.position).normalized;

                // 1. ????????????? (Angle)
                if (Vector3.Angle(transform.forward, directionToPlayer) < viewAngle / 2)
                {
                    float distanceToPlayer = Vector3.Distance(transform.position, player.position);

                    // 2. ??? Raycast ????????????????? (Wall) ???????
                    if (!Physics.Raycast(transform.position + Vector3.up, directionToPlayer, distanceToPlayer, wallLayer))
                    {
                        // ?????????????????? ?????????? Player ???????????????
                        Debug.Log($"[NPC Log] ??????????????: {player.name} ????!");

                        targetPlayer = player;
                        targetPlayerStatus = player.GetComponent<PlayerStatus>();
                        foundPlayer = true;
                        break; // ???????????????????????
                    }
                }
            }
        }

        // ???????????????? ??????????????????
        if (!foundPlayer)
        {
            targetPlayer = null;
            targetPlayerStatus = null;
        }
    }

    // ---????????? (?????? Role Police)---
    private void AttackBehavior()
    {
        agent.speed = chaseSpeed;
        agent.destination = targetPlayer.position;

        float distance = Vector3.Distance(transform.position, targetPlayer.position);

        if (distance <= attackRange)
        {
            // ??????????????????????????
            agent.isStopped = true;

            if (Runner.SimulationTime >= nextAttackTime)
            {
                nextAttackTime = Runner.SimulationTime + attackRate;

                // ???????????????????????? (????????? State Authority ???????? ????????????????????????????)
                targetPlayerStatus.TakeDamage(attackDamage);
                Debug.Log($"[NPC Police] Attacked Player for {attackDamage} damage.");
            }
        }
        else
        {
            agent.isStopped = false;
        }
    }

    // ????????????????????? Scene (???????? NPC ????????????????????)
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, viewDistance);

        Vector3 viewAngleA = Quaternion.AngleAxis(-viewAngle / 2, Vector3.up) * transform.forward;
        Vector3 viewAngleB = Quaternion.AngleAxis(viewAngle / 2, Vector3.up) * transform.forward;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + viewAngleA * viewDistance);
        Gizmos.DrawLine(transform.position, transform.position + viewAngleB * viewDistance);
    }
}