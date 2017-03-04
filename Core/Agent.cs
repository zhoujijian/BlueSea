using System;
using System.Text;
using CUtil;
using LitJson;

namespace Core.Net {
	public class AgentMessage {
		public const string OPEN   = "Open";
		public const string CLIENT = "Client";
		public const string CLOSE  = "Close";

		public object Data { get; set; }
	}

	public class Session<TValue> where TValue : class {
		public int session { get; set; }
		public int message { get; set; }
		public TValue data { get; set; }
	}

	public class ChannelAgent : Actor {
		private IActorProxy proxySocket;

		public Action OnStart { get; set; }
		public Action OnClose { get; set; }
		public Action<int, string, Action<object>> OnData { get; set; }
		public Action<string, object> OnNotify { get; set; }

		public override void Handle(ActorMessage message, Action<object> retback) {
			object data = message.Content;
			switch(message.Method) {
				case AgentMessage.OPEN:   { onOpen();         break; }
				case AgentMessage.CLIENT: { onRecvData(data); break; }
				case AgentMessage.CLOSE:  { onClose();        break; }
				default: { onNotify(message.Method, data); break; }
			}
		}

		public void SendClient(int message, int session, object data) {
			string textServer = JsonMapper.ToJson(new Session<object> {
				message = message,
				session = session,
				data = data
			});
			CLogger.Log("[Agent{0}]Send json to client:{1}", Context.ID, textServer);

			proxySocket.SendCmd(ServerMessage.SENDDATA, new ServerMessage {
				Chanid = Context.ID,
				Data   = Encoding.UTF8.GetBytes(textServer)
			});
		}

		private void onOpen() {
			CLogger.Log("[Agent]Open a new client");

			proxySocket = ActorProxyFactory.Create(Context, Actorid.TCPMANAGER);
			if (OnStart != null) {
				OnStart();
			}
		}

		private void onNotify(string method, object data) {
			if (OnNotify != null) {
				OnNotify(method, data);
			}
		}

		private void onRecvData(object data) {
			string textClient = Encoding.UTF8.GetString((byte[])data);
			CLogger.Log("[Agent{0}]Receive json from client:{1}", Context.ID, textClient);
			Session<object> client = JsonMapper.ToObject<Session<object>>(textClient);

			// if callback : request => response
			// else        : command => no-response
			if (OnData != null) {
				OnData(client.message, textClient, reply => {
					SendClient(client.message, client.session, reply);
				});
			}
		}

		private void onClose() {
			CLogger.Log("[Agent]Close client");
			if (OnClose != null) {
				OnClose();
			}
		}
	}
}