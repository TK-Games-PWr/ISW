using UnityEngine;
using System;
using UnityEngine.AI;
using UnityEngine.InputSystem.Controls;
using static EnemyManager;

namespace EnemySystem
{
    [RequireComponent(typeof(EnemyResources))]
    public class AICore : MonoBehaviour, IHearingTarget
    {
        // --- Events ---
        public static event Action<AICore, float, AlertLevel> OnAlertChanged;
        
        [Header("Configuration")]
        public EnemyConfig config;

        [Header("Patrol Settings")]
        public Transform[] patrolPoints;

        float _combatLostPlayerTime = 0f;

        // --- References ---
        public EnemySensors Sensors { get; private set; }
        public EnemyMovement Movement { get; private set; }
        public EnemyCombat Combat { get; private set; }
        public EnemyResources Resources { get; private set; }
        public EnemyAlertSystem AlertSystem { get; private set; }

        EnemyCombat _baseEnemyCombat;
        EnemyCombat _rambEnemyCombat;
        EnemyCombat _coverEnemyCombat;

        float _lastAlertTime = 0f;
        public float TimeInCombat { get; private set; } = 0f;

        public bool IsDead => Resources.IsDead;

        [Tooltip("Disables hearing system connection, useful for putting agent in menu background.")] [SerializeField]
        bool isDeaf = false;
        
        internal AgentState _currentAgentState = AgentState.Patrol;

        void Awake()
        {
            Resources = GetComponent<EnemyResources>();
            NavMeshAgent agent = GetComponent<NavMeshAgent>();
            AIAnimationController animController = GetComponent<AIAnimationController>();
            
            Sensors = new EnemySensors(this, transform, config.sensors);
            Movement = new EnemyMovement(this, transform, agent, Sensors, animController, config.movement, patrolPoints);
            AlertSystem = new EnemyAlertSystem(this, Sensors, Movement, config.alert);
            
            ChangeEnemyType(EnemyType.Normal);
        }
        
        async void OnEnable()
        {
            if (isDeaf) return;
            await Awaitable.EndOfFrameAsync();
            if (SoundSystem.Instance != null)
            {
                SoundSystem.Instance.RegisterListener(this);
            }
            else
            {
                Debug.LogError(gameObject.name + " failed to register sound event.");
            }
        }

        void OnDisable()
        {
            if (isDeaf) return;
            if (SoundSystem.Instance != null)
            {
                SoundSystem.Instance.UnregisterListener(this);
            }
            else
            {
                Debug.LogError(gameObject.name + " failed to unregister sound event.");
            }
        }

        void Update()
        {
            if (Resources.IsDead) return;
            
            // TODO transfer these to state manager to tick every 100ms
            Sensors.Tick();
            AlertSystem.Tick(Time.deltaTime);

            OnAlertChanged?.Invoke(this, AlertSystem.TriggerMultiplier, AlertSystem.CurrentAlertLevel);

            switch (_currentAgentState)
            {
                case AgentState.Patrol:
                    Movement.UpdatePatrolState();
                    break;
                case AgentState.Alert:
                    Movement.UpdateAngularSpeed(AgentState.Alert);
                    // Movement handled by Coroutines triggered in SetAlertLevel
                    break;
                case AgentState.Combat:
                    UpdateCombatLogic();
                    Movement.UpdateAngularSpeed(AgentState.Combat);
                    break;
            }
        }
        
        public Vector3 GetHearingPosition()
        {
            return transform.position + new Vector3(0, 1.5f, 0); 
        }

        public void OnSoundHeard(Vector3 soundOrigin, float baseVolume, float distance, float range, bool capAlertLevel,
            AnimationCurve falloffCurve)
        {
            AlertSystem.OnSoundHeard(soundOrigin, baseVolume, distance, range, capAlertLevel, falloffCurve);
        }

        internal void ChangeState(AgentState newAgentState)
        {
            if (_currentAgentState == newAgentState) return;
            _currentAgentState = newAgentState;

            switch (newAgentState)
            {
                case AgentState.Patrol:
                    break;
                case AgentState.Alert:
                    Movement.SetSpeedMultiplier(1f);
                    break;
                case AgentState.Combat:
                    // Speed set dynamically in combat loop
                    break;
            }
        }

        void UpdateCombatLogic()
        {
            TimeInCombat += Time.deltaTime;
            if (Sensors.PlayerTransform == null || Combat.IsReloading) return;

            bool hasLos = Sensors.IsPlayerVisible;
            float distanceToPlayer = Vector3.Distance(transform.position, Sensors.PlayerTransform.position);

            // Handle Movement
            Combat.HandleCombatMovement(Sensors.PlayerTransform, distanceToPlayer, hasLos);

            if (config.alert.fightDelay > TimeInCombat) return;

            if (!hasLos)
            {
                _combatLostPlayerTime += Time.deltaTime;
                if (_combatLostPlayerTime >= config.alert.endCombatTimeout)
                {
                    AlertSystem.TriggerMultiplier = 0.9f;
                    AlertSystem.DetermineAlertLevel();
                    _combatLostPlayerTime = 0f;
                    return;
                }
            }
            else
            {
                _combatLostPlayerTime = 0f;
            }

            // Handle Shooting
            if (distanceToPlayer <= Combat.WeaponRange && hasLos)
            {
                Combat.CombatAction();
            }
        }

        public void ChangeEnemyType(EnemyType type)
        {
            if (Resources.currentGun)
            {
                Resources.currentGun.gameObject.SetActive(false);
            }

            Resources.currentGun = FindGunForType(type);
            Resources.currentGun.gameObject.SetActive(true);

            // Instantiate and swap the combat module polymorphically
            NavMeshAgent agent = GetComponent<NavMeshAgent>();
            Combat = config.combat.CreateCombatInstance(this, transform, agent, Sensors, Movement, Resources);

            // Initialize the new combat module
            Combat.Init();
        }

        PlayerShootingSystem.Gun FindGunForType(EnemyType type)
        {
            if (type == EnemyType.Rambenemy)
            {
                return Resources.shotgunWeapon;
            }

            return Resources.glockWeapon;
        }
    }
}