namespace EnemySystem.States
{
    public class AlertState : IEnemyState
    {
        AICore _brain;

        public AlertState(AICore brain)
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