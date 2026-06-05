using UnityEngine;

namespace EnemySystem
{
    public class EnemyAlertSystem
    {
        public AlertLevel CurrentAlertLevel { get; private set; } = AlertLevel.None;
        public float TriggerMultiplier { get; set; } = 0f;

        readonly EnemyBrain _brain;
        readonly EnemySensors _sensors;
        readonly AlertConfig _config;

        float _lastAlertTime = 0f;

        public EnemyAlertSystem(EnemyBrain brain, EnemySensors sensors, AlertConfig config)
        {
            _brain = brain;
            _sensors = sensors;
            _config = config;
        }

        public void Tick(float tickRate)
        {
            if (_brain.CurrentAgentState != AgentState.Combat)
            {
                UpdateAlertSystem(tickRate);
            }
        }

        void UpdateAlertSystem(float tickRate)
        {
            if (_sensors.IsPlayerVisible)
            {
                float distanceToPlayer = Vector3.Distance(_brain.transform.position, _sensors.PlayerTransform.position);
                TriggerMultiplier += tickRate * _config.alertSensitivity / distanceToPlayer;
                TriggerMultiplier = Mathf.Clamp(TriggerMultiplier, 0f, 10f);
                _sensors.UpdateLastKnownPosition();
                _lastAlertTime = Time.time;

                DetermineAlertLevel();
            }
            else
            {
                if (_brain.CurrentAgentState != AgentState.Alert && Time.time - _lastAlertTime >= _config.triggerMultiplierTimeout)
                {
                    if (TriggerMultiplier > 0)
                    {
                        TriggerMultiplier -= tickRate;
                    }
                    else
                    {
                        TriggerMultiplier = 0f;
                    }
                }

                if (_brain.CurrentAgentState != AgentState.Combat && Time.time - _lastAlertTime > _config.timeToLoseAlertLevel)
                {
                    if (CurrentAlertLevel == AlertLevel.None)
                    {
                        _brain.ChangeState(AgentState.Patrol);
                    }
                }
            }
        }
        
        internal void OnSoundHeard(Vector3 soundOrigin, float baseVolume, float distance, float range, bool capAlertLevel, AnimationCurve falloffCurve)
        {
            float dist = Mathf.Clamp01((range - distance) / range);
            dist = baseVolume * (falloffCurve?.Evaluate(1f - dist) ?? dist);
            if (!capAlertLevel || TriggerMultiplier < 0.76f)
            {
                TriggerMultiplier += capAlertLevel ? Mathf.Min(dist, 0.76f - TriggerMultiplier) : dist;
            }
            _lastAlertTime = Time.time;
            if(TriggerMultiplier > 0.75f) _sensors.UpdateLastKnownPosition(soundOrigin); // enemy will go to the sound it hears if alerted
            DetermineAlertLevel();
        }

        internal void DetermineAlertLevel(float tm = -1f)
        {
            if (tm >= 0f) TriggerMultiplier = tm;
            if (TriggerMultiplier <= 0f) SetAlertLevel(AlertLevel.None);
            else if (TriggerMultiplier <= 0.25f) SetAlertLevel(AlertLevel.Low);
            else if (TriggerMultiplier <= 0.75f) SetAlertLevel(AlertLevel.Medium);
            else if (TriggerMultiplier <= 1.0f) SetAlertLevel(AlertLevel.High);
            else SetAlertLevel(AlertLevel.Extreme);
        }

        void SetAlertLevel(AlertLevel newLevel)
        {
            if (CurrentAlertLevel == newLevel) return;
            CurrentAlertLevel = newLevel;
            
            switch (CurrentAlertLevel)
            {
                case AlertLevel.None: case AlertLevel.Low:
                    _brain.ChangeState(AgentState.Patrol);
                    break;
                case AlertLevel.Medium:
                case AlertLevel.High:
                    _brain.ChangeState(AgentState.Alert);
                    break;
                case AlertLevel.Extreme:
                    _brain.ChangeState(AgentState.Combat);
                    break;
            }
        }
        
        // Called by EnemyResources when damaged from stealth
        internal void ForceAlertSpike()
        {
            if (_brain.CurrentAgentState != AgentState.Combat)
            {
                TriggerMultiplier = 2f;
                DetermineAlertLevel();
            }
        }
    }
}