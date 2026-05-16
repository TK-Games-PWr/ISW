using UnityEngine;

namespace EnemySystem
{
    public class RamberCombat : EnemyCombat
    {
        internal override void HandleCombatMovement(Transform playerTransform, float distanceToPlayer, bool hasLOS)
        {
            movement.SetSpeedMultiplier(combatSpeedMultiplier);

            Vector3 direction = (playerTransform.position - transform.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
            transform.rotation =
                Quaternion.RotateTowards(transform.rotation, lookRotation, agent.angularSpeed * Time.deltaTime);

            if (distanceToPlayer <= weaponRange && hasLOS)
            {
                agent.SetDestination(transform.position);
            }
            else
            {
                agent.SetDestination(playerTransform.position);
            }
        }

        internal override void CombatAction()
        {
            base.CombatAction();
        }
    }
}
