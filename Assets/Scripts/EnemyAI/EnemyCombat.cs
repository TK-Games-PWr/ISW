using System.Collections;
using TK_Shared.ObjectInteractions3D;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace EnemySystem
{
    [RequireComponent(typeof(EnemySensors))]
    public class EnemyCombat : MonoBehaviour
    {
        protected EnemyResources resources;
        
        [SerializeField] protected float optimalCombatDistancePct = 0.7f;
        [Tooltip("Total amount of bullets, specified in magazines of current weapon")]
        [SerializeField] protected int totalMagazines = 1;

        protected int availableAmmo;
        [SerializeField] protected float reloadTime = 1.7f;
        [SerializeField] protected float weaponRange; 
        [SerializeField] protected int magazineAmmo = 15; 

        [Tooltip("Extra delay added between shots for semi-automatic weapons to simulate an AI's trigger finger.")]
        [SerializeField] protected float singleShotDelay = 0.6f;

        internal float WeaponRange => weaponRange;
        internal float OptimalDistance => weaponRange * optimalCombatDistancePct;
        internal bool IsReloading { get; private set; } = false;

        protected int currentAmmo;
        protected float nextFireTime = 0f;
        
        [SerializeField] protected float combatSpeedMultiplier = 1.3f;
        [SerializeField] protected float retreatSpeedMultiplier = 0.3f;
        
        protected NavMeshAgent agent;
        protected EnemySensors sensors;
        protected EnemyMovement movement;

        protected virtual void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            sensors = GetComponent<EnemySensors>();
            movement = GetComponent<EnemyMovement>();
            resources = GetComponent<EnemyResources>();
        }

        protected virtual void Start()
        {
            if (resources.currentGun != null && resources.currentGun.TryGetComponent(out GrabbableObject grabbable))
            {
                grabbable.Grab(resources.gunPivot);
            }

            availableAmmo = (totalMagazines - 1) * magazineAmmo;
            currentAmmo = magazineAmmo;
        }
        
        // --- Combat Movement Logic ---
        internal virtual void HandleCombatMovement(Transform playerTransform, float distanceToPlayer, bool hasLOS)
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
        internal virtual void CombatAction(float distanceToPlayer)
        {
            if (currentAmmo > 0)
            {
                if (Time.time >= nextFireTime)
                {
                    Shoot();

                    float cooldown = resources.currentGun.gunInfo.fireRate;

                    if (!resources.currentGun.gunInfo.isAutomatic)
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

        protected virtual void Shoot()
        {
            currentAmmo--;
            resources.currentGun.PerformShoot();

            Vector3 rayOrigin = resources.gunPivot.position;
            
            var playerController = sensors.PlayerTransform.GetComponent<TK_Shared._3DPlayerMovement.PlayerActionsController>();
            Vector3 targetPosition = sensors.PlayerTransform.position + Vector3.up * playerController.eyeLevel;
            
            Vector3 direction = (targetPosition - rayOrigin).normalized;

            float movementFactor = agent.speed > 0.01f
                ? Mathf.Clamp01(agent.velocity.magnitude / agent.speed)
                : 0f;
            
            float spreadAmount = (resources.currentGun.gunInfo.spread + 
                                  resources.currentGun.gunInfo.movementSpreadPenalty * movementFactor);
            Quaternion spreadRotation = Quaternion.LookRotation(direction);

            for (int i = 0; i < resources.currentGun.gunInfo.firesShotPerAmmo; i++)
            {
                RaycastHit? hit = TryRaycastHit(direction, spreadRotation, rayOrigin, spreadAmount);
                
                if (hit != null)
                {
                    float multiplier = resources.currentGun.gunInfo.damageFalloff.Evaluate(hit.Value.distance / 100f);
                    float finalDamage = resources.currentGun.gunInfo.flatDamage * multiplier;
                    sensors.PlayerResources.Damage(finalDamage);
                }
            }
        }

        RaycastHit? TryRaycastHit(Vector3 direction, Quaternion spreadRotation, Vector3 rayOrigin, float spreadAmount)
        {
            direction += spreadRotation * Vector3.right * Random.Range(-spreadAmount, spreadAmount);
            direction += spreadRotation * Vector3.up * Random.Range(-spreadAmount, spreadAmount);
            direction.Normalize();

            // Nothing was hit 
            if (!Physics.Raycast(rayOrigin, direction, out RaycastHit hit, weaponRange))
                return null;

            // Something other than the player was hit
            if (hit.collider.transform != sensors.PlayerTransform)
                return null;

            return hit;
        }

        protected virtual IEnumerator ReloadRoutine()
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
