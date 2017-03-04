using System;
using System.Threading.Tasks;

namespace Core {
	public interface IActorContainer {
		ActorContext Context { get; }
		ActorMailbox Mailbox { get; }
		void Post(ActorMessage message);
	}

	public class ActorContainer : IActorContainer {
		public ActorContext Context { get; private set; }
		public ActorMailbox Mailbox { get; private set; }

		public ActorContainer(ActorContext context) {
			this.Context = context;
			this.Mailbox = new ActorMailbox(context);
		}

		public void Post(ActorMessage msg) {
			Mailbox.Post(msg);
		}
	}

	// TODO: communicate to remote
	public class RemoteContainer : IActorContainer {
		public ActorContext Context { get; private set; }
		public ActorMailbox Mailbox { get; private set; }

		public void Post(ActorMessage msg) { }
	}

	public interface IActor {
		ActorContext Context { get; set; }
		void Handle(ActorMessage message, Action<object> retback);
		Task HandleCommandAsync(ActorMessage cmd);
		Task<object> HandleRequestAsync(ActorMessage req);
	}

	public class Actor : IActor {
		public ActorContext Context { get; set; }
		public virtual void Handle(ActorMessage message, Action<object> retback) { }
		public virtual Task HandleCommandAsync(ActorMessage cmd) { return Task.CompletedTask; }
		public virtual Task<object> HandleRequestAsync(ActorMessage req) { return Task.FromResult<object>(null); }
	}
}