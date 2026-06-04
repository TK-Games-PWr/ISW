using UnityEngine;
using UnityEngine.AI;

namespace EnemySystem
{
    public class CoverGuyCombat : EnemyCombat
    {
        LayerMask playerLayerMask;
        LayerMask _checkCoverMask;
        
        public CoverGuyCombat(AICore brain, Transform transform, NavMeshAgent agent, EnemySensors sensors, EnemyMovement movement, EnemyResources resources, CoverGuyCombatConfig config) 
            : base(brain, transform, agent, sensors, movement, resources, config)
        {
            this.config = config;
        }

        protected internal override void Init()
        {
            _checkCoverMask = brain.config.sensors.sightObstaclesMask;
            _checkCoverMask |= (1 << playerLayerMask);
            base.Init();
        }
        
        internal override void HandleCombatMovement(Transform playerTransform, float distanceToPlayer, bool hasLOS)
        {
            movement.SetSpeedMultiplier(config.combatSpeedMultiplier);

            if (distanceToPlayer <= WeaponRange && hasLOS)
            {
                if (!IsCovered(playerTransform))
                {
                    Vector3 coverPos = NavGridSystem.Instance.GetBestCover(agent, _checkCoverMask, playerTransform.position + new Vector3(0, 1, 0));
                    agent.SetDestination(coverPos);
                }
            }
            else
            {
                agent.stoppingDistance = brain.config.movement.agentStopDistance;
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
                if (!hit.collider.CompareTag(brain.config.sensors.playerTag)) return true;
            }

            return false;
        }
    }
}
