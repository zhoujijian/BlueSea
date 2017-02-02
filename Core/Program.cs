using System;
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
		message.Force<EHelloCode>(_ => {
			IActorProxy proxy = ActorProxyFactory.Create(Context, Helper.WELCOME);
			Console.WriteLine("[Hello]Send Request Message ====>");
			proxy.SendReq("Hello", "Hello, BlueSea", reply1 => {
				Console.WriteLine("[Hello]<==== Receive Response: " + reply1);
				proxy.SendReq("Hello", "[Hello]Again Send Request Message ====>", reply2 => {
					Console.WriteLine("[Hello]<==== Again Receive Response: " + reply2);
				});
			});
		});
	}
}

public class Welcome : Actor {
	public override void Handle(ActorMessage message, Action<object> retback) {
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

	private static void exampleCar() {
//		Launcher launcher = new Launcher();
//		ActorSystem system = launcher.Launch();
//		system.CreateRef(CarActorid.CARROOM, new CarRoom(CarActorid.CARROOM, system));
	}
}