namespace EnemySystem.States
{
    public class AlertState : IEnemyState
    {
        readonly EnemyBrain _brain;

        public AlertState(EnemyBrain brain)
        {
            _brain = brain;
        }

        AlertLevel _lastAlertLevel;

        public void Enter()
        {
            _brain.Movement.SetSpeedMultiplier(1f);
            _brain.Movement.UpdateAngularSpeed(AgentState.Alert);
            _lastAlertLevel = AlertLevel.None; // Force tick update
        }

        public void Tick()
        {
            if (_lastAlertLevel != _brain.AlertSystem.CurrentAlertLevel)
            {
                _lastAlertLevel = _brain.AlertSystem.CurrentAlertLevel;
                _brain.Movement.StopAllMovementCoroutines();

                if (_lastAlertLevel == AlertLevel.Medium)
                {
                    _brain.Movement.StartLookAround(3f, _brain);
                }
                else if (_lastAlertLevel == AlertLevel.High)
                {
                    _brain.Movement.StartInvestigate(5f, _brain.Sensors.LastKnownPosition, _brain);
                }
            }
        }

        public void Update()
        {
        }

        public void Exit()
        {
            _brain.Movement.StopAllMovementCoroutines();
        }
    }
}