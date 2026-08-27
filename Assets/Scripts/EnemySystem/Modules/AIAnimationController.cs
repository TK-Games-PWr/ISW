using System;
using UnityEngine;
using UnityEngine.AI;

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
        
        [SerializeField] Transform lookTarget;
        Vector3 _lookTargetDefaultPos;
        
        [SerializeField] internal HumanoidAnimator humanoidAnimator;
        internal EnemyBrain brain;

        [SerializeField] float armRotationSpeed = 5f;
        [SerializeField] float lookTargetMoveSpeed = 8f;
        
        void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _lookTargetDefaultPos = lookTarget.localPosition;
        }
        
        void Update()
        {
            animator.SetFloat(Speed, _agent.velocity.magnitude/5f);
        }
        
        void LateUpdate()
        {
            bool lookAtPlayer = brain.Sensors.IsPlayerVisible || brain.CurrentAgentState == AgentState.Combat;

            if (lookAtPlayer)
            {
                lookTarget.position = Vector3.Lerp(lookTarget.position,
                    brain.Sensors.LastKnownPosition + (Vector3.up * 1.5f), Time.deltaTime * lookTargetMoveSpeed);
            }
            else
            {
                lookTarget.localPosition = Vector3.Lerp(lookTarget.localPosition,
                    _lookTargetDefaultPos, Time.deltaTime * lookTargetMoveSpeed);
            }
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
    }
}
