namespace EnemySystem.States
{
    public class PatrolState : IEnemyState
    {
        EnemyBrain _brain;

        public PatrolState(EnemyBrain brain)
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