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

        public GameEvent(Kind eventKind, string target, object argument) {
            EventKind = eventKind;
            Target = target;
            Argument = argument;
        }
    }
}