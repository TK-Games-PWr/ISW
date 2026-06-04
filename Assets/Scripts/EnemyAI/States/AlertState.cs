namespace EnemySystem.States
{
    public class AlertState : IEnemyState
    {
        EnemyBrain _brain;

        public AlertState(EnemyBrain brain)
        {
            _brain = brain;
        }

        public void Enter()
        {
            _brain.Movement.SetSpeedMultiplier(1f);
            _brain.Movement.UpdateAngularSpeed(AgentState.Alert);
        }

        public void Tick()
        {
        }

        public void Update()
        {
        }

        public void Exit()
        {
        }
    }
}