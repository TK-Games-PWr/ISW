using UnityEngine;
using System;
using System.Collections.Generic;
using EnemySystem.States;
using Sirenix.OdinInspector;
using UnityEngine.AI;

namespace EnemySystem
{
    [RequireComponent(typeof(EnemyResources))]
    public partial class EnemyBrain : MonoBehaviour, IHearingTarget
    {
        // --- Events ---
        public static event Action<EnemyBrain, float, AlertLevel> OnAlertChanged;

        [Header("Configuration")]
        public EnemyConfig baseConfig;
        public EnemyConfig Config { get; private set; }

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
        
        float _tickTimer;

        public bool IsDead => Resources.IsDead;

        [Tooltip("Disables hearing system connection, useful for putting agent in menu background.")] [SerializeField]
        bool isDeaf = false;
        
        [Header("Stat Overrides")]
        [ListDrawerSettings(ShowIndexLabels = false)]
        public List<StatOverride> statOverrides = new ();
        
        internal AgentState currentAgentState = AgentState.Patrol;

        void Awake()
        {
            Config = Instantiate(baseConfig);
            ApplyOverrides();
            
            Resources = GetComponent<EnemyResources>();
            Resources.Init();
            
            NavMeshAgent agent = GetComponent<NavMeshAgent>();
            AIAnimationController animController = GetComponent<AIAnimationController>();
            
            Sensors = new EnemySensors(this, transform, Config.sensors);
            Movement = new EnemyMovement(this, transform, agent, Sensors, animController, Config.movement, patrolPoints);
            AlertSystem = new EnemyAlertSystem(this, Sensors, Movement, Config.alert);
            
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
            if (_tickTimer >= Config.tickRate)
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
            AlertSystem.Tick(Config.tickRate);
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
            if (currentAgentState == newAgentState) return;
            _currentState?.Exit();
            
            currentAgentState = newAgentState;

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
            CombatConfig selectedConfig = Config.GetCombatConfig(type);
            
            Resources.SwitchWeapon(selectedConfig.preferredWeapon);
            
            NavMeshAgent agent = GetComponent<NavMeshAgent>();
            Combat = selectedConfig.CreateCombatInstance(this, transform, agent, Sensors, Movement, Resources);
            
            Combat.Init();
        }
    }
}