using System.Collections;
using PlayerShootingSystem;
using TK_Shared.ObjectInteractions3D;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace EnemySystem
{
    [RequireComponent(typeof(EnemySensors))]
    public class EnemyCombat : MonoBehaviour
    {
        [Header("Combat Settings")]
        public Gun currentGun;
        [SerializeField] Transform GunPivot;
        [SerializeField] float optimalCombatDistancePct = 0.7f;
        [Tooltip("Total amount of bullets, specified in magazines of current weapon")]
        [SerializeField] int totalMagazines = 1;
        private int availableAmmo;
        [SerializeField] float reloadTime = 1.7f;
        [SerializeField] float weaponRange; // todo: replace from guninfo
        [SerializeField] int magazineAmmo = 15; // todo: replace from guninfo
        [Tooltip("Extra delay added between shots for semi-automatic weapons to simulate an AI's trigger finger.")]
        [SerializeField] float singleShotDelay = 0.6f;

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
        }

        private void Start()
        {
            if (currentGun != null && currentGun.TryGetComponent(out GrabbableObject grabbable))
            {
                grabbable.Grab(GunPivot);
            }

            availableAmmo = (totalMagazines - 1) * magazineAmmo;
            currentAmmo = magazineAmmo;
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
            else if (availableAmmo > 0 && !IsReloading)
            {
                StartCoroutine(ReloadRoutine());
            }
        }

        private void TryShootOnce(float distanceToPlayer)
        {
            currentAmmo--;
            currentGun.PerformShoot();

            Vector3 rayOrigin = GunPivot.position;
            Vector3 targetPosition = sensors.PlayerTransform.position + Vector3.up * 1.7f;
            Vector3 direction = (targetPosition - rayOrigin).normalized;

            float movementFactor = agent.speed > 0.01f
                ? Mathf.Clamp01(agent.velocity.magnitude / agent.speed)
                : 0f;

            float spreadAmount = (currentGun.gunInfo.spread + 
                                  currentGun.gunInfo.movementSpreadPenalty * movementFactor);
            Quaternion spreadRotation = Quaternion.LookRotation(direction);
            direction += spreadRotation * Vector3.right * Random.Range(-spreadAmount, spreadAmount);
            direction += spreadRotation * Vector3.up * Random.Range(-spreadAmount, spreadAmount);
            direction.Normalize();

            // Nothing was hit 
            if (!Physics.Raycast(rayOrigin, direction, out RaycastHit hit, weaponRange))
                return;

            // Something other than the player was hit
            if (hit.collider.transform != sensors.PlayerTransform)
                return;

            float multiplier = currentGun.gunInfo.damageFalloff.Evaluate(hit.distance / 100f);
            float finalDamage = currentGun.gunInfo.flatDamage * multiplier;
            sensors.PlayerResources.Damage(finalDamage);
        }

        private IEnumerator ReloadRoutine()
        {
            IsReloading = true;
            agent.isStopped = true;

            yield return new WaitForSeconds(reloadTime);

            currentAmmo = availableAmmo > magazineAmmo ?  magazineAmmo : availableAmmo;
            availableAmmo -= currentAmmo;
            IsReloading = false;
            agent.isStopped = false;
        }
    }
}
