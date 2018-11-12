namespace Core {
    public interface IService {
        void OnStart();
        void OnUpdate();
        void OnDestroy();
        void OnGameEvent(GameEvent evt);
    }
}