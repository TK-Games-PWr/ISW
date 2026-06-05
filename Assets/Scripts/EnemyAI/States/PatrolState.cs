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
            _brain.Movement.UpdatePatrolState();
        }

        public void Update()
        {
        }

        public void Exit()
        {
        }
    }
}