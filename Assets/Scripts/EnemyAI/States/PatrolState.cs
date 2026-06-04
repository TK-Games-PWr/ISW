namespace EnemySystem.States
{
    public class PatrolState : IEnemyState
    {
        AICore _brain;

        public PatrolState(AICore brain)
        {
            _brain = brain;
        }

        public void Enter()
        {
        }

        public void Tick()
        {
        }

        public void Update()
        {
            _brain.Movement.UpdatePatrolState();
        }

        public void Exit()
        {
        }
    }
}