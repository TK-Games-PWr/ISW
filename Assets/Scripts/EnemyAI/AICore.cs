using UnityEngine;
using System;
using EnemySystem.States;
using UnityEngine.AI;

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

        // --- References ---
        public EnemySensors Sensors { get; private set; }
        public EnemyMovement Movement { get; private set; }
        public EnemyCombat Combat { get; private set; }
        public EnemyResources Resources { get; private set; }
        public EnemyAlertSystem AlertSystem { get; private set; }
        
        // --- States ---
        IEnemyState _currentState;
        PatrolState _patrolState;
        AlertState _alertState;
        CombatState _combatState;

        float _lastAlertTime = 0f;
        float _tickTimer = 0f;
        [SerializeField] float tickRate = 0.1f;

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
            
            _patrolState = new PatrolState(this);
            _alertState = new AlertState(this);
            _combatState = new CombatState(this);
            
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
            
            _tickTimer += Time.deltaTime;
            if (_tickTimer >= tickRate)
            {
                Tick();
                _tickTimer = 0f;
            }
            
            _currentState?.Update();

            OnAlertChanged?.Invoke(this, AlertSystem.TriggerMultiplier, AlertSystem.CurrentAlertLevel);
        }

        void Tick()
        {
            Sensors.Tick();
            AlertSystem.Tick(tickRate);
            _currentState?.Tick();
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
            _currentState?.Exit();
            
            _currentAgentState = newAgentState;

            _currentState = newAgentState switch
            {
                AgentState.Patrol => _patrolState,
                AgentState.Alert => _patrolState,
                AgentState.Combat => _combatState,
                _ => _currentState
            };

            if (newAgentState != AgentState.Combat && Combat != null)
            {
                Combat.ResetCombatState();
            }

            _currentState?.Enter();
        }

        public void ChangeEnemyType(EnemyType type)
        {
            CombatConfig selectedConfig = config.GetCombatConfig(type);
            
            Resources.SwitchWeapon(selectedConfig.preferredWeapon);
            
            NavMeshAgent agent = GetComponent<NavMeshAgent>();
            Combat = selectedConfig.CreateCombatInstance(this, transform, agent, Sensors, Movement, Resources);
            
            Combat.Init();
        }
    }
}