using UnityEngine;
using UnityEngine.AI;

namespace EnemySystem
{
    public class CoverGuyCombat : EnemyCombat
    {
        LayerMask _checkCoverMask;
        CoverGuyCombatConfig _coverConfig;
        
        public CoverGuyCombat(EnemyBrain brain, Transform transform, NavMeshAgent agent, EnemySensors sensors, EnemyMovement movement, EnemyResources resources, CoverGuyCombatConfig config) 
            : base(brain, transform, agent, sensors, movement, resources, config)
        {
            this.config = config;
            if (brain.Config.coverGuyCombat is CoverGuyCombatConfig coverConfig)
            {
                _coverConfig = coverConfig;
            }
            else
            {
                Debug.LogError("Wrong EnemyConfig provided, expected CoverGuyCombatConfig");
            }
        }

        protected internal override void Init()
        {
            _checkCoverMask = brain.Config.sensors.sightObstaclesMask;
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
                    movement.SetSpeedMultiplier(_coverConfig.hidingSpeedMultiplier);
                    agent.SetDestination(coverPos);
                }
            }
            else
            {
                agent.stoppingDistance = brain.Config.movement.agentStopDistance;
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
                if (!hit.collider.CompareTag(brain.Config.sensors.playerTag)) return true;
            }

            return false;
        }
    }
}
