namespace Core {
    public class GameEvent {
        public enum Kind {
            ClientEstablished,
            ClientRecvData,
            ClientTerminated
        }

        public Kind EventKind  { get; }
        public string Target   { get; }
        public object Argument { get; }
        public ChannelContext Context { get; }

        public GameEvent(ChannelContext context, Kind eventKind, string target, object argument) {
            Context = context;
            EventKind = eventKind;
            Target = target;
            Argument = argument;
        }
    }
}