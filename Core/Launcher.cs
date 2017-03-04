using Core.Net;

namespace Core {
	public class Actorid {
		public const int UNKNOWN    = -1;
		public const int TCPMANAGER = 10;
		public const int PRESERVE   = 1000;
	}

	public class Launcher {
		public ActorSystem Launch(System.Func<ChannelAgent> agentCreate) {
			ActorSystem system = new ActorSystem();
			ActorContext server = system.RegActor(Actorid.TCPMANAGER, new TcpManager(agentCreate));
			IActorProxy proxy = ActorProxyFactory.Create(server, Actorid.TCPMANAGER); // TODO: source context
			proxy.SendCmd(ServerMessage.START, null);
			system.Start();

			return system;
		}
	}
}