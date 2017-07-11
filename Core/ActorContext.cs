using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using CUtil;

namespace Core {
	public class ActorContext {
		private int session = 0;
		private Dictionary<int, Action<object>> responses = new Dictionary<int, Action<object>>();

		private readonly int id;
		private readonly IActor self;
		private readonly ActorSystem system;

		public int          ID     { get { return id; } }
		public IActor       Self   { get { return self; } }
		public ActorSystem  System { get { return system; } }

		public ActorContext(int id, IActor self, ActorSystem system) {
			this.id = id;
			this.self = self;
			this.system = system;
		}

		public void SendCall(int kind, int target, string method, object param, Action<object> response) {
			CAssert.Assert((kind == ActorMessage.REQ && response != null) ||
			               (kind == ActorMessage.CMD && response == null));

			if (kind == ActorMessage.REQ) {
				CAssert.Assert(response != null);
				CAssert.Assert(!responses.ContainsKey(session));
				responses.Add(session, response);
			}
			ActorMessage msg = new ActorMessage(kind, session, ID, target, method, param);
			System.Send(msg);

			session ++;
		}

		public void RecvCall(ActorMessage msg) {
			checkMessage(msg);

            switch(msg.Kind) {
                case ActorMessage.TIMER: {
                    Action timerCallback = msg.Content as Action;
                    CAssert.Assert(timerCallback != null);
                    timerCallback();
                    break;
                }
                case ActorMessage.CMD: {
                    Self.Handle(msg, null);
                    break;
                }
                case ActorMessage.REQ: {
				    Self.Handle(msg, reply => {
					    CAssert.Assert(msg.Source > 0);
					    ActorMessage msgback = new ActorMessage(ActorMessage.REP, msg.Session, ID, msg.Source, null, reply);
					    System.Send(msgback);
				    });
                    break;
                }
                case ActorMessage.REP: {
				    Action<object> handler = responses[msg.Session];
				    responses.Remove(msg.Session);
                    handler?.Invoke(msg.Content);
                    break;
                }
            }
		}

		private void checkMessage(ActorMessage msg) {
			CAssert.Assert(msg.Target == ID);
			CAssert.Assert(msg.Kind == ActorMessage.REQ ||
                msg.Kind == ActorMessage.REP || msg.Kind == ActorMessage.CMD || msg.Kind == ActorMessage.TIMER);
		}
	}
}