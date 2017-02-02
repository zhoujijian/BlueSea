﻿using System;
using System.Collections.Generic;
using CUtil;

namespace Core {
	public class ActorContext {
		private int session = 0;
		private Dictionary<int, Action<object>> responses = new Dictionary<int, Action<object>>();

		public int ID { get; private set; }
		public IActor Self { get; private set; }
		public ActorSystem System { get; private set; }

		public ActorContext(int id, IActor self, ActorSystem system) {
			ID = id;
			Self = self;
			System = system;
		}

		public void SendCall(int kind, int target, string method, object param, Action<object> response) {
			CAssert.Assert((kind == ActorMessage.REQ && response != null) ||
			               (kind == ActorMessage.CMD && response == null));

			if (kind == ActorMessage.REQ) {
				CAssert.Assert(response != null);
				CAssert.Assert(!responses.ContainsKey(session));
				responses.Add(session, response);
			}
			ActorMessage message = new ActorMessage(kind, session, ID, target, method, param);
			System.Send(message);

			session ++;
		}

		public void RecvCall(ActorMessage msg) {
			CAssert.Assert(msg.Target == ID);
			CAssert.Assert(msg.Kind == ActorMessage.REQ || msg.Kind == ActorMessage.REP || msg.Kind == ActorMessage.CMD);

			if (msg.Kind == ActorMessage.CMD) {
				Self.Handle(msg, null);
			}
			else if (msg.Kind == ActorMessage.REQ) {
				Self.Handle(msg, reply => {
					CAssert.Assert(msg.Source > 0);
					ActorMessage msgback = new ActorMessage(ActorMessage.REP, msg.Session, ID, msg.Source, null, reply);
					System.Send(msgback);
				});
			}
			else {
				Action<object> handler = responses[msg.Session];
				responses.Remove(msg.Session);

				if (handler != null) {
					handler(msg.Content);
				}
			}
		}
	}
}