using System;

namespace Core {
	public interface IActorProxy {
		int Target { get; }
		void SendReq(string method, object param, Action<object> retback = null);
		void SendCmd(string method, object param);
	}

	public class ActorProxy : IActorProxy {
		private ActorContext source;
		private int target;

		public int Target { get { return target; } }

		public ActorProxy(ActorContext srcctx, int tartid) {
			source = srcctx;
			target = tartid;
		}

		public void SendReq(string method, object param, Action<object> retback = null) {
			source.SendCall(ActorMessage.REQ, target, method, param, retback);
		}

		public void SendCmd(string method, object param) {
			source.SendCall(ActorMessage.CMD, target, method, param, null);
		}
	}

	public class ActorProxyFactory {
		public static IActorProxy Create(ActorContext srcctx, int tartid) {
			return new ActorProxy(srcctx, tartid);
		}
	}
}