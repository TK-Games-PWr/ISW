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
        internal EnemySensors sensors;
        internal  EnemyMovement movement;
        EnemyCombat _combat;
        internal EnemyResources resources;

        [SerializeField] EnemyCombat baseEnemyCombat;
        [SerializeField] EnemyCombat rambEnemyCombat;
        [SerializeField] EnemyCombat coverEnemyCombat;

        float _lastAlertTime = 0f;
        public float TimeInCombat { get; private set; } = 0f;

        public bool IsDead => resources.IsDead;

        [Tooltip("Disables hearing system connection, useful for putting agent in menu background.")] [SerializeField]
        bool isDeaf = false;
        
        // --- State Variables ---
        [Header("DEBUG")]
        public AgentState currentAgentState = AgentState.Patrol;
        public AlertLevel currentAlertLevel = AlertLevel.None;
        public float triggerMultiplier = 0f;

        void Awake()
        {
            resources = GetComponent<EnemyResources>();
            NavMeshAgent agent = GetComponent<NavMeshAgent>();
            AIAnimationController animController = GetComponent<AIAnimationController>();
            
            sensors = new EnemySensors(this, transform, config.sensors);
            movement = new EnemyMovement(this, transform, agent, sensors, animController, config.movement, patrolPoints);
            
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
            if (resources.IsDead) return;
            
            sensors.Tick();

            if (currentAgentState != AgentState.Combat)
            {
                UpdateAlertSystem();
            }

            OnAlertChanged?.Invoke(this, triggerMultiplier, currentAlertLevel);

            switch (currentAgentState)
            {
                case AgentState.Patrol:
                    movement.UpdatePatrolState();
                    break;
                case AgentState.Alert:
                    movement.UpdateAngularSpeed(AgentState.Alert);
                    // Movement handled by Coroutines triggered in SetAlertLevel
                    break;
                case AgentState.Combat:
                    UpdateCombatLogic();
                    movement.UpdateAngularSpeed(AgentState.Combat);
                    break;
            }
        }
        
        public Vector3 GetHearingPosition()
        {
            return transform.position + new Vector3(0, 1.5f, 0); 
        }

        public void OnSoundHeard(Vector3 soundOrigin, float baseVolume, float distance, float range, bool capAlertLevel, AnimationCurve falloffCurve)
        {
            float dist = Mathf.Clamp01((range - distance) / range);
            dist = baseVolume * (falloffCurve?.Evaluate(1f - dist) ?? dist);
            if (!capAlertLevel || triggerMultiplier < 0.76f)
            {
                triggerMultiplier += capAlertLevel ? Mathf.Min(dist, 0.76f - triggerMultiplier) : dist;
            }
            _lastAlertTime = Time.time;
            if(triggerMultiplier > 0.75f) sensors.UpdateLastKnownPosition(soundOrigin); // enemy will go to the sound it hears if alerted
            DetermineAlertLevel();
        }

        void UpdateAlertSystem()
        {
            if (sensors.IsPlayerVisible)
            {
                float distanceToPlayer = Vector3.Distance(transform.position, sensors.PlayerTransform.position);
                triggerMultiplier += Time.deltaTime * config.alert.alertSensitivity / distanceToPlayer;
                triggerMultiplier = Mathf.Clamp(triggerMultiplier, 0f, 10f);
                sensors.UpdateLastKnownPosition();
                _lastAlertTime = Time.time;

                DetermineAlertLevel();
            }
            else
            {
                if (currentAgentState != AgentState.Alert && Time.time - _lastAlertTime >= config.alert.triggerMultiplierTimeout)
                {
                    if (triggerMultiplier > 0)
                    {
                        triggerMultiplier -= Time.deltaTime;
                    }
                    else
                    {
                        triggerMultiplier = 0f;
                    }
                }

                if (currentAgentState != AgentState.Combat && Time.time - _lastAlertTime > config.alert.timeToLoseAlertLevel)
                {
                    if (currentAlertLevel == AlertLevel.None)
                    {
                        ChangeState(AgentState.Patrol);
                    }
                }
            }
        }

        internal void DetermineAlertLevel(float tm = -1f)
        {
            if (tm >= 0f) triggerMultiplier = tm;
            if (triggerMultiplier <= 0f) SetAlertLevel(AlertLevel.None);
            else if (triggerMultiplier <= 0.25f) SetAlertLevel(AlertLevel.Low);
            else if (triggerMultiplier <= 0.75f) SetAlertLevel(AlertLevel.Medium);
            else if (triggerMultiplier <= 1.0f) SetAlertLevel(AlertLevel.High);
            else SetAlertLevel(AlertLevel.Extreme);
        }

        void SetAlertLevel(AlertLevel newLevel)
        {
            if (currentAlertLevel == newLevel) return;
            AlertLevel lastAlertLevel = currentAlertLevel;
            currentAlertLevel = newLevel;
            
            if ((lastAlertLevel is AlertLevel.None or AlertLevel.Low &&
                 newLevel is AlertLevel.None or AlertLevel.Low)) return;
            
            movement.StopAllMovementCoroutines();

            switch (currentAlertLevel)
            {
                case AlertLevel.None: case AlertLevel.Low:
                    ChangeState(AgentState.Patrol);
                    movement.ResumeDefaultMovement();
                    break;
                case AlertLevel.Medium:
                    ChangeState(AgentState.Alert);
                    movement.StartLookAround(3f, this);
                    break;
                case AlertLevel.High:
                    ChangeState(AgentState.Alert);
                    movement.StartInvestigate(5f, sensors.LastKnownPosition, this);
                    break;
                case AlertLevel.Extreme:
                    ChangeState(AgentState.Combat);
                    sensors.AlertNearbyEnemies();
                    break;
            }
        }

        internal void ChangeState(AgentState newAgentState)
        {
            if (currentAgentState == newAgentState) return;
            currentAgentState = newAgentState;

            switch (newAgentState)
            {
                case AgentState.Patrol:
                    break;
                case AgentState.Alert:
                    movement.SetSpeedMultiplier(1f);
                    break;
                case AgentState.Combat:
                    // Speed set dynamically in combat loop
                    break;
            }
        }

        void UpdateCombatLogic()
        {
            TimeInCombat += Time.deltaTime;
            if (sensors.PlayerTransform == null || _combat.IsReloading) return;

            bool hasLos = sensors.IsPlayerVisible;
            float distanceToPlayer = Vector3.Distance(transform.position, sensors.PlayerTransform.position);

            // Handle Movement
            _combat.HandleCombatMovement(sensors.PlayerTransform, distanceToPlayer, hasLos);

            if (config.alert.fightDelay > TimeInCombat) return;

            if (!hasLos)
            {
                _combatLostPlayerTime += Time.deltaTime;
                if (_combatLostPlayerTime >= config.alert.endCombatTimeout)
                {
                    triggerMultiplier = 0.9f;
                    DetermineAlertLevel();
                    _combatLostPlayerTime = 0f;
                    return;
                }
            }
            else
            {
                _combatLostPlayerTime = 0f;
            }

            // Handle Shooting
            if (distanceToPlayer <= _combat.WeaponRange && hasLos)
            {
                _combat.CombatAction();
            }
        }

        // Called by EnemyResources when damaged from stealth
        internal void ForceAlertSpike()
        {
            if (currentAgentState != AgentState.Combat)
            {
                triggerMultiplier = 2f;
                DetermineAlertLevel();
            }
        }

        public void ChangeEnemyType(EnemyType type)
        {
            resources.currentGun.gameObject.SetActive(false);
            _combat = GetCombatModule(type);
            resources.currentGun = _combat.classGun;
            resources.currentGun.gameObject.SetActive(true);
        }

        EnemyCombat GetCombatModule(EnemyType enemyType)
        {
            switch (enemyType)
            {
                case EnemyType.Rambenemy:
                    return rambEnemyCombat;
                case EnemyType.CoverGuy:
                    return coverEnemyCombat;
                default:
                    return baseEnemyCombat;
            }
        }
    }
}