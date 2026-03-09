using UnityEngine;
using UnityEngine.AI;
using System.Collections;

namespace EnemySystem
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class EnemyMovement : MonoBehaviour
    {
        [Header("Speed Settings")]
        [SerializeField] float basePlayerVelocity = 4f;
        [SerializeField] float patrolSpeedMultiplier = 1f;
        [SerializeField] float combatSpeedMultiplier = 1.3f;
        [SerializeField] float retreatSpeedMultiplier = 0.3f;

        [Header("Patrol Settings")]
        [SerializeField] Transform[] patrolPoints;
        [SerializeField] float waitTimeAtWaypoint = 2f;

        private NavMeshAgent agent;
        private EnemySensors sensors;

        private int currentPatrolIndex = 0;
        private bool isWaiting = false;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            sensors = GetComponent<EnemySensors>();
        }

        private void Start()
        {
            ResumeDefaultMovement();
        }

        internal void SetSpeedMultiplier(float multiplier)
        {
            agent.speed = basePlayerVelocity * multiplier;
        }

        // --- Patrol Logic ---
        internal void UpdatePatrolState()
        {
            if (patrolPoints.Length == 0) return;
            if (!agent.pathPending && agent.remainingDistance < 0.5f && !isWaiting)
            {
                StartCoroutine(PatrolWaitRoutine());
            }
        }

        private void GoToNextPatrolPoint()
        {
            if (patrolPoints.Length == 0) return;
            agent.destination = patrolPoints[currentPatrolIndex].position;
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
        }

        // --- Combat Logic ---
        internal void HandleCombatMovement(Transform playerTransform, float distanceToPlayer, float weaponRange, float optimalDistance, bool hasLOS)
        {
            SetSpeedMultiplier(combatSpeedMultiplier);

            Vector3 direction = (playerTransform.position - transform.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
            transform.rotation = Quaternion.RotateTowards(transform.rotation, lookRotation, agent.angularSpeed * Time.deltaTime);

            if (distanceToPlayer <= weaponRange && hasLOS)
            {
                if (distanceToPlayer <= optimalDistance)
                {
                    SetSpeedMultiplier(retreatSpeedMultiplier);
                    Vector3 retreatDirection = transform.position - playerTransform.position;
                    agent.SetDestination(transform.position + retreatDirection.normalized * 2f);
                }
                else
                {
                    agent.SetDestination(transform.position); // Stop and shoot
                }
            }
            else
            {
                agent.SetDestination(playerTransform.position); // Chase
            }
        }

        // --- Coroutines & State Handlers ---
        internal void ResumeDefaultMovement()
        {
            SetSpeedMultiplier(patrolSpeedMultiplier);
            agent.isStopped = false;
            isWaiting = false;
            GoToNextPatrolPoint();
        }

        internal void StopAllMovementCoroutines()
        {
            StopAllCoroutines();
            isWaiting = false;
        }

        internal void StartLookAround(float duration, AICore brain)
        {
            StartCoroutine(LookAroundRoutine(duration, brain));
        }

        internal void StartInvestigate(float duration, Vector3 targetPos, AICore brain)
        {
            StartCoroutine(InvestigateRoutine(duration, targetPos, brain));
        }

        private IEnumerator PatrolWaitRoutine()
        {
            isWaiting = true;
            agent.isStopped = true;
            yield return StartCoroutine(SweepRotationRoutine(waitTimeAtWaypoint, false, 40));
            agent.isStopped = false;
            GoToNextPatrolPoint();
            isWaiting = false;
        }

        private IEnumerator LookAroundRoutine(float duration, AICore brain)
        {
            agent.isStopped = true;
            yield return StartCoroutine(SweepRotationRoutine(duration, true));
            agent.isStopped = false;
            brain.ChangeState(AICore.AIState.Patrol);
        }

        private IEnumerator InvestigateRoutine(float duration, Vector3 targetPos, AICore brain)
        {
            agent.isStopped = false;
            agent.SetDestination(targetPos);

            while (agent.pathPending || agent.remainingDistance > 0.5f) yield return null;

            agent.isStopped = true;
            yield return StartCoroutine(SweepRotationRoutine(duration, false));

            agent.isStopped = false;
            brain.ChangeState(AICore.AIState.Patrol);
        }

        private IEnumerator SweepRotationRoutine(float duration, bool trackLastKnownPosition, float lookAngle = 70f)
        {
            float timer = 0f;
            bool lookingLeft = true;
            Quaternion centerRotation = transform.rotation;

            while (timer < duration)
            {
                if (trackLastKnownPosition)
                {
                    Vector3 direction = (sensors.LastKnownPosition - transform.position).normalized;
                    if (direction != Vector3.zero) centerRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));

                    if (sensors.HasLineOfSight())
                    {
                        timer = 0f;
                        transform.rotation = Quaternion.RotateTowards(transform.rotation, centerRotation, agent.angularSpeed * Time.deltaTime);
                        yield return null;
                        continue;
                    }
                }
                else
                {
                    if (sensors.HasLineOfSight()) break;
                }

                timer += Time.deltaTime;
                Quaternion targetSweep = centerRotation * Quaternion.Euler(0, lookingLeft ? -lookAngle : lookAngle, 0);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetSweep, agent.angularSpeed * Time.deltaTime);

                if (Quaternion.Angle(transform.rotation, targetSweep) < 1f) lookingLeft = !lookingLeft;
                yield return null;
            }
        }
    }
}