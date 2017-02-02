using System;

namespace CUtil {
	public class CAssert {
		public static void Assert(bool condt, string hint = null) {
			if (!condt) {
				throw new Exception(hint);
			}
		}
	}

	public class CLogger {
		public static void Log(string format, params object[] args) {
			if (args == null || args.Length <= 0) {
				Console.WriteLine(format);
			} else {
				Console.WriteLine(format, args);
			}
		}
	}
}