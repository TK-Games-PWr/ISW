using UnityEngine;

namespace EnemySystem
{
    public class RamberCombat : EnemyCombat
    {
        [Header("RambEmemy specific stats")]
        [SerializeField] float shotgunRange = 5f;

        internal override void HandleCombatMovement(Transform playerTransform, float distanceToPlayer, bool hasLOS)
        {
            movement.SetSpeedMultiplier(combatSpeedMultiplier);

            Vector3 direction = (playerTransform.position - transform.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
            transform.rotation =
                Quaternion.RotateTowards(transform.rotation, lookRotation, agent.angularSpeed * Time.deltaTime);

            if (distanceToPlayer <= shotgunRange && hasLOS)
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
            if (sensors.PlayerTransform == null) return;
            float distance = Vector3.Distance(transform.position, sensors.PlayerTransform.position);
            if (distance > shotgunRange) return;
            base.CombatAction();
        }
    }
}
