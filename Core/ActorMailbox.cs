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
			run(msg);
		}

		private void run(ActorMessage msg) {
			Task.Run(() => {
				context.RecvCall(msg);

				ActorMessage another = null;
				lock(msgQ) {
					msgQ.Dequeue();
					if (msgQ.Count > 0) {
						another = msgQ.Peek();
					}
				}
				if (another != null) {
					run(another);
				}
			});			
		}
	}
}