using System.Collections;
using PlayerShootingSystem;
using TK_Shared._3DPlayerMovement;
using TK_Shared.ObjectInteractions3D;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace EnemySystem
{
    [RequireComponent(typeof(EnemySensors), typeof(EnemyMovement), typeof(EnemyResources))]
    public class EnemyCombat : MonoBehaviour
    {
        protected EnemyResources resources;
        
        [SerializeField] protected float optimalCombatDistancePct = 0.7f;

        protected int availableAmmo;
        [SerializeField] protected float weaponRange; 

        [Tooltip("Extra delay added between shots for semi-automatic weapons to simulate an AI's trigger finger.")]
        [SerializeField] protected float singleShotDelay = 0.6f;

        GunInfo GunInfo => classGun.gunInfo;

        internal float WeaponRange => weaponRange;
        internal float OptimalDistance => weaponRange * optimalCombatDistancePct;
        internal bool IsReloading { get; private set; } = false;

        protected int currentAmmo;
        protected float nextFireTime = 0f;
        
        [SerializeField] protected float combatSpeedMultiplier = 1.3f;
        [SerializeField] protected float retreatSpeedMultiplier = 0.3f;
        
        [FormerlySerializedAs("preferredGun")] public Gun classGun;
        
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
            if (classGun != null && classGun.TryGetComponent(out GrabbableObject grabbable))
            {
                bool shouldDisable = false;
                if (!classGun.gameObject.activeInHierarchy)
                {
                    shouldDisable = true;
                    classGun.gameObject.SetActive(true);
                }
                
                grabbable.Grab(resources.gunPivot);
                
                if(shouldDisable) classGun.gameObject.SetActive(false);
            }

            availableAmmo = (resources.totalMagazines - 1) * GunInfo.maxAmmo;
            currentAmmo = GunInfo.maxAmmo;
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
        internal virtual void CombatAction()
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

        protected void Shoot()
        {
            currentAmmo--;
            resources.currentGun.PerformShoot();

            Vector3 rayOrigin = resources.gunPivot.position;
            
            var playerController = sensors.PlayerTransform.GetComponent<TK_Shared._3DPlayerMovement.PlayerActionsController>();
            Vector3 targetPosition = sensors.PlayerTransform.position + Vector3.up * PlayerActionsController.EyeLevel;
            
            Vector3 direction = (targetPosition - rayOrigin).normalized;

            float movementFactor = agent.speed > 0.01f
                ? Mathf.Clamp01(agent.velocity.magnitude / agent.speed)
                : 0f;
            
            float spreadAmount = (resources.currentGun.gunInfo.spread + 
                                  resources.currentGun.gunInfo.movementSpreadPenalty * movementFactor);
            Quaternion spreadRotation = Quaternion.LookRotation(direction);

            for (int i = 0; i < resources.currentGun.gunInfo.firesShotPerAmmo; i++)
            {
                direction = (targetPosition - rayOrigin).normalized;
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
            float x, y;
            if (resources.currentGun.gunInfo.firesShotPerAmmo > 1)
            {
                Vector2 disc = Random.insideUnitCircle * spreadAmount;
                x = disc.x;
                y = disc.y;
            }
            else
            {
                x = Random.Range(-spreadAmount, spreadAmount);
                y = Random.Range(-spreadAmount, spreadAmount);
            }
            direction += spreadRotation * Vector3.right * x;
            direction += spreadRotation * Vector3.up * y;
            direction.Normalize();

            if (!Physics.Raycast(rayOrigin, direction, out RaycastHit hit, weaponRange))
                return null;

            if (hit.collider.transform != sensors.PlayerTransform)
                return null;

            return hit;
        }

        protected IEnumerator ReloadRoutine()
        {
            IsReloading = true;
            agent.isStopped = true;

            yield return new WaitForSeconds(GunInfo.reloadTime);

            currentAmmo = availableAmmo > GunInfo.maxAmmo ?  GunInfo.maxAmmo : availableAmmo;
            availableAmmo -= currentAmmo;
            IsReloading = false;
            agent.isStopped = false;
        }
    }
}
