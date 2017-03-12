using System;
using System.Threading;
using Core;

public enum EHelloCode {
	Start = 0
}

public class Helper {
	public const int HELLO   = 100;
	public const int WELCOME = 101;
	public static ActorSystem System = new ActorSystem();
}

public class Hello : Actor {
	public override void Handle(ActorMessage message, Action<object> retback) {
		message.Force<EHelloCode>(async _ => {
			Console.WriteLine("<================ Async Handle ================>");

			Console.WriteLine("[Hello]Current Thread:" + Thread.CurrentThread.ManagedThreadId);
			IActorProxy proxy = ActorProxyFactory.Create(Context, Helper.WELCOME);
			Console.WriteLine("[Hello]Send Request Message ====>");

			string reply1 = await proxy.SendReqAsync<string>("Hello", "Hello, BlueSea");
			Console.WriteLine("[Hello]Current Thread:" + Thread.CurrentThread.ManagedThreadId);
			Console.WriteLine("[Hello]<==== Receive Response: " + reply1);

			string reply2 = await proxy.SendReqAsync<string>("Hello", "[Hello]Again Send Request Message ====>");
			Console.WriteLine("[Hello]Current Thread:" + Thread.CurrentThread.ManagedThreadId);
			Console.WriteLine("[Hello]<==== Again Receive Response: " + reply2);

			Console.WriteLine("<================ Async Handle ================>");
		});
	}
}

public class Welcome : Actor {
	public override void Handle(ActorMessage message, Action<object> retback) {
		Console.WriteLine("[Welcome]Current Thread:" + Thread.CurrentThread.ManagedThreadId);
		Console.WriteLine("[Welcome]<==== Other's Request: " + message.Content);
		Console.WriteLine("[Welcome]Response to other ====>");
		retback("You are welcome!");		
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