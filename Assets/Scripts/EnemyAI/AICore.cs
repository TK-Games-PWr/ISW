using UnityEngine;
using System;
using UnityEngine.InputSystem.Controls;

namespace EnemySystem
{
    [RequireComponent(typeof(EnemySensors), typeof(EnemyMovement), typeof(EnemyCombat))]
    [RequireComponent(typeof(EnemyHealth))]
    public class AICore : MonoBehaviour
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

        public enum EnemyType
        {
            Glock,
            Shotgun
        }

        // --- Events ---
        public static event Action<AICore, float, AlertLevel> OnAlertChanged;

        [Header("General Settings")] [SerializeField]
        EnemyType enemyType = EnemyType.Glock;

        [Header("Alert System")] [SerializeField]
        float alertSensitivity = 1f;

        [SerializeField] float timeToLoseAlertLevel = 3f;

        [Tooltip("Delay before agents starts shooting in seconds")] [SerializeField]
        float fightDelay = 1f;

        [Tooltip("Time before TM starts decreasing")] [SerializeField]
        float triggerMultiplierTimeout = 2f;

        // --- State Variables ---
        public AIState currentState = AIState.Patrol;
        public AlertLevel currentAlertLevel = AlertLevel.None;
        public float triggerMultiplier = 0f;

        // --- References ---
        EnemySensors sensors;
        EnemyMovement movement;
        EnemyCombat combat;
        EnemyHealth health;

        float lastAlertTime = 0f;
        public float timeInCombat { get; private set; } = 0f;

        void Awake()
        {
            sensors = GetComponent<EnemySensors>();
            movement = GetComponent<EnemyMovement>();
            combat = GetComponent<EnemyCombat>();
            health = GetComponent<EnemyHealth>();
        }

        void Update()
        {
            if (health.IsDead) return;

            if (currentState != AIState.Combat)
            {
                UpdateAlertSystem();
            }

            OnAlertChanged?.Invoke(this, triggerMultiplier, currentAlertLevel);

            switch (currentState)
            {
                case AIState.Patrol:
                    movement.UpdatePatrolState();
                    break;
                case AIState.Alert:
                    // Movement handled by Coroutines triggered in SetAlertLevel
                    break;
                case AIState.Combat:
                    UpdateCombatLogic();
                    break;
            }
        }

        public void HearingUpdate(float baseVolume, float distance, float range, AnimationCurve falloffCurve = null)
        {
            float dist = Mathf.Clamp01((range - distance) / range);
            triggerMultiplier += baseVolume * (falloffCurve?.Evaluate(1f - dist) ?? dist);
            lastAlertTime = Time.time;
            // sensors.UpdateLastKnownPosition(); // enemy would look at every sound he hears
            DetermineAlertLevel();
        }

        void UpdateAlertSystem()
        {
            if (sensors.HasLineOfSight())
            {
                float distanceToPlayer = Vector3.Distance(transform.position, sensors.PlayerTransform.position);
                triggerMultiplier += Time.deltaTime * alertSensitivity / distanceToPlayer;
                triggerMultiplier = Mathf.Clamp(triggerMultiplier, 0f, 10f);
                sensors.UpdateLastKnownPosition();
                lastAlertTime = Time.time;

                DetermineAlertLevel();
            }
            else
            {
                if (currentState != AIState.Alert && Time.time - lastAlertTime >= triggerMultiplierTimeout)
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

                if (currentState != AIState.Combat && Time.time - lastAlertTime > timeToLoseAlertLevel)
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

            movement.StopAllMovementCoroutines();

            switch (currentAlertLevel)
            {
                case AlertLevel.Low:
                    movement.ResumeDefaultMovement();
                    currentAlertLevel = AlertLevel.None;
                    break;
                case AlertLevel.Medium:
                    ChangeState(AIState.Alert);
                    movement.StartLookAround(3f, this);
                    break;
                case AlertLevel.High:
                    ChangeState(AIState.Alert);
                    movement.StartInvestigate(5f, sensors.LastKnownPosition, this);
                    break;
                case AlertLevel.Extreme:
                    ChangeState(AIState.Combat);
                    sensors.AlertNearbyEnemies();
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
                    movement.ResumeDefaultMovement();
                    currentAlertLevel = AlertLevel.None;
                    break;
                case AIState.Alert:
                    movement.SetSpeedMultiplier(1f);
                    break;
                case AIState.Combat:
                    // Speed set dynamically in combat loop
                    break;
            }
        }

        void UpdateCombatLogic()
        {
            timeInCombat += Time.deltaTime;
            if (sensors.PlayerTransform == null || combat.IsReloading) return;

            bool hasLOS = sensors.HasLineOfSight();
            float distanceToPlayer = Vector3.Distance(transform.position, sensors.PlayerTransform.position);

            // Handle Movement
            movement.HandleCombatMovement(sensors.PlayerTransform, distanceToPlayer, combat.WeaponRange,
                combat.OptimalDistance, hasLOS);

            if (fightDelay > timeInCombat) return;

            // Handle Shooting
            if (distanceToPlayer <= combat.WeaponRange && hasLOS)
            {
                combat.CombatAction(distanceToPlayer);
            }
        }

        // Called by EnemyHealth when damaged from stealth
        internal void ForceAlertSpike()
        {
            if (currentState != AIState.Combat)
            {
                triggerMultiplier = 2f;
                DetermineAlertLevel();
            }
        }
    }
}