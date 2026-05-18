using UnityEngine;
using System;
using UnityEngine.InputSystem.Controls;
using static EnemyManager;

namespace EnemySystem
{
    [RequireComponent(typeof(EnemySensors), typeof(EnemyMovement), typeof(EnemyCombat))]
    [RequireComponent(typeof(EnemyResources))]
    public class AICore : MonoBehaviour, IHearingTarget
    {
        // --- Enums ---
        public enum AIState
        {
            Patrol,
            Alert,
            Combat
        }

        public enum AlertLevel
        {
            None,
            Low,
            Medium,
            High,
            Extreme
        }

        // --- Events ---
        public static event Action<AICore, float, AlertLevel> OnAlertChanged;

        [Header("Alert System")] [SerializeField]
        float alertSensitivity = 1f;

        [SerializeField] float timeToLoseAlertLevel = 3f;

        [Tooltip("Delay before agents starts shooting in seconds")] [SerializeField]
        float fightDelay = 1f;

        [Tooltip("Time before TM starts decreasing")] [SerializeField]
        float triggerMultiplierTimeout = 2f;

        [Tooltip("Time after which enemy ends combat and returns to patrol")]
        [SerializeField] float endCombatTimeout = 10f;

        float _combatLostPlayerTime = 0f;

        // --- State Variables ---
        public AIState currentState = AIState.Patrol;
        public AlertLevel currentAlertLevel = AlertLevel.None;
        public float triggerMultiplier = 0f;

        // --- References ---
        EnemySensors _sensors;
        EnemyMovement _movement;
        EnemyCombat _combat;
        EnemyResources _resources;

        [SerializeField] EnemyCombat baseEnemyCombat;
        [SerializeField] EnemyCombat rambEnemyCombat;
        [SerializeField] EnemyCombat coverEnemyCombat;

        float _lastAlertTime = 0f;
        public float TimeInCombat { get; private set; } = 0f;

        public bool IsDead => _resources.IsDead;

        void Awake()
        {
            _sensors = GetComponent<EnemySensors>();
            _movement = GetComponent<EnemyMovement>();
            _resources = GetComponent<EnemyResources>();
            ChangeEnemyType(EnemyType.Normal);
        }
        
        async void OnEnable()
        {
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

        async void OnDisable()
        {
            await Awaitable.EndOfFrameAsync();
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
            if (_resources.IsDead) return;

            if (currentState != AIState.Combat)
            {
                UpdateAlertSystem();
            }

            OnAlertChanged?.Invoke(this, triggerMultiplier, currentAlertLevel);

            switch (currentState)
            {
                case AIState.Patrol:
                    _movement.UpdatePatrolState();
                    break;
                case AIState.Alert:
                    _movement.UpdateAngularSpeed(AIState.Alert);
                    // Movement handled by Coroutines triggered in SetAlertLevel
                    break;
                case AIState.Combat:
                    UpdateCombatLogic();
                    _movement.UpdateAngularSpeed(AIState.Combat);
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
            if(triggerMultiplier > 0.75f) _sensors.UpdateLastKnownPosition(soundOrigin); // enemy will go to the sound it hears if alerted
            DetermineAlertLevel();
        }

        void UpdateAlertSystem()
        {
            if (_sensors.HasLineOfSight())
            {
                float distanceToPlayer = Vector3.Distance(transform.position, _sensors.PlayerTransform.position);
                triggerMultiplier += Time.deltaTime * alertSensitivity / distanceToPlayer;
                triggerMultiplier = Mathf.Clamp(triggerMultiplier, 0f, 10f);
                _sensors.UpdateLastKnownPosition();
                _lastAlertTime = Time.time;

                DetermineAlertLevel();
            }
            else
            {
                if (currentState != AIState.Alert && Time.time - _lastAlertTime >= triggerMultiplierTimeout)
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

                if (currentState != AIState.Combat && Time.time - _lastAlertTime > timeToLoseAlertLevel)
                {
                    if (currentAlertLevel == AlertLevel.None)
                    {
                        ChangeState(AIState.Patrol);
                    }
                }
            }
        }

        internal void DetermineAlertLevel()
        {
            if (triggerMultiplier <= 0) return;

            if (triggerMultiplier <= 0.25f) SetAlertLevel(AlertLevel.Low);
            else if (triggerMultiplier <= 0.75f) SetAlertLevel(AlertLevel.Medium);
            else if (triggerMultiplier <= 1.0f) SetAlertLevel(AlertLevel.High);
            else SetAlertLevel(AlertLevel.Extreme);
        }

        void SetAlertLevel(AlertLevel newLevel)
        {
            if (currentAlertLevel == newLevel) return;
            currentAlertLevel = newLevel;

            _movement.StopAllMovementCoroutines();

            switch (currentAlertLevel)
            {
                case AlertLevel.Low:
                    _movement.ResumeDefaultMovement();
                    currentAlertLevel = AlertLevel.None;
                    break;
                case AlertLevel.Medium:
                    ChangeState(AIState.Alert);
                    _movement.StartLookAround(3f, this);
                    break;
                case AlertLevel.High:
                    ChangeState(AIState.Alert);
                    _movement.StartInvestigate(5f, _sensors.LastKnownPosition, this);
                    break;
                case AlertLevel.Extreme:
                    ChangeState(AIState.Combat);
                    _sensors.AlertNearbyEnemies();
                    break;
            }
        }

        internal void ChangeState(AIState newState)
        {
            if (currentState == newState) return;
            currentState = newState;

            switch (newState)
            {
                case AIState.Patrol:
                    _movement.ResumeDefaultMovement();
                    currentAlertLevel = AlertLevel.None;
                    break;
                case AIState.Alert:
                    _movement.SetSpeedMultiplier(1f);
                    break;
                case AIState.Combat:
                    // Speed set dynamically in combat loop
                    break;
            }
        }

        void UpdateCombatLogic()
        {
            TimeInCombat += Time.deltaTime;
            if (_sensors.PlayerTransform == null || _combat.IsReloading) return;

            bool hasLos = _sensors.HasLineOfSight();
            float distanceToPlayer = Vector3.Distance(transform.position, _sensors.PlayerTransform.position);

            // Handle Movement
            _combat.HandleCombatMovement(_sensors.PlayerTransform, distanceToPlayer, hasLos);

            if (fightDelay > TimeInCombat) return;

            if (!hasLos)
            {
                _combatLostPlayerTime += Time.deltaTime;
                if (_combatLostPlayerTime >= endCombatTimeout)
                {
                    triggerMultiplier = 0.9f;
                    DetermineAlertLevel();
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
            if (currentState != AIState.Combat)
            {
                triggerMultiplier = 2f;
                DetermineAlertLevel();
            }
        }

        public void ChangeEnemyType(EnemyType type)
        {
            _resources.currentGun.gameObject.SetActive(false);
            _combat = GetCombatModule(type);
            _resources.currentGun = _combat.classGun;
            _resources.currentGun.gameObject.SetActive(true);
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