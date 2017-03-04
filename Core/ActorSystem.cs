using System;
using System.Threading;
using System.Collections.Generic;
using CUtil;

namespace Core {
	public class ActorSystem {
		public const int PRESERVE = 1000;

		private int actorid = PRESERVE;
		private Dictionary<int, IActorContainer> containers = new Dictionary<int, IActorContainer>();

		public void Start() { }

		public int NextActorid() {
			int nextid = Interlocked.Increment(ref actorid);
			return nextid;
		}

		public ActorContext RegActor(IActor actor) {
			return RegActor(NextActorid(), actor);
		}

		public ActorContext RegActor(int id, IActor actor) {
			ActorContext context = new ActorContext(id, actor, this);
			actor.Context = context;

			lock(containers) {
				CAssert.Assert(!containers.ContainsKey(id));
				containers.Add(id, new ActorContainer(context));
			}
			return context;
		}

		public void Send(ActorMessage msg) {
			IActorContainer container = null;
			lock(containers) {
				containers.TryGetValue(msg.Target, out container);
			}
			CAssert.Assert(container != null, msg.Target.ToString());
			container.Post(msg);
		}
	}
}