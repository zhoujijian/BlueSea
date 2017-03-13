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
			IActorProxy proxy = ActorProxyFactory.Create(container.Context, Actorid.TCPMANAGER); // TODO: source context
			system.Start();

			return system;
		}
	}
}