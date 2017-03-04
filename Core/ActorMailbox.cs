using System.Threading.Tasks;
using System.Collections.Generic;

namespace Core {
	public class ActorMailbox {
		private ActorContext context;
		private Queue<ActorMessage> msgQ = new Queue<ActorMessage>();

		public ActorMailbox(ActorContext context) {
			this.context = context;
		}

		public void Post(ActorMessage msg) {
			lock(msgQ) {
				msgQ.Enqueue(msg);
				if (msgQ.Count > 1) { return; }
			}
			runTask(msg);
		}

		private void runTask(ActorMessage msg) {
			Task.Run(async () => {
				object ret = await context.RecvCallAsync(msg);
				if (msg.Kind == ActorMessage.REQ) {
					ActorMessage reply = new ActorMessage(ActorMessage.REP, msg.Session, context.ID, msg.Source, null, ret);
					context.System.Send(reply);
				}

				ActorMessage another = null;
				lock(msgQ) {
					msgQ.Dequeue();
					if (msgQ.Count > 0) {
						another = msgQ.Peek();
					}
				}
				if (another != null) {
					runTask(another);
				}
			});
		}
	}
}