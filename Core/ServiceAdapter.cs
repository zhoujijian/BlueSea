namespace Core {
    public class ServiceAdapter : IService {
        public virtual void OnStart() { }
        public virtual void OnUpdate() { }
        public virtual void OnDestroy() { }
        public virtual void OnGameEvent(GameEvent evt) { }
    }
}