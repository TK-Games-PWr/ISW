namespace EnemySystem.States
{
    public class PatrolState : IEnemyState
    {
        readonly EnemyBrain _brain;

        public PatrolState(EnemyBrain brain)
        {
            _brain = brain;
        }

        public void Enter()
        {
            _brain.Movement.ResumeDefaultMovement();
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
            _brain.Movement.StopAllMovementCoroutines();
        }
    }
}