using System;
using System.Collections.Generic;

namespace Core {
	public interface IActor {
		ActorContext Context { get; set; }
		void Handle(ActorMessage message, Action<object> retback);
	}

	public class Actor : IActor {
		public ActorContext Context { get; set; }
		public virtual void Handle(ActorMessage message, Action<object> retback) { }
	}

	public class ActorMailbox {
		private Queue<ActorMessage> messageQ = new Queue<ActorMessage>();

		public void Send(int src, ActorMessage msg) { }
	}
}