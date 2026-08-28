using UnityEngine;
using UnityEngine.AI;

namespace EnemySystem
{
    public class RamberCombat : EnemyCombat
    {
        public RamberCombat(EnemyBrain brain, Transform transform, NavMeshAgent agent, EnemySensors sensors, EnemyMovement movement, EnemyResources resources, AIAnimationController animationController, RambenemyCombatConfig config) 
            : base(brain, transform, agent, sensors, movement, resources, animationController, config)
        {
        }
        
        internal override void HandleCombatMovement(Transform playerTransform, float distanceToPlayer, bool hasLOS)
        {
            movement.SetSpeedMultiplier(config.combatSpeedMultiplier);

            if (distanceToPlayer <= WeaponRange && hasLOS)
            {
                if (distanceToPlayer <= OptimalDistance)
                {
                    movement.SetSpeedMultiplier(config.retreatSpeedMultiplier);
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
                agent.stoppingDistance = brain.Config.movement.agentStopDistance;
                agent.SetDestination(playerTransform.position); // Chase
                if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance) agent.velocity = Vector3.zero;
            }
        }

        internal override void CombatAction()
        {
            base.CombatAction();
        }
    }
}
