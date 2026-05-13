using System;
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
        [Tooltip("Total amount of bullets, specified in magazines of current weapon")]
        [SerializeField] int totalMagazines = 1;

        int availableAmmo;
        [SerializeField] float reloadTime = 1.7f;
        [SerializeField] float weaponRange; // todo: replace from guninfo
        [SerializeField] int magazineAmmo = 15; // todo: replace from guninfo

        [Tooltip("Extra delay added between shots for semi-automatic weapons to simulate an AI's trigger finger.")]
        [SerializeField] float singleShotDelay = 0.6f;

        internal float WeaponRange => weaponRange;
        internal float OptimalDistance => weaponRange * optimalCombatDistancePct;
        internal bool IsReloading { get; private set; } = false;

        int currentAmmo;
        float nextFireTime = 0f;
        
        [SerializeField] float combatSpeedMultiplier = 1.3f;
        [SerializeField] float retreatSpeedMultiplier = 0.3f;
        
        NavMeshAgent agent;

        EnemySensors sensors;
        EnemyMovement movement;

        void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            sensors = GetComponent<EnemySensors>();
        }

        void Start()
        {
            if (currentGun != null && currentGun.TryGetComponent(out GrabbableObject grabbable))
            {
                grabbable.Grab(GunPivot);
            }
            
            availableAmmo = (totalMagazines - 1) * magazineAmmo;
            currentAmmo = magazineAmmo;
        }
        
        // --- Combat Movement Logic ---
        internal void HandleCombatMovement(Transform playerTransform, float distanceToPlayer, bool hasLOS)
        {
            movement.SetSpeedMultiplier(combatSpeedMultiplier);

            Vector3 direction = (playerTransform.position - transform.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
            transform.rotation =
                Quaternion.RotateTowards(transform.rotation, lookRotation, agent.angularSpeed * Time.deltaTime);

            if (distanceToPlayer <= weaponRange && hasLOS)
            {
                if (distanceToPlayer <= OptimalDistance)
                {
                    movement.SetSpeedMultiplier(retreatSpeedMultiplier);
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
                agent.SetDestination(playerTransform.position); // Chase
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
            else if (availableAmmo > 0 && !IsReloading)
            {
                StartCoroutine(ReloadRoutine());
            }
        }

        void TryShootOnce(float distanceToPlayer)
        {
            currentAmmo--;
            currentGun.PerformShoot();

            float multiplier = currentGun.gunInfo.damageFalloff.Evaluate(distanceToPlayer / 100f);
            float finalDamage = currentGun.gunInfo.flatDamage * multiplier;
            if (sensors.PlayerResources != null) sensors.PlayerResources.Damage(finalDamage);
        }

        IEnumerator ReloadRoutine()
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