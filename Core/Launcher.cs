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
            system.Start();

            SocketMailbox mailbox = new SocketMailbox(system, agentCreate);
            system.Launch(mailbox.Id, mailbox.Context.Self);
            mailbox.Start();			

			IActorProxy proxy = ActorProxyFactory.Create(mailbox.Context, Actorid.TCPMANAGER); // TODO: source context
			proxy.SendCmd(ServerMessage.LISTEN, new ServerListen {
				ActorId = -1000,
				Ip      = "115.159.31.169",
				Port    = 8888
			});

			return system;
		}
	}
}
