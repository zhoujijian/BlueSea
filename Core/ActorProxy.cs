using System;

namespace Core {
	public interface IActorProxy {
		int Target { get; }
		void SendReq(string method, object param, Action<object> retback = null);
		ActorWork SendReqAsync(string method, object param);
		ActorWork<TResult> SendReqAsync<TResult>(string method, object param);
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

		public ActorWork SendReqAsync(string method, object param) {
			ActorWork awaitor = new ActorWork();
			source.SendCall(ActorMessage.REQ, target, method, param, _ => {
				awaitor.CallContinue();
			});
			return awaitor;
		}

		public ActorWork<TResult> SendReqAsync<TResult>(string method, object param) {
			ActorWork<TResult> awaitor = new ActorWork<TResult>();
			source.SendCall(ActorMessage.REQ, target, method, param, ret => {
				awaitor.CallContinue(ret);
			});
			return awaitor;
		}
	}

	public class ActorProxyFactory {
		public static IActorProxy Create(ActorContext srcctx, int tartid) {
			return new ActorProxy(srcctx, tartid);
		}
	}
}