using UnityEngine;
using System.Collections;
using UnityEngine.AI; // Needed to stop agent during reload
using TK_Shared.ObjectInteractions3D;

namespace EnemySystem
{
    [RequireComponent(typeof(EnemySensors))]
    public class EnemyCombat : MonoBehaviour
    {
        [Header("Combat Settings")]
        public PlayerShootingSystem.Gun currentGun;
        [SerializeField] Transform GunPivot;
        [SerializeField] float optimalCombatDistancePct = 0.7f;
        [SerializeField] float reloadTime = 1.7f;
        [SerializeField] float weaponRange; // todo: replace from guninfo
        [SerializeField] int maxAmmo = 15; // todo: replace from guninfo

        [Tooltip("Extra delay added between shots for semi-automatic weapons to simulate an AI's trigger finger.")]
        [SerializeField] float singleShotDelay = 0.2f;

        internal float WeaponRange => weaponRange;
        internal float OptimalDistance => weaponRange * optimalCombatDistancePct;
        internal bool IsReloading { get; private set; } = false;

        private int currentAmmo;
        private float nextFireTime = 0f;
        private NavMeshAgent agent;

        private EnemySensors sensors;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            sensors = GetComponent<EnemySensors>();
            currentAmmo = maxAmmo;

            if (currentGun != null && currentGun.TryGetComponent(out GrabbableObject grabbable))
            {
                grabbable.Grab(GunPivot);
            }
        }

        // Assuming player is visible!
        internal void CombatAction(float distanceToPlayer)
        {
            if (currentAmmo > 0)
            {
                if (Time.time >= nextFireTime)
                {
                    TryShootOnce(distanceToPlayer);

                    float cooldown = currentGun.gunInfo.fireRate;

                    if (!currentGun.gunInfo.isAutomatic)
                    {
                        cooldown += singleShotDelay;
                    }

                    nextFireTime = Time.time + cooldown;
                }
            }
            else if (!IsReloading)
            {
                StartCoroutine(ReloadRoutine());
            }
        }

        private void TryShootOnce(float distanceToPlayer)
        {
            currentAmmo--;
            Debug.Log($"{gameObject.name} is shooting!");
            currentGun.PerformShoot();

            float multiplier = currentGun.gunInfo.damageFalloff.Evaluate(distanceToPlayer / 100f);
            float finalDamage = currentGun.gunInfo.flatDamage * multiplier;
            if (sensors.PlayerResources != null) sensors.PlayerResources.Damage(finalDamage);
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