using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Animations.Rigging;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace EnemySystem
{
    public class AIAnimationController : MonoBehaviour
    {
        readonly static int Speed = Animator.StringToHash("Speed");
        readonly static int Looking = Animator.StringToHash("Looking");
        [SerializeField] Animator animator;
        [SerializeField] Rigidbody[]  rigidbodies;
        NavMeshAgent _agent;

        [SerializeField] Transform gunPivot;
        
        [FormerlySerializedAs("lookTarget")] [SerializeField] Transform weaponTarget;
        [SerializeField] Transform lookTarget;
        
        Vector3 _lookTargetDefaultPos;
        Vector3 _additionalSweepTargetPos;
        
        [SerializeField] internal HumanoidAnimator humanoidAnimator;
        [SerializeField] MultiAimConstraint headConstraint;
        float _targetHeadConstraintWeight = 1f;
        bool _isHolster = true;
        internal EnemyBrain brain;

        [SerializeField] float lookTargetMoveSpeed = 8f;
        [SerializeField] float aimTargetMoveSpeed = 4f;
        
        void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _lookTargetDefaultPos = weaponTarget.localPosition;
        }
        
        void Update()
        {
            animator.SetFloat(Speed, _agent.velocity.magnitude/5f);
        }
        
        void LateUpdate()
        {
            bool lookAtPlayer = !_isHolster && brain.Sensors.IsPlayerVisible;

            if (lookAtPlayer)
            {
                weaponTarget.position = Vector3.Lerp(weaponTarget.position,
                    brain.Sensors.LastKnownPosition + (Vector3.up * 1.5f), Time.deltaTime * aimTargetMoveSpeed);
                lookTarget.position = Vector3.Lerp(lookTarget.position,
                    brain.Sensors.LastKnownPosition + (Vector3.up * 1.5f), Time.deltaTime * lookTargetMoveSpeed);
            }
            else
            {
                if (!_isHolster)
                {
                    weaponTarget.localPosition = Vector3.Lerp(weaponTarget.localPosition,
                        _lookTargetDefaultPos, Time.deltaTime * aimTargetMoveSpeed);
                }
                else
                {
                    weaponTarget.localPosition = Vector3.Lerp(weaponTarget.localPosition,
                        new Vector3(0, -2, 1), Time.deltaTime * aimTargetMoveSpeed);
                }
                lookTarget.localPosition = Vector3.Lerp(lookTarget.localPosition,
                    _lookTargetDefaultPos + _additionalSweepTargetPos, Time.deltaTime * lookTargetMoveSpeed);
            }
            
            if(Mathf.Abs(headConstraint.weight - _targetHeadConstraintWeight) < 0.01f)
                headConstraint.weight = _targetHeadConstraintWeight;
            else
                headConstraint.weight = Mathf.Lerp(headConstraint.weight, _targetHeadConstraintWeight, Time.deltaTime * lookTargetMoveSpeed);
        }

        public void AgentLooking(bool looking)
        {
            animator.SetBool(Looking, looking);
        }
        
        public void SetRagdoll(bool isRagdoll)
        {
            animator.enabled = !isRagdoll;
            foreach (Rigidbody rb in rigidbodies)
            {
                rb.isKinematic = !isRagdoll;
            }
        }

        internal void SetState(AgentState state, bool isReloading=false)
        {
            switch (state)
            {
                case AgentState.None: case AgentState.Patrol:
                    _isHolster = true;
                    break;
                case AgentState.Combat: case AgentState.Alert:
                    _isHolster = isReloading;
                    break;
            }
        }
        
        internal IEnumerator SweepRotationRoutine(float duration, bool trackLastKnownPosition, float lookAngle = 70f)
        {
            float timer = 0f;
            bool lookingLeft = Random.value < 0.5f;
            float currentSweepAngle = 0f;

            while (timer < duration)
            {
                float centerAngle = 0f;

                if (trackLastKnownPosition)
                {
                    Vector3 direction = (brain.Sensors.LastKnownPosition - transform.position).normalized;
                    if (direction != Vector3.zero)
                    {
                        Vector3 localDirection = transform.InverseTransformDirection(direction);
                        centerAngle = Mathf.Atan2(localDirection.x, localDirection.z) * Mathf.Rad2Deg;
                    }
                    
                    if (brain.Sensors.IsPlayerVisible)
                    {
                        timer = 0f;
                        
                        currentSweepAngle = Mathf.MoveTowardsAngle(currentSweepAngle, centerAngle, _agent.angularSpeed * Time.deltaTime);
                        _additionalSweepTargetPos = (Quaternion.Euler(0, currentSweepAngle, 0) * _lookTargetDefaultPos) - _lookTargetDefaultPos;
                        
                        yield return null;
                        continue;
                    }
                }
                else
                {
                    if (brain.Sensors.IsPlayerVisible) break;
                }

                timer += Time.deltaTime;
                
                float targetAngle = centerAngle + (lookingLeft ? -lookAngle : lookAngle);
                
                currentSweepAngle = Mathf.MoveTowardsAngle(currentSweepAngle, targetAngle, _agent.angularSpeed * Time.deltaTime / 2f);
                
                _additionalSweepTargetPos = (Quaternion.Euler(0, currentSweepAngle, 0) * _lookTargetDefaultPos) - _lookTargetDefaultPos;
                
                if (Mathf.Abs(Mathf.DeltaAngle(currentSweepAngle, targetAngle)) < 1f) lookingLeft = !lookingLeft;
                
                yield return null;
            }

            // Reset the offset when the routine finishes so the character looks straight again
            _additionalSweepTargetPos = Vector3.zero;
        }
    }
}
