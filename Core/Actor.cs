using System;
using System.Reflection;

namespace Core {
    public interface IActor {
        ActorContext Context { get; set; }
        void Start();
        void Handle(ActorMessage msg, Action<object> retback);
    }

    public class Actor : IActor {
        public ActorContext Context { get; set; }
        protected Action<object> retback;

        public virtual void Start() { }

        public virtual void Handle(ActorMessage msg, Action<object> retback) {
            this.retback = retback;
            MethodInfo method = GetType().GetMethod(msg.Method, BindingFlags.Public | BindingFlags.Instance);
            method.Invoke(this, msg.Parameters);
            this.retback = null;
        }
    }
}
