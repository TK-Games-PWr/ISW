using UnityEngine;

namespace EnemySystem
{
    public class CoverGuyCombat : EnemyCombat
    {
        [Header("Specific to cover guy")]
        [SerializeField] float hidingSpeedMultiplier = 2f;
        [SerializeField] LayerMask playerLayerMask;
        LayerMask _checkCoverMask;

        protected override void Awake()
        {
            base.Awake();
            _checkCoverMask = sensors.sightObstaclesMask;
            _checkCoverMask |= (1 << playerLayerMask);
        }
        
        internal override void HandleCombatMovement(Transform playerTransform, float distanceToPlayer, bool hasLOS)
        {
            movement.SetSpeedMultiplier(combatSpeedMultiplier);

            Vector3 direction = (playerTransform.position - transform.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
            transform.rotation =
                Quaternion.RotateTowards(transform.rotation, lookRotation, agent.angularSpeed * Time.deltaTime);

            if (distanceToPlayer <= weaponRange && hasLOS)
            {
                if (!IsCovered(playerTransform))
                {
                    Vector3 coverPos = NavGridSystem.Instance.GetBestCover(agent, _checkCoverMask, playerTransform.position + new Vector3(0, 1, 0));
                    agent.SetDestination(coverPos);
                }
            }
            else
            {
                agent.stoppingDistance = movement.agentStopDistance;
                agent.SetDestination(playerTransform.position); // Chase
                if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance) agent.velocity = Vector3.zero;
            }
        }
        
        // Assuming player is visible!
        internal override void CombatAction()
        {
            base.CombatAction();
        }

        bool IsCovered(Transform playerTransform)
        {
            if (Physics.Linecast(transform.position + new Vector3(0, 1, 0), playerTransform.position, out RaycastHit hit, _checkCoverMask))
            {
                if (!hit.collider.CompareTag(sensors.playerTag)) return true;
            }

            return false;
        }
    }
}
