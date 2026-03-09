using UnityEngine;
using System.Collections;
using UnityEngine.AI; // Needed to stop agent during reload

namespace EnemySystem
{
    [RequireComponent(typeof(EnemySensors))]
    public class EnemyCombat : MonoBehaviour
    {
        [Header("Combat Settings")]
        public PlayerShootingSystem.GunInfo gunInfo;
        public PlayerShootingSystem.Gun currentGun;
        [SerializeField] float optimalCombatDistancePct = 0.7f;
        [SerializeField] float reloadTime = 1.7f;
        [SerializeField] float weaponRange; // todo: replace from guninfo
        [SerializeField] int maxAmmo = 15; // todo: replace from guninfo

        internal float WeaponRange => weaponRange;
        internal float OptimalDistance => weaponRange * optimalCombatDistancePct;
        internal bool IsReloading { get; private set; } = false;

        private int currentAmmo;
        private NavMeshAgent agent;

        private PlayerResources playerResources;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            playerResources = GetComponent<EnemySensors>().PlayerTransform.GetComponent<PlayerResources>();
            currentAmmo = maxAmmo;
        }

        // Assuming player is visible!
        internal void CombatAction(float distanceToPlayer)
        {
            if (currentAmmo > 0)
            {
                TryShootOnce(distanceToPlayer);
            }
            else if (!IsReloading)
            {
                StartCoroutine(ReloadRoutine());
            }
        }

        private void TryShootOnce(float distanceToPlayer)
        {
            // currentAmmo--;
            Debug.Log($"{gameObject.name} is shooting!");
            currentGun.PerformShoot();

            float multiplier = currentGun.gunInfo.damageFalloff.Evaluate(distanceToPlayer / 100f);
            float finalDamage = currentGun.gunInfo.flatDamage * multiplier;
            playerResources.Damage(finalDamage);
        }

        private IEnumerator ReloadRoutine()
        {
            IsReloading = true;
            agent.isStopped = true;

            yield return new WaitForSeconds(reloadTime);

            currentAmmo = maxAmmo;
            IsReloading = false;
            agent.isStopped = false;
        }
    }
}