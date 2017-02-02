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
			message.With<AgentMessage>(agent => {
				switch(message.Method) {
					case AgentMessage.OPEN:   { onOpen();               break; }
					case AgentMessage.CLIENT: { onRecvData(agent.Data); break; }
					case AgentMessage.CLOSE:  { onClose();              break; }
					default:                  { onNotify(message.Method, agent); break; }
				}
			});
		}

		public void SendClient(int message, int session, object data) {
			string textServer = JsonMapper.ToJson(new Session<object> {
				message = message,
				session = session,
				data = JsonMapper.ToJson(data) // must be json text, client unable to parse, otherwise
			});

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

		private void onNotify(string method, AgentMessage msg) {
			if (OnNotify != null) {
				OnNotify(method, msg.Data);
			}
		}

		private void onRecvData(object data) {
			string textClient = Encoding.UTF8.GetString((byte[])data);
			Session<string> client = JsonMapper.ToObject<Session<string>>(textClient);

			// if callback : request => response
			// else        : command => no-response
			if (OnData != null) {
				OnData(client.message, client.data, reply => {
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