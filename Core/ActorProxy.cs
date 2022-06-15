using System;

namespace Core {
    public interface IActorProxy {
        int Target { get; }
        ActorWork SendReqAsync(string method, params object[] parameters);
        ActorWork<TResult> SendReqAsync<TResult>(string method, params object[] parameters);
        void SendCmd(string method, params object[] parameters);
    }

    public class PipelineAttribute : Attribute { }

    public class ActorProxy : IActorProxy {
        private ActorContext source;
        private int target;

        public int Target { get { return target; } }

        public ActorProxy(ActorContext srcctx, int tartid) {
            source = srcctx;
            target = tartid;
        }

        public void SendCmd(string method, params object[] param) {
            source.SendCall(ActorMessage.CMD, target, method, param, null);
        }

        public ActorWork SendReqAsync(string method, params object[] parameters) {
            ActorWork awaitor = new ActorWork();
            source.SendCall(ActorMessage.REQ, target, method, parameters, _ => {
                awaitor.CallContinue();
            });
            return awaitor;
        }

        public ActorWork<TResult> SendReqAsync<TResult>(string method, params object[] parameters) {
            ActorWork<TResult> awaitor = new ActorWork<TResult>();
            source.SendCall(ActorMessage.REQ, target, method, parameters, ret => {
                awaitor.CallContinue(ret);
            });
            return awaitor;
        }
    }

    public class ProxyNameAttribute : Attribute {
        public string Name { get; private set; }

        public ProxyNameAttribute(string name) {
            Name = name;
        }
    }

    public class ActorProxyFactory {
        public static IActorProxy Create(ActorContext srcctx, int tartid) {
            return new ActorProxy(srcctx, tartid);
        }
    }
}
