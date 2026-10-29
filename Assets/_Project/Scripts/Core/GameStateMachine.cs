namespace SlimeHub.Core
{
    public interface IGameState
    {
        void Enter();
        void Update();
        void Exit();
    }

    public sealed class GameStateMachine
    {
        public IGameState Current { get; private set; }

        public void Change(IGameState next)
        {
            if (ReferenceEquals(Current, next))
                return;
            Current?.Exit();
            Current = next;
            Current?.Enter();
        }

        public void Update()
        {
            Current?.Update();
        }
    }

    public sealed class LambdaState : IGameState
    {
        readonly System.Action enter;
        readonly System.Action tick;
        readonly System.Action exit;

        public LambdaState(System.Action tick, System.Action enter = null, System.Action exit = null)
        {
            this.tick = tick;
            this.enter = enter;
            this.exit = exit;
        }

        public void Enter() => enter?.Invoke();
        public void Update() => tick?.Invoke();
        public void Exit() => exit?.Invoke();
    }
}
