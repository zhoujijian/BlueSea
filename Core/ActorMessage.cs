using System;
using CUtil;

namespace Core {
    public class ActorMessage {
        public const int REQ = 0;
        public const int REP = 1;
        public const int CMD = 2;
        public const int TIMER = 3; // TODO: how to handle system message, such as TIMER?

        public readonly int Kind;
        public readonly int Session;
        public readonly int Source;
        public readonly int Target;
        public readonly string Method;
        public readonly object Content;
        public readonly object[] Parameters;

        public ActorMessage(int kind, int session, int source, int target, string method, object param) {
            CAssert.Assert (kind == REQ || kind == REP || kind == CMD || kind == TIMER);
            Kind    = kind;
            Source  = source;
            Session = session;
            Target  = target;
            Method  = method;
            Content = param;
        }

        public ActorMessage(int kind, int session, int source, int target, string method, object[] parameters) {
            CAssert.Assert(kind == REQ || kind == REP || kind == CMD || kind == TIMER);
            Kind       = kind;
            Source     = source;
            Session    = session;
            Target     = target;
            Method     = method;
            Parameters = parameters;
        }

        public void With<T>(Action<T> handler) where T : class {
            T cont = Content == null ? default(T) : Content as T;
            handler(cont);
        }

        public void Force<T>(Action<T> handler) {
            handler((T)Content);
        }

        public override string ToString() {
            return
                string.Format("[ActorMessage]Source:{0}, Target:{1}, Message:{2}", Source, Target, Content);
        }
    }

    public class ActorRecv {
        public ActorMessage Message    { get; set; }
        public Action<object> Response { get; set; }
    }
}
