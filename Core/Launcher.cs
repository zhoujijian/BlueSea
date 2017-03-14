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
			SocketContainer container = new SocketContainer(system, agentCreate);
			system.RegContainer(container);
			container.Start();
			system.Start();

			IActorProxy proxy = ActorProxyFactory.Create(container.Context, Actorid.TCPMANAGER); // TODO: source context
			proxy.SendCmd(ServerMessage.LISTEN, new ServerListen {
				ActorId = -1000,
				Ip      = "127.0.0.1",
				Port    = 8888
			});

			return system;
		}
	}
}