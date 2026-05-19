using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using EnemyAI;

namespace EnemySystem
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class EnemyMovement : MonoBehaviour
    {
        [Header("Speed Settings")]
        
        [SerializeField] float basePlayerVelocity = 4f;

        [SerializeField] float patrolSpeedMultiplier = 1f;
        
        [SerializeField] float baseAngularSpeed = 120f;

        [SerializeField] float combatAngularSpeed = 360f;

        [Header("Patrol Settings")] [SerializeField]
        Transform[] patrolPoints;

        [SerializeField] float waitTimeAtWaypoint = 2f;

        NavMeshAgent agent;
        EnemySensors sensors;
        AIAnimationController  animationController;

        int currentPatrolIndex = 0;
        bool isWaiting = false;
        
        [SerializeField] internal float agentStopDistance = 1f;

        Quaternion originalRotation; // Used when there is only one patrol point so enemy doesn't drift

        void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            sensors = GetComponent<EnemySensors>();
            animationController = GetComponent<AIAnimationController>();
        }

        void Start()
        {
            originalRotation = transform.rotation;
            ResumeDefaultMovement();
            if (patrolPoints.Length == 0)
            {
                GameObject point = new GameObject("patrolPoint");
                point.transform.position = transform.position;
                point.transform.parent = transform.parent;
                patrolPoints = new[] { point.transform };
            }
        }

        internal void SetSpeedMultiplier(float multiplier)
        {
            agent.speed = basePlayerVelocity * multiplier;
        }

        internal void UpdateAngularSpeed(AICore.AIState state)
        {
            agent.angularSpeed = state switch
            {
                AICore.AIState.Combat => combatAngularSpeed,
                _ => baseAngularSpeed
            };
        }

        // --- Patrol Logic ---
        internal void UpdatePatrolState()
        {
            UpdateAngularSpeed(AICore.AIState.Patrol);
            if (patrolPoints.Length == 0) return;
            if (!agent.pathPending && agent.remainingDistance < 0.5f && !isWaiting)
            {
                StartCoroutine(PatrolWaitRoutine());
            }
        }

        void GoToNextPatrolPoint()
        {
            if (patrolPoints.Length == 0) return;
            agent.destination = patrolPoints[currentPatrolIndex].position;
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
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
            animationController.AgentLooking(false);
        }

        internal void StartLookAround(float duration, AICore brain)
        {
            StartCoroutine(LookAroundRoutine(duration, brain));
        }

        internal void StartInvestigate(float duration, Vector3 targetPos, AICore brain)
        {
            StartCoroutine(InvestigateRoutine(duration, targetPos, brain));
        }

        IEnumerator PatrolWaitRoutine()
        {
            isWaiting = true;
            agent.isStopped = true;
            animationController.AgentLooking(true);
            yield return StartCoroutine(SweepRotationRoutine(waitTimeAtWaypoint, false, 30));
            animationController.AgentLooking(false);
            agent.isStopped = false;
            GoToNextPatrolPoint();
            if (patrolPoints.Length <= 1) yield return StartCoroutine(FixRotationRoutine(originalRotation, waitTimeAtWaypoint));
            isWaiting = false;
        }

        IEnumerator LookAroundRoutine(float duration, AICore brain)
        {
            agent.isStopped = true;
            yield return StartCoroutine(SweepRotationRoutine(duration, sensors.HasLineOfSight()));
            agent.isStopped = false;
            brain.ChangeState(AICore.AIState.Patrol);
        }

        IEnumerator InvestigateRoutine(float duration, Vector3 targetPos, AICore brain)
        {
            agent.isStopped = false;
            agent.stoppingDistance = agentStopDistance;
            agent.SetDestination(targetPos);

            while (agent.pathPending || agent.remainingDistance > agentStopDistance) yield return null;

            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance) agent.velocity = Vector3.zero;
            
            agent.isStopped = true;
            yield return StartCoroutine(SweepRotationRoutine(duration, false, 70f));

            agent.isStopped = false;
            brain.DetermineAlertLevel(0.2f);
        }

        IEnumerator SweepRotationRoutine(float duration, bool trackLastKnownPosition, float lookAngle = 70f)
        {
            float timer = 0f;
            bool lookingLeft = true;
            Quaternion centerRotation = transform.rotation;

            while (timer < duration)
            {
                if (trackLastKnownPosition)
                {
                    Vector3 direction = (sensors.LastKnownPosition - transform.position).normalized;
                    if (direction != Vector3.zero)
                        centerRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));

                    if (sensors.HasLineOfSight())
                    {
                        timer = 0f;
                        transform.rotation = Quaternion.RotateTowards(transform.rotation, centerRotation,
                            agent.angularSpeed * Time.deltaTime);
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
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetSweep,
                    agent.angularSpeed * Time.deltaTime / 2f);

                if (Quaternion.Angle(transform.rotation, targetSweep) < 1f) lookingLeft = !lookingLeft;
                yield return null;
            }
        }

        IEnumerator FixRotationRoutine(Quaternion targetRotation, float duration)
        {
            float timer = 0f;

            while (timer < duration)
            {
                timer += Time.deltaTime;
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation,
                    agent.angularSpeed * Time.deltaTime / 2f);
                
                yield return null;
            }
        }
    }
}