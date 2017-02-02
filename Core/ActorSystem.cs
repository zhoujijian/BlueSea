using System;
using System.Threading;
using System.Collections.Generic;
using CUtil;

namespace Core {
	public class ActorSystem {
		public const int PRESERVE = 1000;

		private int actorid = PRESERVE;
		private Queue<ActorMessage> messages = new Queue<ActorMessage>();
		private List<ActorMessage> lexec = new List<ActorMessage>();
		private Dictionary<int, ActorContext> contexts = new Dictionary<int, ActorContext>();

		public void Start() {
			Thread exec = new Thread(execMessage);
			exec.Start();
		}

		public int NextActorid() {
			int nextid = Interlocked.Increment(ref actorid);
			return nextid;
		}

		public ActorContext RegActor(IActor actor) {
			return RegActor(NextActorid(), actor);
		}

		public ActorContext RegActor(int id, IActor actor) {
			ActorContext ctx = new ActorContext(id, actor, this);
			actor.Context = ctx;

			lock(contexts) {
				CAssert.Assert(!contexts.ContainsKey(id));
				contexts.Add(id, ctx);
			}

			return ctx;
		}

		public void Send(ActorMessage message) {
			lock(messages) {
				messages.Enqueue(message);
			}
		}

		private void execMessage() {
			while(true) {
				lexec.Clear();

				lock(messages) {
					while(messages.Count > 0) {
						lexec.Add(messages.Dequeue());
					}
				}

				if (lexec.Count > 0) {
					lock (contexts) {
						foreach (ActorMessage msg in lexec) {
							ActorContext context = null;
							if (!contexts.TryGetValue(msg.Target, out context)) {
								throw new Exception("cannot find actor context:" + msg.Target);
							}
							context.RecvCall(msg);
						}
					}
				}

				Thread.Sleep(0);
			}
		}
	}
}