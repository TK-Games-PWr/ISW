using System.Collections;
using PlayerShootingSystem;
using TK_Shared._3DPlayerMovement;
using TK_Shared.ObjectInteractions3D;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace EnemySystem
{
    public class EnemyCombat
    {
        protected CombatConfig config;
        protected Transform transform;
        protected NavMeshAgent agent;
        protected EnemySensors sensors;
        protected EnemyMovement movement;
        protected EnemyResources resources;
        protected EnemyBrain brain;
        readonly protected AIAnimationController animationController;

        protected int availableAmmo;

        GunInfo GunInfo => resources.currentGun.gunInfo;

        public float WeaponRange => config.weaponRange;
        public float OptimalDistance => config.weaponRange * config.optimalCombatDistancePct;
        public bool IsReloading { get; protected set; } = false;

        protected int currentAmmo;
        protected float nextFireTime = 0f;
        
        public EnemyCombat(EnemyBrain brain, Transform transform, NavMeshAgent agent, EnemySensors sensors, EnemyMovement movement, EnemyResources resources, AIAnimationController animationController, CombatConfig config)
        {
            this.brain = brain;
            this.transform = transform;
            this.agent = agent;
            this.sensors = sensors;
            this.movement = movement;
            this.resources = resources;
            this.animationController = animationController;
            this.config = config;
        }

        protected internal virtual void Init()
        {
            if (resources.currentGun != null && resources.currentGun.TryGetComponent(out GrabbableObject grabbable))
            {
                bool shouldDisable = false;
                if (!resources.currentGun.gameObject.activeInHierarchy)
                {
                    shouldDisable = true;
                    resources.currentGun.gameObject.SetActive(true);
                }
                
                grabbable.Grab(resources.gunParent, true);
                
                if(shouldDisable) resources.currentGun.gameObject.SetActive(false);
            }

            if (resources.currentGun != null)
            {
                availableAmmo = (resources.totalMagazines - 1) * GunInfo.maxAmmo;
                currentAmmo = GunInfo.maxAmmo;
            }
        }

        internal virtual void RotateTowardsPlayer(Transform playerTransform)
        {
            Vector3 direction = (playerTransform.position - transform.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
            transform.rotation =
                Quaternion.RotateTowards(transform.rotation, lookRotation, agent.angularSpeed * Time.deltaTime);
        }
        
        // --- Combat Movement Logic ---
        internal virtual void HandleCombatMovement(Transform playerTransform, float distanceToPlayer, bool hasLOS)
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
                        cooldown += config.singleShotDelay;
                    }

                    nextFireTime = Time.time + cooldown;
                }
            }
            else if (availableAmmo > 0 && !IsReloading)
            {
                brain.StartCoroutine(ReloadRoutine());
            }
        }

        protected void Shoot()
        {
            currentAmmo--;
            resources.currentGun.PerformShoot();

            Vector3 rayOrigin = resources.gunParent.position;
            
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

            if (!Physics.Raycast(rayOrigin, direction, out RaycastHit hit, config.weaponRange))
                return null;

            if (hit.collider.transform != sensors.PlayerTransform)
                return null;

            return hit;
        }

        protected IEnumerator ReloadRoutine()
        {
            IsReloading = true;
            agent.isStopped = true;
            animationController.SetState(brain.CurrentAgentState, true);
            
            yield return new WaitForSeconds(GunInfo.reloadTime);
            
            animationController.SetState(brain.CurrentAgentState, false);
            currentAmmo = availableAmmo > GunInfo.maxAmmo ?  GunInfo.maxAmmo : availableAmmo;
            availableAmmo -= currentAmmo;
            IsReloading = false;
            agent.isStopped = false;
        }
        
        public virtual void ResetCombatState()
        {
            IsReloading = false;
            if (agent != null && agent.gameObject.activeInHierarchy && agent.isOnNavMesh)
            {
                agent.isStopped = false;
            }
        }
    }
}
