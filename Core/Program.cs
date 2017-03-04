using System;
using System.Threading;
using System.Threading.Tasks;
using Core;
using Core.Net;

public enum EHelloCode {
	Start = 0
}

public class Helper {
	public const int HELLO   = 100;
	public const int WELCOME = 101;
	public static ActorSystem System = new ActorSystem();
}

public class Hello : Actor {
	public override Task HandleCommandAsync(ActorMessage cmd) {
		cmd.Force<EHelloCode>(_ => {
			Console.WriteLine("[Hello]Current Thread:" + Thread.CurrentThread.ManagedThreadId);
			IActorProxy proxy = ActorProxyFactory.Create(Context, Helper.WELCOME);
			Console.WriteLine("[Hello]Send Request Message ====>");

			proxy.SendReq("Hello", "Hello, BlueSea", reply1 => {
				Console.WriteLine("[Hello]Current Thread:" + Thread.CurrentThread.ManagedThreadId);
				Console.WriteLine("[Hello]<==== Receive Response: " + reply1);

				proxy.SendReq("Hello", "[Hello]Again Send Request Message ====>", reply2 => {
					Console.WriteLine("[Hello]Current Thread:" + Thread.CurrentThread.ManagedThreadId);
					Console.WriteLine("[Hello]<==== Again Receive Response: " + reply2);
				});
			});
		});
		return Task.CompletedTask;
	}
}

public class Welcome : Actor {
	public override Task<object> HandleRequestAsync(ActorMessage req) {
		Console.WriteLine("[Welcome]Current Thread:" + Thread.CurrentThread.ManagedThreadId);
		Console.WriteLine("[Welcome]<==== Other's Request: " + req.Content);
		Console.WriteLine("[Welcome]Response to other ====>");
		return Task.FromResult<object>("You are welcome!");
	}
}

class MainClass {
	public static void Main(string[] args) {
		exampleHello();
	}

	private static void exampleHello() {
		ActorSystem system = Helper.System;
		ActorContext hello   = system.RegActor(Helper.HELLO, new Hello());
		ActorContext welcome = system.RegActor(Helper.WELCOME, new Welcome());
		system.Start();

		IActorProxy proxy = ActorProxyFactory.Create(hello, hello.ID);
		proxy.SendCmd("Start", EHelloCode.Start);

		Console.ReadLine();
	}
}