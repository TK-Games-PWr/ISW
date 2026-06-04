using UnityEngine;
using UnityEngine.AI;
using System.Collections;

namespace EnemySystem
{
    public class EnemyMovement
    {
        MovementConfig _config;
        NavMeshAgent _agent;
        EnemySensors _sensors;
        AIAnimationController _animationController;
        AICore _brain;
        Transform _transform;

        Transform[] _patrolPoints;
        int _currentPatrolIndex = 0;
        bool _isWaiting = false;

        Quaternion _originalRotation; // Used when there is only one patrol point so enemy doesn't drift

        public EnemyMovement(AICore brain, Transform transform, NavMeshAgent agent, EnemySensors sensors, AIAnimationController animationController, MovementConfig config, Transform[] patrolPoints)
        {
            _brain = brain;
            _transform = transform;
            _agent = agent;
            _sensors = sensors;
            _animationController = animationController;
            _config = config;
            _patrolPoints = patrolPoints;

            _originalRotation = _transform.rotation;
            
            if (_patrolPoints == null || _patrolPoints.Length == 0)
            {
                GameObject point = new ("patrolPoint")
                {
                    transform =
                    {
                        position = _transform.position,
                        parent = _transform.parent
                    }
                };
                _patrolPoints = new[] { point.transform };
            }

            ResumeDefaultMovement();
        }

        internal void SetSpeedMultiplier(float multiplier)
        {
            _agent.speed = _config.basePlayerSpeed * multiplier;
        }

        internal void UpdateAngularSpeed(AgentState agentState)
        {
            _agent.angularSpeed = agentState switch
            {
                AgentState.Combat => _config.combatAngularSpeed,
                _ => _config.baseAngularSpeed
            };
        }

        // --- Patrol Logic ---
        internal void UpdatePatrolState()
        {
            UpdateAngularSpeed(AgentState.Patrol);
            if (_patrolPoints.Length == 0) return;
            if (!_agent.pathPending && _agent.remainingDistance < 0.5f && !_isWaiting)
            {
                _brain.StartCoroutine(PatrolWaitRoutine());
            }
        }

        void GoToNextPatrolPoint()
        {
            if (_patrolPoints.Length == 0) return;
            _agent.destination = _patrolPoints[_currentPatrolIndex].position;
            _currentPatrolIndex = (_currentPatrolIndex + 1) % _patrolPoints.Length;
        }

        // --- Coroutines & State Handlers ---
        internal void ResumeDefaultMovement()
        {
            SetSpeedMultiplier(_config.patrolSpeedMultiplier);
            _agent.isStopped = false;
            _isWaiting = false;
            GoToNextPatrolPoint();
        }

        internal void StopAllMovementCoroutines()
        {
            _brain.StopAllCoroutines();
            _isWaiting = false;
            _animationController.AgentLooking(false);
        }

        internal void StartLookAround(float duration, AICore brain)
        {
            _brain.StartCoroutine(LookAroundRoutine(duration, brain));
        }

        internal void StartInvestigate(float duration, Vector3 targetPos, AICore brain)
        {
            _brain.StartCoroutine(InvestigateRoutine(duration, targetPos, brain));
        }

        IEnumerator PatrolWaitRoutine()
        {
            _isWaiting = true;
            _agent.isStopped = true;
            _animationController.AgentLooking(true);
            yield return _brain.StartCoroutine(SweepRotationRoutine(_config.waitTimeAtWaypoint, false, 30));
            _animationController.AgentLooking(false);
            _agent.isStopped = false;
            GoToNextPatrolPoint();
            if (_patrolPoints.Length <= 1) yield return _brain.StartCoroutine(FixRotationRoutine(_originalRotation, _config.waitTimeAtWaypoint));
            _isWaiting = false;
        }

        IEnumerator LookAroundRoutine(float duration, AICore brain)
        {
            _agent.isStopped = true;
            yield return _brain.StartCoroutine(SweepRotationRoutine(duration, _sensors.IsPlayerVisible));
            _agent.isStopped = false;
            brain.ChangeState(AgentState.Patrol);
        }

        IEnumerator InvestigateRoutine(float duration, Vector3 targetPos, AICore brain)
        {
            _agent.isStopped = false;
            _agent.stoppingDistance = _config.agentStopDistance;
            if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, 100f, NavMesh.AllAreas))
                _agent.SetDestination(hit.position);

            while (_agent.pathPending || _agent.remainingDistance > _config.agentStopDistance) yield return null;

            if (!_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance) _agent.velocity = Vector3.zero;
            
            _agent.isStopped = true;
            yield return _brain.StartCoroutine(SweepRotationRoutine(duration, false, 70f));

            _agent.isStopped = false;
            brain.DetermineAlertLevel(0.2f);
        }

        IEnumerator SweepRotationRoutine(float duration, bool trackLastKnownPosition, float lookAngle = 70f)
        {
            float timer = 0f;
            bool lookingLeft = true;
            Quaternion centerRotation = _transform.rotation;

            while (timer < duration)
            {
                if (trackLastKnownPosition)
                {
                    Vector3 direction = (_sensors.LastKnownPosition - _transform.position).normalized;
                    if (direction != Vector3.zero)
                        centerRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));

                    if (_sensors.IsPlayerVisible)
                    {
                        timer = 0f;
                        _transform.rotation = Quaternion.RotateTowards(_transform.rotation, centerRotation,
                            _agent.angularSpeed * Time.deltaTime);
                        yield return null;
                        continue;
                    }
                }
                else
                {
                    if (_sensors.IsPlayerVisible) break;
                }

                timer += Time.deltaTime;
                Quaternion targetSweep = centerRotation * Quaternion.Euler(0, lookingLeft ? -lookAngle : lookAngle, 0);
                _transform.rotation = Quaternion.RotateTowards(_transform.rotation, targetSweep,
                    _agent.angularSpeed * Time.deltaTime / 2f);

                if (Quaternion.Angle(_transform.rotation, targetSweep) < 1f) lookingLeft = !lookingLeft;
                yield return null;
            }
        }

        IEnumerator FixRotationRoutine(Quaternion targetRotation, float duration)
        {
            float timer = 0f;

            while (timer < duration)
            {
                timer += Time.deltaTime;
                _transform.rotation = Quaternion.RotateTowards(_transform.rotation, targetRotation,
                    _agent.angularSpeed * Time.deltaTime / 2f);
                
                yield return null;
            }
        }
    }
}