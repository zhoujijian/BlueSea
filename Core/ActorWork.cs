using System;
using System.Runtime.CompilerServices;
using CUtil;

namespace Core {
	public class ActorWork<TResult> : INotifyCompletion {
		private TResult result;
		private Action onContinue;

		public bool IsCompleted { get; private set; }
		public TResult GetResult() { return result; }
		public ActorWork<TResult> GetAwaiter() { return this; }

		public void OnCompleted(Action continuation) {
			onContinue = continuation;
		}

		// executed by only 1 thread once
		public void CallContinue(object content) {
			CAssert.Assert(!IsCompleted);
			CAssert.Assert(onContinue != null);

			result = (TResult)content;
			IsCompleted = true;

			if (onContinue != null) {
				onContinue();
				onContinue = null;
			}
		}
	}

	public class ActorWork : INotifyCompletion {
		private Action onContinue;

		public bool IsCompleted { get; private set; }
		public void GetResult() { }
		public ActorWork GetAwaiter() { return this; }

		public void OnCompleted(Action continuation) {
			this.onContinue = continuation;
		}

		// executed by only 1 thread once
		public void CallContinue() {
			CAssert.Assert(!IsCompleted);
			CAssert.Assert(onContinue != null);

			IsCompleted = true;

			if (onContinue != null) {
				onContinue();
                onContinue = null;
			}
		}
	}
}