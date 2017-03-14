using System;
using System.Runtime.CompilerServices;
using CUtil;

namespace Core {
	public class ActorWork<TResult> : INotifyCompletion {
		private TResult result;
		private Action continuation;

		public bool IsCompleted { get; private set; }
		public TResult GetResult() { return result; }
		public ActorWork<TResult> GetAwaiter() { return this; }

		public void OnCompleted(Action continuation) {
			this.continuation = continuation;
		}

		// executed by only 1 thread once
		public void CallContinue(object content) {
			CAssert.Assert(!IsCompleted);
			CAssert.Assert(continuation != null);

			result = (TResult)content;
			IsCompleted = true;

			if (continuation != null) {
				continuation();
				continuation = null;
			}
		}
	}

	public class ActorWork : INotifyCompletion {
		private Action continuation;

		public bool IsCompleted { get; private set; }
		public void GetResult() { }
		public ActorWork GetAwaiter() { return this; }

		public void OnCompleted(Action continuation) {
			this.continuation = continuation;
		}

		// executed by only 1 thread once
		public void CallContinue() {
			CAssert.Assert(!IsCompleted);
			CAssert.Assert(continuation != null);

			IsCompleted = true;

			if (continuation != null) {
				continuation();
			}
		}
	}
}