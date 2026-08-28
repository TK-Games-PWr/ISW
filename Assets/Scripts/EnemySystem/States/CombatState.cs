using UnityEngine;

namespace EnemySystem.States
{
    public class CombatState : IEnemyState
    {
        readonly EnemyBrain _brain;
        readonly EnemySensors _sensors;
        readonly EnemyAlertSystem _alertSystem;
        EnemyCombat Combat => _brain.Combat;
        readonly AlertConfig _alertConfig;
        
        float _combatLostPlayerTime = 0f;
        float _timeInCombat = 0f;
        bool _hasLos = false;
        float _distanceToPlayer = Mathf.Infinity;

        public CombatState(EnemyBrain brain, EnemySensors sensors, EnemyAlertSystem alertSystem)
        {
            _brain = brain;
            _sensors = sensors;
            _alertSystem = alertSystem;
            _alertConfig = brain.Config.alert;
        }

        public void Enter()
        {
            _timeInCombat = 0f;
            _combatLostPlayerTime = 0f;
            _brain.Movement.StopAllMovementCoroutines();
            _sensors.AlertNearbyEnemies();
            Combat.ResetCombatState();
            _brain.Movement.UpdateAngularSpeed(AgentState.Combat);
        }

        public void Tick()
        {
            _distanceToPlayer = Vector3.Distance(_brain.transform.position, _sensors.PlayerTransform.position);
            
            Combat.HandleCombatMovement(_sensors.PlayerTransform, _distanceToPlayer, _hasLos);
        }

        public void Update()
        {
            _timeInCombat += Time.deltaTime;
            if (_sensors.PlayerTransform == null) return;

            _hasLos = _sensors.IsPlayerVisible;
            Combat.RotateTowardsPlayer(_sensors.PlayerTransform);

            if (_alertConfig.fightDelay > _timeInCombat) return;

            if (!_hasLos)
            {
                _combatLostPlayerTime += Time.deltaTime;
                if (_combatLostPlayerTime >= _alertConfig.endCombatTimeout)
                {
                    _alertSystem.TriggerMultiplier = 0.9f;
                    _alertSystem.DetermineAlertLevel();
                    _combatLostPlayerTime = 0f;
                    return;
                }
            }
            else
            {
                _combatLostPlayerTime = 0f;
            }

            // Handle Shooting
            if (_distanceToPlayer <= Combat.WeaponRange && _hasLos && !Combat.IsReloading)
            {
                Combat.CombatAction();
            }
        }
        
        public void Exit()
        {
        }
    }
}