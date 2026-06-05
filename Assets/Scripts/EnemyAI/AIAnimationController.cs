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
        void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
        }

        // Update is called once per frame
        void Update()
        {
            animator.SetFloat(Speed, _agent.velocity.magnitude/5f);
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
