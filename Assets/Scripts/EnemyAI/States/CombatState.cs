using UnityEngine;

namespace EnemySystem.States
{
    public class CombatState : IEnemyState
    {
        EnemyBrain _brain;
        float _combatLostPlayerTime = 0f;
        float _timeInCombat = 0f;

        bool _hasLos = false;
        float _distanceToPlayer = Mathf.Infinity;

        public CombatState(EnemyBrain brain)
        {
            _brain = brain;
        }

        public void Enter()
        {
            _timeInCombat = 0f;
            _combatLostPlayerTime = 0f;
            _brain.Movement.StopAllMovementCoroutines();
            _brain.Sensors.AlertNearbyEnemies();
            _brain.Combat.ResetCombatState();
            _brain.Movement.UpdateAngularSpeed(AgentState.Combat);
        }

        public void Tick()
        {
            _distanceToPlayer = Vector3.Distance(_brain.transform.position, _brain.Sensors.PlayerTransform.position);
            
            _brain.Combat.HandleCombatMovement(_brain.Sensors.PlayerTransform, _distanceToPlayer, _hasLos);
        }

        public void Update()
        {
            _timeInCombat += Time.deltaTime;
            if (_brain.Sensors.PlayerTransform == null || _brain.Combat.IsReloading) return;

            _hasLos = _brain.Sensors.IsPlayerVisible;
            _brain.Combat.RotateTowardsPlayer(_brain.Sensors.PlayerTransform);

            if (_brain.Config.alert.fightDelay > _timeInCombat) return;

            if (!_hasLos)
            {
                _combatLostPlayerTime += Time.deltaTime;
                if (_combatLostPlayerTime >= _brain.Config.alert.endCombatTimeout)
                {
                    _brain.AlertSystem.TriggerMultiplier = 0.9f;
                    _brain.AlertSystem.DetermineAlertLevel();
                    _combatLostPlayerTime = 0f;
                    return;
                }
            }
            else
            {
                _combatLostPlayerTime = 0f;
            }

            // Handle Shooting
            if (_distanceToPlayer <= _brain.Combat.WeaponRange && _hasLos)
            {
                _brain.Combat.CombatAction();
            }
        }
        
        public void Exit()
        {
        }
    }
}