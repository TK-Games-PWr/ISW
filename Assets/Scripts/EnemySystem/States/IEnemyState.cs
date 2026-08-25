namespace EnemySystem.States
{
    public interface IEnemyState
    {
        void Enter();
        void Tick();
        void Update();
        void Exit();
    }
}